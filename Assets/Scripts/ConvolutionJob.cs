using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWaves.Operations;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// The main <c>ConvolutionJob</c> class.
/// </summary>
public class ConvolutionJob : MonoBehaviour
{
    /// <summary>
    /// Array of all available audio sources in the scene.
    /// </summary>
    private GameObject[] audioSources;
    /// <summary>
    /// List of all the original audio clips.
    /// </summary>
    private List<AudioClip> originalAudioClips;
    /// <summary>
    /// List of all the mixed audio clips
    /// </summary>
    private List<AudioClip> mixedAudioClips;
    /// <summary>
    /// List of all the audio samples
    /// </summary>
    private List<float[]> audioSamples;
    /// <summary>
    /// List of the <c>NativeArrays</c> of samples
    /// </summary>
    private List<NativeArray<float>> signals;
    /// <summary>
    /// The <c>RoomImpulseResponseJob</c> object in the scene
    /// </summary>
    private RoomImpulseResponseJob rirScript;
    /// <summary>
    /// List of the offset samples used for controlling the start index of the mixed sound.
    /// </summary>
    private List<int> offsetSamples;

    private void Start()
    {
        rirScript = GameObject.Find("Room Impulse Response").GetComponent<RoomImpulseResponseJob>();
        originalAudioClips = new List<AudioClip>();
        mixedAudioClips = new List<AudioClip>();
        audioSamples = new List<float[]>();
        signals = new List<NativeArray<float>>();
        offsetSamples = new List<int>();
    }
 
    /// <summary>
    /// Struct for the convolution job
    /// </summary>
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    struct ConvOverlapAddJob : IJob
    {
        [ReadOnly] [NoAlias] public NativeArray<float> imp;
        [ReadOnly] [NoAlias] public NativeArray<float> signal;
        [NoAlias] public NativeArray<float> conv;

        /// <summary>
        /// Performs a Bit Reversal Algorithm on a postive integer for given number of bits
        /// </summary>
        /// <param name="n">Number of bits</param>
        /// <param name="bits">The bits to be reversed</param>
        /// <returns>The bit reversed</returns>
        private static int BitReverse(int n, int bits)
        {
            int reversedN = n;
            int count = bits - 1;

            n >>= 1;
            while (n > 0)
            {
                reversedN = (reversedN << 1) | (n & 1);
                count--;
                n >>= 1;
            }

            return ((reversedN << count) & ((1 << bits) - 1));
        }
        /// <summary>
        /// Implementation of fast Fourier transform.
        /// </summary>
        /// <param name="buffer">The signal to be transformed</param>
        /// <param name="length">Length of the signal</param>
        /// <param name="inverse">Compute IFFT if <c>true</c></param>
        private void FFT([NoAlias] ref NativeArray<Complex> buffer, int length, bool inverse = false)
        {
            int bits = (int)math.log2(length);
            int swapPos;
            Complex temp;
            for (int j = 1; j < length; j++)
            {
                swapPos = BitReverse(j, bits);
                if (swapPos <= j)
                    continue;
                temp = buffer[j];
                buffer[j] = buffer[swapPos];
                buffer[swapPos] = temp;
            }

            // First the full length is used and 1011 value is swapped with 1101. Second if new swapPos is less than j
            // then it means that swap was happen when j was the swapPos.

            float term1 = 2 * math.PI * (inverse ? 1 : -1);
            float term2, term;
            int N2, i, k;
            int evenIndex, oddIndex;
            Complex exp;

            for (int N = 2; N <= length; N <<= 1)
            {
                term2 = term1 / (float)N;
                N2 = (N / 2);

                for (i = 0; i < length; i += N)
                {
                    for (k = 0; k < N / 2; k++)
                    {
                        evenIndex = i + k;
                        oddIndex = i + k + N2;

                        term = term2 * k;
                        exp = new Complex(math.cos(term), math.sin(term)) * buffer[oddIndex];

                        buffer[oddIndex] = buffer[evenIndex] - exp;
                        buffer[evenIndex] += exp;
                    }
                }
            }
            if (inverse)
            {
                for (i = 0; i < length; i++)
                    buffer[i] /= length;
            }
        }

        /*
         * The software implementation is inspired by Julius O. Smith's
         * implementation of the Overlap-Add Convolution algorithm.
         * 
         * Smith, J.O. Spectral Audio Signal Processing,
         * http://ccrma.stanford.edu/~jos/sasp/, online book,
         * 2011 edition,
         */

        /// <summary>
        /// Calculates the convolution of the impulse response and the signal.
        /// </summary>
        /// <param name="imp">Impulse response</param>
        /// <param name="signal">Signal</param>
        private void Convolve([NoAlias] ref NativeArray<float> imp, [NoAlias] ref NativeArray<float> signal)
        {
            int L = imp.Length;
            int Nsig = signal.Length;
            
            /*Nsig = (int)math.ceil((math.ceil(Nsig / L) - Nsig / L) * L);
            NativeArray<Complex> signalzp = new NativeArray<Complex>(Nsig, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < Nsig; i++)
                signalzp[i] = i > signal.Length ? new Complex() : new Complex(signal[i]);
            */
            int M = L;
            int Nfft = (int)math.pow(2, math.ceil(math.log2(M+L-1)));
            M = Nfft - L + 1;
            int R = M;
            int Nframes = 1 + (int)math.floor(math.abs((Nsig - M)) / R);

            Complex empty = new Complex();
            int i;

            //Complex* impZeroPadded = stackalloc Complex[Nfft];
            NativeArray<Complex> impZeroPadded = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (i = 0; i < Nfft; i++)
                impZeroPadded[i] = i < L ? new Complex(imp[i]) : empty;
            
            FFT(ref impZeroPadded, Nfft);

            //Complex* signalZeroPadded = stackalloc Complex[Nfft];
            //Complex* convFFT = stackalloc Complex[Nfft];

            NativeArray<Complex> signalZeroPadded = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<Complex> convFFT = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            int startIndex, stopIndex;
            float temp;
            for (int m = 0; m < Nframes; m++)
            {
                startIndex = m * R;
                stopIndex = math.min(m * R + M, Nsig) - 1;
                for (i = 0; i < Nfft; i++)
                    signalZeroPadded[i] = startIndex + i < stopIndex ? new Complex(signal[startIndex + i]) : empty;
                FFT(ref signalZeroPadded, Nfft);

                for (i = 0; i < Nfft; i++)
                    convFFT[i] = impZeroPadded[i] * signalZeroPadded[i];
                FFT(ref convFFT, Nfft, inverse: true);

                for (i = startIndex; i < m * R + Nfft; i++)
                {
                    temp = conv[i] + convFFT[i - startIndex].real;
                    conv[i] = temp;
                }
            }
            //impZeroPadded.Dispose();
            //signalZeroPadded.Dispose();
            //convFFT.Dispose();
        }

        public void Execute()
        {
            Convolve(ref imp, ref signal);
        }
    }

    /// <summary>
    /// Adds the audio source clip and initiates a generation of impulse responses.
    /// </summary>
    /// <param name="c">The audio clip</param>
    public void AddAudioSource(AudioClip c)
    {
        audioSources = GameObject.FindGameObjectsWithTag("Audio Source");

        foreach(GameObject a in audioSources)
        {
            float volume = 0.2f;
            if (Equals(a.name, "Guitar Play"))
                volume = 0.5f;
            if (a.transform.childCount == 0)
            {
                GameObject go = new GameObject();
                go.AddComponent<AudioSource>();
                go.GetComponent<AudioSource>();
                go.GetComponent<AudioSource>().clip = c;
                go.GetComponent<AudioSource>().loop = true;
                go.GetComponent<AudioSource>().volume = volume;
                go.GetComponent<AudioSource>().mute = true;
                go.GetComponent<AudioSource>().spatialBlend = 1;
                go.GetComponent<AudioSource>().minDistance = 1;
                go.GetComponent<AudioSource>().maxDistance = 60;
                go.GetComponent<AudioSource>().Play();
                Instantiate(go, a.transform);
                Destroy(go);
            }
        }

        originalAudioClips.Add(c);
        mixedAudioClips.Add(c);
        float[] audioSample = new float[c.samples * c.channels];
        c.GetData(audioSample, 0);
        audioSamples.Add(audioSample);
        signals.Add(new NativeArray<float>(audioSample, Allocator.Persistent));
        offsetSamples.Add(0);

        rirScript.ToggleCalculateImpulseResponse();
    }

    private void Update()
    {
        /// Initiate the process of convolving with the impulse responses.
        if (rirScript.newImpulseResponse)
        {
            rirScript.newImpulseResponse = false;

            int nAudioSources = audioSources.Length;
            
            List<NativeArray<float>> convs = new List<NativeArray<float>>(nAudioSources);

            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(nAudioSources, Allocator.Temp);

            /// Used for printing a room impulse response to file.
            /*
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            
            rirScript.jobHandles[0].Complete();
            float[] imp = rirScript.impulseResponses[0].ToArray();
            foreach (float f in imp)
            {
                string temp = f.ToString().Replace(',', '.');
                s.Append(temp).Append(";");
            }
            File.WriteAllText(Application.dataPath + "/output_plot.txt", s.ToString());
            */

            for (int i = 0; i < nAudioSources; i++)
            {
                int convLength = rirScript.nSamples + audioSamples[i].Length - 1;
                convs.Add(new NativeArray<float>(convLength, Allocator.TempJob));
            }
           
            for (int i = 0; i < nAudioSources; i++)
            {
                rirScript.jobHandles[i].Complete();

                ConvOverlapAddJob job = new ConvOverlapAddJob()
                {
                    imp = rirScript.impulseResponses[i],
                    signal = signals[i],
                    conv = convs[i],
                };

                JobHandle jobHandle = job.Schedule();
                jobHandles.Add(jobHandle);
            }

            for (int i = 0; i < nAudioSources; i++)
            {
                jobHandles[i].Complete();
                rirScript.impulseResponses[i].Dispose();

                float[] mixedAudio = convs[i].ToArray();
                float multiplicationFactor = Mathf.Max(audioSamples[i]) / Mathf.Max(mixedAudio);
                int numSamples = mixedAudio.Length;

                convs[i].Dispose();

                for (int j = 0; j < numSamples; j++)
                    mixedAudio[j] *= multiplicationFactor;
                
                AudioClip mixedAudioClip = AudioClip.Create("MixedAudioClip", numSamples, originalAudioClips[i].channels, originalAudioClips[i].frequency, false);
                mixedAudioClips[i] = mixedAudioClip; 

                offsetSamples[i] += numSamples - audioSources[i].GetComponent<AudioSource>().timeSamples;
                if (offsetSamples[i] >= numSamples)
                    offsetSamples[i] = Mathf.Abs(numSamples - offsetSamples[i]);

                mixedAudioClip.SetData(mixedAudio, offsetSamples[i]);

                audioSources[i].GetComponent<AudioSource>().Stop();
                audioSources[i].GetComponent<AudioSource>().clip = mixedAudioClip;
                audioSources[i].GetComponent<AudioSource>().Play();
                
            }
            rirScript.jobHandles.Dispose();
            jobHandles.Dispose();

        }
    }

    /*
    public bool isAudioEffect = true;
    public void ToggleAudioEffect()
    {
        isAudioEffect = !isAudioEffect;

        foreach (GameObject a in audioSources)
        {
            a.GetComponent<AudioSource>().mute = !isAudioEffect;
            a.transform.GetChild(0).GetComponent<AudioSource>().mute = isAudioEffect;
        }

    }
    */
    private void OnDisable()
    {
        for (int i = 0; i < signals.Count; i++)
            signals[i].Dispose();
    }
    /// <summary>
    /// Struct for handling complex numbers
    /// </summary>
    public struct Complex
    {
        public float real;
        public float imag;

        /// <summary>
        /// Empty constructor
        /// </summary>
        /// <param name="real">Real part</param>
        /// <param name="imag">Imaginary part</param>
        public Complex(float real=0.0f, float imag=0.0f)
        {
            this.real = real;
            this.imag = imag;
        }

        /// <summary>
        /// Converts from polar form to rectangular
        /// </summary>
        /// <param name="r">Radius</param>
        /// <param name="radians">Radians</param>
        /// <returns>Rectangular form</returns>
        public static Complex FromPolar(float r, float radians)
        {
            Complex data = new Complex(r * math.cos(radians), r * math.sin(radians));
            return data;
        }

        /// <summary>
        /// Override addition operator
        /// </summary>
        /// <param name="a">First complex number</param>
        /// <param name="b">Second complex number</param>
        /// <returns>Complex number <c>a</c> + complex number <c>b</c></returns>
        public static Complex operator +(Complex a, Complex b)
        {
            Complex data = new Complex(a.real + b.real, a.imag + b.imag);
            return data;
        }
        /// <summary>
        /// Override subtraction operator
        /// </summary>
        /// <param name="a">First complex number</param>
        /// <param name="b">Second complex number</param>
        /// <returns>Complex number <c>a</c> - complex number <c>b</c></returns>
        public static Complex operator -(Complex a, Complex b)
        {
            Complex data = new Complex(a.real - b.real, a.imag - b.imag);
            return data;
        }
        /// <summary>
        /// Override multiplication operator
        /// </summary>
        /// <param name="a">First complex number</param>
        /// <param name="b">Second complex number</param>
        /// <returns>Complex number <c>a</c> * complex number <c>b</c></returns>
        public static Complex operator *(Complex a, Complex b)
        {
            Complex data = new Complex((a.real * b.real) - (a.imag * b.imag), (a.real * b.imag + (a.imag * b.real)));
            return data;
        }
        /// <summary>
        /// Override multiplication operator
        /// </summary>
        /// <param name="a">Complex number</param>
        /// <param name="b">Float</param>
        /// <returns>Complex number <c>a</c> * float <c>b</c></returns>
        public static Complex operator *(Complex a, float b)
        {
            Complex data = new Complex(a.real * b, a.imag * b);
            return data;
        }
        /// <summary>
        /// Override division operator
        /// </summary>
        /// <param name="a">First complex number</param>
        /// <param name="b">Second complex number</param>
        /// <returns>Complex number <c>a</c> / complex number <c>b</c></returns>
        public static Complex operator /(Complex a, Complex b)
        {
            Complex data = new Complex(((a.real * b.real) + (a.imag * b.imag)) / (math.pow(b.real, 2) + math.pow(b.imag, 2)), (a.imag * b.real - (a.real * b.imag)) / (math.pow(b.real, 2) + math.pow(b.imag, 2)));
            return data;
        }
        /// <summary>
        /// Override division operator
        /// </summary>
        /// <param name="a">Complex number</param>
        /// <param name="b">Float</param>
        /// <returns>Complex number <c>a</c> / float <c>b</c></returns>
        public static Complex operator /(Complex a, int b)
        {
            Complex data = new Complex(a.real / b, a.imag / b);
            return data;
        }
        /// <summary>
        /// Returns magnitude of complex number
        /// </summary>
        public float Magnitude
        {
            get
            {
                return math.sqrt(math.pow(real, 2) + math.pow(imag, 2));
            }
        }
        /// <summary>
        /// Returns the phase of complex number
        /// </summary>
        public float Phase
        {
            get
            {
                return math.atan(imag / real);
            }
        }
    }
  
}
