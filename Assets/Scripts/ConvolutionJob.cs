//#define NOT_OVERLAP

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

public class ConvolutionJob : MonoBehaviour
{
    private GameObject[] audioSources;
    private List<AudioClip> originalAudioClips;
    private List<AudioClip> mixedAudioClips;
    private List<float[]> audioSamples;
    private List<NativeArray<float>> signals;

    private RoomImpulseResponseJob rirScript;

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
#if NOT_OVERLAP
    [BurstCompile(CompileSynchronously = true)]
    struct MultiplyJob : IJobParallelFor
    {
        public NativeArray<float> array;
        public float factor; 

        public void Execute(int index)
        {
            array[index] *= factor;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ConvJob : IJob
    {
        [ReadOnly]
        public NativeArray<float> imp;
        [ReadOnly]
        public NativeArray<float> signal;
        [WriteOnly]
        public NativeArray<float> conv;
        int BitReverse(int n, int bits)
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
        void FFT(NativeArray<Complex> buffer, bool inverse = false)
        {
            int bits = (int)math.log2(buffer.Length);

            for (int j = 1; j < buffer.Length; j++)
            {
                int swapPos = BitReverse(j, bits);
                if (swapPos <= j)
                    continue;
                Complex temp = buffer[j];
                buffer[j] = buffer[swapPos];
                buffer[swapPos] = temp;
            }

            // First the full length is used and 1011 value is swapped with 1101. Second if new swapPos is less than j
            // then it means that swap was happen when j was the swapPos.

            for (int N = 2; N <= buffer.Length; N <<= 1)
            {
                for (int i = 0; i < buffer.Length; i += N)
                {
                    for (int k = 0; k < N / 2; k++)
                    {

                        int evenIndex = i + k;
                        int oddIndex = i + k + (N / 2);
                        Complex even = buffer[evenIndex];
                        Complex odd = buffer[oddIndex];

                        float term = 2 * math.PI * k / (float)N * (inverse ? 1 : -1);
                        Complex exp = new Complex(math.cos(term), math.sin(term)) * odd;

                        buffer[evenIndex] = even + exp;
                        buffer[oddIndex] = even - exp;

                    }
                }
            }
            if (inverse)
            {
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] /= buffer.Length;
            }
        }
        void Convolve(NativeArray<float> imp, NativeArray<float> signal)
        {
            int convLength = imp.Length + signal.Length - 1;
            convLength = math.ceilpow2(convLength);
            NativeArray<Complex> imp_fft = new NativeArray<Complex>(convLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<Complex> signal_fft = new NativeArray<Complex>(convLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < convLength; i++)
            {
                imp_fft[i] = new Complex(i >= imp.Length ? 0 : imp[i], 0);
                signal_fft[i] = new Complex(i >= signal.Length ? 0 : signal[i], 0);
            }
                
            FFT(imp_fft);
            FFT(signal_fft);

            NativeArray<Complex> convFFT = new NativeArray<Complex>(convLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < convLength; i++)
                convFFT[i] = imp_fft[i] * signal_fft[i];

            imp_fft.Dispose();
            signal_fft.Dispose();

            FFT(convFFT, inverse: true);

            for (int i = 0; i <= imp.Length + signal.Length - 1 - 1; i++)
                conv[i] = convFFT[i].real;
            convFFT.Dispose();
        }

        public void Execute()
        {
            Convolve(imp, signal);
        }
        

        
        //double[] ToDoubleArray(float[] a)
        //{
        //    double[] d = new double[a.Length];
        //    for (int i = 0; i < a.Length; i++)
        //        d[i] = (double)a[i];
        //    return d;
        //}
        //public void Execute()
        //{
        //    double[] c = Operation.Convolve(ToDoubleArray(signal.ToArray()), ToDoubleArray(imp.ToArray()));
        //    for (int i = 0; i < c.Length; i++)
        //        conv[i] = (float)c[i];   
        //}
          
    } 
# else      
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    struct ConvOverlapAddJob : IJob
    {
        [ReadOnly] public NativeArray<float> imp;
        [ReadOnly] public NativeArray<float> signal;
        public NativeArray<float> conv;

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
        private void FFT(ref NativeArray<Complex> buffer, int length, bool inverse = false)
        {
            int bits = (int)math.log2(length);

            for (int j = 1; j < length; j++)
            {
                int swapPos = BitReverse(j, bits);
                if (swapPos <= j)
                    continue;
                Complex temp = buffer[j];
                buffer[j] = buffer[swapPos];
                buffer[swapPos] = temp;
            }

            // First the full length is used and 1011 value is swapped with 1101. Second if new swapPos is less than j
            // then it means that swap was happen when j was the swapPos.

            for (int N = 2; N <= length; N <<= 1)
            {
                for (int i = 0; i < length; i += N)
                {
                    for (int k = 0; k < N / 2; k++)
                    {

                        int evenIndex = i + k;
                        int oddIndex = i + k + (N / 2);
                        Complex even = buffer[evenIndex];
                        Complex odd = buffer[oddIndex];

                        float term = 2 * math.PI * k / (float)N * (inverse ? 1 : -1);
                        Complex exp = new Complex(math.cos(term), math.sin(term)) * odd;

                        buffer[evenIndex] = even + exp;
                        buffer[oddIndex] = even - exp;

                    }
                }
            }
            if (inverse)
            {
                for (int i = 0; i < length; i++)
                    buffer[i] /= length;
            }
        }
        private void Convolve(ref NativeArray<float> imp, ref NativeArray<float> signal)
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

            //Complex* impZeroPadded = stackalloc Complex[Nfft];
            NativeArray<Complex> impZeroPadded = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < Nfft; i++)
                impZeroPadded[i] = i < L ? new Complex(imp[i]) : new Complex();
            
            FFT(ref impZeroPadded, Nfft);

            //Complex* signalZeroPadded = stackalloc Complex[Nfft];
            //Complex* convFFT = stackalloc Complex[Nfft];

            NativeArray<Complex> signalZeroPadded = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<Complex> convFFT = new NativeArray<Complex>(Nfft, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int m = 0; m < Nframes; m++)
            {
                int startIndex = m * R;
                int stopIndex = math.min(m * R + M, Nsig) - 1;
                for (int i = 0; i < Nfft; i++)
                    signalZeroPadded[i] = startIndex + i < stopIndex ? new Complex(signal[startIndex + i]) : new Complex();
                FFT(ref signalZeroPadded, Nfft);

                for (int i = 0; i < Nfft; i++)
                    convFFT[i] = impZeroPadded[i] * signalZeroPadded[i];
                FFT(ref convFFT, Nfft, inverse: true);

                float temp;
                for (int i = startIndex; i < m * R + Nfft; i++)
                {
                    temp = conv[i] + convFFT[i - startIndex].real;
                    conv[i] = temp;
                }
            }
            impZeroPadded.Dispose();
            signalZeroPadded.Dispose();
            convFFT.Dispose();
        }
        private void OverlapAdd(ref NativeArray<float> imp, ref NativeArray<float> signal)
        {
            //convolution using OVERLAP-ADD

            // get length that arrays will be zero-padded to
            int K = (int)math.pow(2, math.ceil(math.log(imp.Length + signal.Length - 1) / math.log(2)));

            //create temporary (zero padded to K) arrays
            NativeArray<Complex> ir_pad = new NativeArray<Complex>(K, Allocator.Temp);
            for (int i = 0; i < imp.Length; ++i)
                ir_pad[i] = new Complex(imp[i]);
            NativeArray<Complex> data_pad = new NativeArray<Complex>(K, Allocator.Temp);
            for (int i = 0; i < signal.Length; ++i)
                data_pad[i] = new Complex(signal[i]);

            //FFT 
            FFT(ref data_pad, K);
            FFT(ref ir_pad, K);
            //convolution
            NativeArray<Complex> ifft = new NativeArray<Complex>(K, Allocator.Temp);
            for (int i = 0; i < ifft.Length; ++i)
                ifft[i] = data_pad[i] * ir_pad[i] * K;
            FFT(ref ifft, K, true);

            for (int i = 0; i < conv.Length; ++i)
                conv[i] = ifft[i].real;
        }
        public void Execute()
        {
            Convolve(ref imp, ref signal);
            //OverlapAdd(ref imp, ref signal);
        }
    }
# endif

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
        if (rirScript.newImpulseResponse)
        {
            rirScript.newImpulseResponse = false;

            int nAudioSources = audioSources.Length;
            
            //List<NativeArray<float>> signals = new List<NativeArray<float>>(nAudioSources);
            List<NativeArray<float>> convs = new List<NativeArray<float>>(nAudioSources);

            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(nAudioSources, Allocator.Temp);
            
            /*
            string s = "";
            rirScript.jobHandles[0].Complete();
            float[] imp = rirScript.impulseResponses[0].ToArray();
            foreach (float f in imp)
            {
                string temp = f.ToString().Replace(',', '.');
                s += temp + ";";
            }
            File.WriteAllText(Application.dataPath + "/output_dir.txt", s);
            */

            for (int i = 0; i < nAudioSources; i++)
            {
                int convLength = rirScript.nSamples + audioSamples[i].Length - 1;
                convs.Add(new NativeArray<float>(convLength, Allocator.TempJob));
            }
           
            for (int i = 0; i < nAudioSources; i++)
            {
                rirScript.jobHandles[i].Complete();

#if NOT_OVERLAP
                ConvJob job = new ConvJob()
                {
                    imp = rirScript.impulseResponses[i],
                    signal = signals[i],
                    conv = convs[i],
                };
#else
                ConvOverlapAddJob job = new ConvOverlapAddJob()
                {
                    imp = rirScript.impulseResponses[i],
                    signal = signals[i],
                    conv = convs[i],
                };
#endif
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
                
                /*MultiplyJob multiplyJob = new MultiplyJob()
                {
                    array = convs[i],
                    factor = multiplicationFactor
                };

                JobHandle multiplyHandle = multiplyJob.Schedule(numSamples, 64);
                multiplyHandle.Complete();
                */
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

    private void OnDisable()
    {
        for (int i = 0; i < signals.Count; i++)
            signals[i].Dispose();
    }
    public struct Complex
    {
        public float real;
        public float imag;
        //Empty constructor
 
        public Complex(float real=0.0f, float imag=0.0f)
        {
            this.real = real;
            this.imag = imag;
        }

        //Convert from polar to rectangular
        public static Complex FromPolar(float r, float radians)
        {
            Complex data = new Complex(r * math.cos(radians), r * math.sin(radians));
            return data;
        }
        //Override addition operator
        public static Complex operator +(Complex a, Complex b)
        {
            Complex data = new Complex(a.real + b.real, a.imag + b.imag);
            return data;
        }
        //Override subtraction operator
        public static Complex operator -(Complex a, Complex b)
        {
            Complex data = new Complex(a.real - b.real, a.imag - b.imag);
            return data;
        }
        //Override multiplication operator
        public static Complex operator *(Complex a, Complex b)
        {
            Complex data = new Complex((a.real * b.real) - (a.imag * b.imag), (a.real * b.imag + (a.imag * b.real)));
            return data;
        }
        //Override multiplication operator
        public static Complex operator *(Complex a, float b)
        {
            Complex data = new Complex(a.real * b, a.imag * b);
            return data;
        }
        //Override division operator
        public static Complex operator /(Complex a, Complex b)
        {
            Complex data = new Complex(((a.real * b.real) + (a.imag * b.imag)) / (math.pow(b.real, 2) + math.pow(b.imag, 2)), (a.imag * b.real - (a.real * b.imag)) / (math.pow(b.real, 2) + math.pow(b.imag, 2)));
            return data;
        }
        //Override division operator
        public static Complex operator /(Complex a, int b)
        {
            Complex data = new Complex(a.real / b, a.imag / b);
            return data;
        }
        //Return magnitude of complex number
        public float Magnitude
        {
            get
            {
                return math.sqrt(math.pow(real, 2) + math.pow(imag, 2));
            }
        }
        public float Phase
        {
            get
            {
                return math.atan(imag / real);
            }
        }
    }
  
}
