using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWaves.Operations;

public class Convolution : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioClip;

    private float[] audioSamples;

    private GameObject rir;
    private RoomImpulseResponse rirScript;


    float[] Convolve(float[] h, float[] x)
    {
        int lenH = h.Length;
        int lenX = x.Length;

        int nconv = lenH + lenX - 1;
        int[] lenY = new int[nconv];
        int i, j, h_start, x_start, x_end;

        float[] y = new float[nconv];

        for (i = 0; i < nconv; i++)
        {
            x_start = Mathf.Max(0, i - lenH + 1);
            x_end = Mathf.Min(i + 1, lenX);
            h_start = Mathf.Min(i, lenH - 1);
            for (j = x_start; j < x_end; j++)
            {
                y[i] += h[h_start--] * x[j];
            }
        }
        return y;
    }

    private void Start()
    {
        rir = GameObject.Find("Room Impulse Response");

        audioSource = gameObject.GetComponent<AudioSource>();
        audioSamples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(audioSamples, 0);
    }

    private void Update()
    {
        if (!rir)
            rir = GameObject.Find("Room Impulse Response");
        else
        {
            rirScript = rir.GetComponent<RoomImpulseResponse>();

            if (rirScript.newImpulseResponse)
            {
                rirScript.newImpulseResponse = false;
                float[] imp = rirScript.roomImpulseResponse;

                double[] conv = Operation.Convolve(ToDoubleArray(audioSamples), ToDoubleArray(imp));

                float[] mixedAudio = ToFloatArray(conv);
                float multiplicationFactor = Mathf.Max(audioSamples) / Mathf.Max(mixedAudio);
                int numSamples = mixedAudio.Length;

                for (int i = 0; i < numSamples; i++)
                    mixedAudio[i] *= multiplicationFactor;

                AudioClip mixedAudioClip = AudioClip.Create("MixedAudioClip", numSamples, audioClip.channels, audioClip.frequency, false);

                //mixedAudioClip.SetData(mixedAudio, numSamples - audioSource.timeSamples);
                //audioClip.SetData(audioSamples, audioClip.samples - audioSource.timeSamples);
                mixedAudioClip.SetData(mixedAudio, 0);
                audioSource.clip = mixedAudioClip;
                audioSource.Stop();
                audioSource.Play();

            }
        }
    }

    float[] ToFloatArray(double[] a)
    {
        float[] f = new float[a.Length];
        int i = 0;
        foreach (double d in a)
        {
            f[i] = (float)d;
            i++;
        }
        return f;
    }
    double[] ToDoubleArray(float[] a)
    {
        double[] d = new double[a.Length];
        int i = 0;
        foreach (float f in a)
        {
            d[i] = (double)f;
            i++;
        }
        return d;
    }
}
