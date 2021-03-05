using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using TMPro;
using System.Runtime.CompilerServices;

public class RoomImpulseResponseJob : MonoBehaviour
{
    [System.Serializable]
    public struct Dimension
    {
        public float x, y, z;
        public Dimension(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Coefficients
    {
        public float frontWall, backWall, leftWall, rightWall, floor, ceiling;
        public bool isReflection;
        public Coefficients(float frontWall, float backWall, float leftWall, float rightWall, float floor, float ceiling, bool isReflection)
        {
            this.frontWall = frontWall;
            this.backWall = backWall;
            this.leftWall = leftWall;
            this.rightWall = rightWall;
            this.floor = floor;
            this.ceiling = ceiling;
            this.isReflection = isReflection;
        }
        public void ToReflection()
        {
            this.frontWall = AbsorptionToReflection(this.frontWall);
            this.backWall = AbsorptionToReflection(this.backWall);
            this.leftWall = AbsorptionToReflection(this.leftWall);
            this.rightWall = AbsorptionToReflection(this.rightWall);
            this.floor = AbsorptionToReflection(this.floor);
            this.ceiling = AbsorptionToReflection(this.ceiling);
            this.isReflection = true;
        }
    }
    [System.Serializable]
    public struct Room
    {
        public Room(Dimension dimension, Coefficients coefficients, float reverberationTime = -1)
        {
            this.dimension = dimension;
            this.coefficients = coefficients;
            this.reverberationTime = reverberationTime;
            this.useReverberationTime = false;
        }
        public Dimension dimension;
        public Coefficients coefficients;
        public float reverberationTime;
        public bool useReverberationTime;
    }

    public Vector3 receiverPosition = new Vector3();
    public List<Vector3> sourcePositions = new List<Vector3>();
    public Room room = new Room();

    public bool calculateImpulseResponse = false;
    [System.NonSerialized]
    public bool newImpulseResponse = false;

    public enum MicrophoneType
    {
        Bidirectional,
        Hypercardioid,
        Cardioid,
        Subcardioid,
        Omnidirectional
    }

    [BurstCompile]
    public struct RirJob : IJob
    {
        [ReadOnly]
        public float3 receiverPos, sourcePos;
        public Room room;
        public NativeArray<float> impulseResponse;
        private static float Sinc(float x)
        {
            if (x == 0)
                return 1.0f;
            return math.sin(x) / x;
        }
        private static float SimMicrophone(float x, float y, float z, float2 microphone_angle, MicrophoneType mtype)
        {
            if (mtype == MicrophoneType.Bidirectional || mtype == MicrophoneType.Cardioid || mtype == MicrophoneType.Subcardioid || mtype == MicrophoneType.Hypercardioid)
            {
                float gain, vartheta, varphi, rho;

                // Polar Pattern         rho
                // ---------------------------
                // Bidirectional         0
                // Hypercardioid         0.25
                // Cardioid              0.5
                // Subcardioid           0.75
                // Omnidirectional       1

                switch (mtype)
                {
                    case MicrophoneType.Bidirectional:
                        rho = 0;
                        break;
                    case MicrophoneType.Hypercardioid:
                        rho = 0.25f;
                        break;
                    case MicrophoneType.Cardioid:
                        rho = 0.5f;
                        break;
                    case MicrophoneType.Subcardioid:
                        rho = 0.75f;
                        break;
                    default:
                        rho = 1;
                        break;
                };

                vartheta = math.acos(z / math.sqrt(math.pow(x, 2) + math.pow(y, 2) + math.pow(z, 2)));
                varphi = math.atan2(y, x);

                gain = math.sin(math.PI / 2 - microphone_angle[1]) * math.sin(vartheta) * math.cos(microphone_angle[0] - varphi) + math.cos(math.PI / 2 - microphone_angle[1]) * math.cos(vartheta);
                gain = rho + (1 - rho) * gain;

                return gain;
            }
            else
                return 1;
        }
        private unsafe void ComputeRIR(float fs, float3 rr, int nMicrophones, float3 ss, float3 LL, int nSamples, NativeArray<float> beta, MicrophoneType microphone_type, int nOrder, float2 microphone_angle, int isHighPassFilter)
        {
            // Temporary variables and constants (high-pass filter)
            float c = 343;
            float W = 2 * math.PI * 100 / fs; // The cut-off frequency equals 100 Hz
            float R1 = math.exp(-W);
            float B1 = 2 * R1 * math.cos(W);
            float B2 = -R1 * R1;
            float A1 = -(1 + R1);
            float X0;
            //float* Y = stackalloc float[3];
            float3 Y = new float3();

            // Temporary variables and constants (image-method)
            float Fc = 0.5f; // The normalized cut-off frequency equals (fs/2) / fs = 0.5
            int Tw = (int)(2 * math.round(0.004f * fs)); // The width of the low-pass FIR equals 8 ms
            float cTs = c / fs;
            NativeArray<float> LPI = new NativeArray<float>(Tw, Allocator.Temp);
            float3 r = new float3();
            float3 s = new float3();
            float3 L = new float3();
            float3 Rm = new float3();
            float3 Rp_plus_Rm = new float3();
            float3 refl = new float3();
            /*
            float* LPI = stackalloc float[Tw];
            float* r = stackalloc float[3];
            float* s = stackalloc float[3];
            float* L = stackalloc float[3];
            float* Rm = stackalloc float[3];
            float* Rp_plus_Rm = stackalloc float[3];
            float* refl = stackalloc float[3];
            */
            float fdist, dist;
            float gain;
            int startPosition;
            int n1, n2, n3;
            int q, j, k;
            int mx, my, mz;
            int n;

            s[0] = ss[0] / cTs; s[1] = ss[1] / cTs; s[2] = ss[2] / cTs;
            L[0] = LL[0] / cTs; L[1] = LL[1] / cTs; L[2] = LL[2] / cTs;

            for (int idxMicrophone = 0; idxMicrophone < nMicrophones; idxMicrophone++)
            {
                // [x_1 x_2 ... x_N y_1 y_2 ... y_N z_1 z_2 ... z_N]
                r[0] = rr[idxMicrophone + 0 * nMicrophones] / cTs;
                r[1] = rr[idxMicrophone + 1 * nMicrophones] / cTs;
                r[2] = rr[idxMicrophone + 2 * nMicrophones] / cTs;

                n1 = (int)math.ceil(nSamples / (2 * L[0]));
                n2 = (int)math.ceil(nSamples / (2 * L[1]));
                n3 = (int)math.ceil(nSamples / (2 * L[2]));

                // Generate room impulse response
                for (mx = -n1; mx <= n1; mx++)
                {
                    Rm[0] = 2 * mx * L[0];

                    for (my = -n2; my <= n2; my++)
                    {
                        Rm[1] = 2 * my * L[1];

                        for (mz = -n3; mz <= n3; mz++)
                        {
                            Rm[2] = 2 * mz * L[2];

                            for (q = 0; q <= 1; q++)
                            {
                                Rp_plus_Rm[0] = (1 - 2 * q) * s[0] - r[0] + Rm[0];
                                refl[0] = math.pow(beta[0], math.abs(mx - q)) * math.pow(beta[1], math.abs(mx));

                                for (j = 0; j <= 1; j++)
                                {
                                    Rp_plus_Rm[1] = (1 - 2 * j) * s[1] - r[1] + Rm[1];
                                    refl[1] = math.pow(beta[2], math.abs(my - j)) * math.pow(beta[3], math.abs(my));

                                    for (k = 0; k <= 1; k++)
                                    {
                                        Rp_plus_Rm[2] = (1 - 2 * k) * s[2] - r[2] + Rm[2];
                                        refl[2] = math.pow(beta[4], math.abs(mz - k)) * math.pow(beta[5], math.abs(mz));

                                        dist = math.sqrt(math.pow(Rp_plus_Rm[0], 2) + math.pow(Rp_plus_Rm[1], 2) + math.pow(Rp_plus_Rm[2], 2));

                                        if (math.abs(2 * mx - q) + math.abs(2 * my - j) + math.abs(2 * mz - k) <= nOrder || nOrder == -1)
                                        {
                                            fdist = math.floor(dist);
                                            if (fdist < nSamples)
                                            {
                                                gain = SimMicrophone(Rp_plus_Rm[0], Rp_plus_Rm[1], Rp_plus_Rm[2], microphone_angle, microphone_type)
                                                    * refl[0] * refl[1] * refl[2] / (4 * math.PI * dist * cTs);

                                                for (n = 0; n < Tw; n++)
                                                {
                                                    float t = (n - 0.5f * Tw + 1) - (dist - fdist);
                                                    LPI[n] = 0.5f * (1.0f + math.cos(2.0f * math.PI * t / Tw)) * 2.0f * Fc * Sinc(math.PI * 2.0f * Fc * t);
                                                }
                                                startPosition = (int)fdist - (Tw / 2) + 1;
                                                for (n = 0; n < Tw; n++)
                                                    if (startPosition + n >= 0 && startPosition + n < nSamples)
                                                    {
                                                        impulseResponse[idxMicrophone + nMicrophones * (startPosition + n)] += gain * LPI[n];
                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 'Original' high-pass filter as proposed by Allen and Berkley.
                if (isHighPassFilter == 1)
                {
                    for (int idx = 0; idx < 3; idx++) { Y[idx] = 0; }
                    for (int idx = 0; idx < nSamples; idx++)
                    {
                        X0 = impulseResponse[idxMicrophone + nMicrophones * idx];
                        Y[2] = Y[1];
                        Y[1] = Y[0];
                        Y[0] = B1 * Y[1] + B2 * Y[2] + X0;
                        impulseResponse[idxMicrophone + nMicrophones * idx] = Y[0] + A1 * Y[1] + R1 * Y[2];
                    }
                }
            }
            LPI.Dispose();
            beta.Dispose();
        }
        private unsafe void RirGenerator(float3 receiverPos, float3 sourcePos, Room r)
        {
            float fs = 16000;
            int nMic = 1;
            MicrophoneType micType = MicrophoneType.Bidirectional;
            int reflectionOrder = -1;
            bool hp_filter = false;
            float2 micOrientation = new float2(0, 0);
            float3 room_dimensions = new float3(r.dimension.x, r.dimension.y, r.dimension.z);
            if (!r.coefficients.isReflection)
                r.coefficients.ToReflection();
            NativeArray<float> beta = new NativeArray<float>(6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            beta[0] = r.coefficients.frontWall;
            beta[1] = r.coefficients.backWall;
            beta[2] = r.coefficients.leftWall;
            beta[3] = r.coefficients.rightWall;
            beta[4] = r.coefficients.floor;
            beta[5] = r.coefficients.ceiling;

            int nSamples = impulseResponse.Length;
            ComputeRIR(fs, receiverPos, nMic, sourcePos, room_dimensions, nSamples, beta, micType, reflectionOrder, micOrientation, hp_filter ? 1 : 0);
        }
        public void Execute()
        {
            RirGenerator(receiverPos, sourcePos, room);
            rand = new Unity.Mathematics.Random(1);
            //EasyRirGenerator(room.reverberationTime);
        }

        private void EasyRirGenerator(float t60)
        {
            float A = 0.01f;
            float decay = math.log(1/1000f) / t60;
            int fs = (int)(impulseResponse.Length / t60);
            for (int i = 0; i < impulseResponse.Length; i++)
                impulseResponse[i] = A * RandomGaussian() * math.exp(decay * i / fs);
        }

        private Unity.Mathematics.Random rand;
        private float RandomGaussian()
        {
            float u1 = 1- rand.NextFloat();
            float u2 = 1- rand.NextFloat();
            return (float)(math.sqrt(-2.0f * math.log(u1)) * math.cos(2.0f * math.PI * u2)); //random normal(0,1)
        }

    }

    private float CalculateReverberationTime()
    {
        float c = 343;
        float V = room.dimension.x * room.dimension.y * room.dimension.z;
        float S_alpha = (room.coefficients.frontWall + room.coefficients.backWall) * room.dimension.x * room.dimension.y +
                      (room.coefficients.leftWall + room.coefficients.rightWall) * room.dimension.y * room.dimension.z +
                      (room.coefficients.floor + room.coefficients.ceiling) * room.dimension.x * room.dimension.z;

        foreach (GameObject o in GameObject.FindGameObjectsWithTag("Object"))
            S_alpha += o.GetComponent<AcousticElementDisplay>().acousticElement.nrc * o.transform.localScale.x * o.transform.localScale.y;

        float reverberationTime = 24 * math.log(10.0f) * V / (c * S_alpha);
        if (reverberationTime < 0.128)
            reverberationTime = 0.128f;
        return reverberationTime;
    }
    public void ToggleCalculateImpulseResponse()
    {
        this.calculateImpulseResponse = !this.calculateImpulseResponse;
    }
    private static float AbsorptionToReflection(float alpha)
    {
        return math.sqrt(math.abs(1 - alpha));
    }
    private static Vector3 CalculatePositionRelativeToOrigo(Transform t, Vector3 position)
    {
        Vector3 origo = t.localPosition - (t.right * t.localScale.x / 2) - (t.up * t.localScale.y / 2) - (t.forward * t.localScale.z / 2);
        return position - origo;
    }

    private List<List<GameObject>> assignedAcousticElements;
    private Coefficients CalculateCoefficients()
    {
        assignedAcousticElements = new List<List<GameObject>>();

        for (int i = 0; i < 6; i++)
            assignedAcousticElements.Add(new List<GameObject>());

        GameObject[] acousticElements = GameObject.FindGameObjectsWithTag("Acoustic Element");

        foreach (GameObject acousticElement in acousticElements)
            AssignElementToWall(acousticElement);

        GameObject frontW = GameObject.Find("Front Wall");
        GameObject backtW = GameObject.Find("Back Wall");
        GameObject leftW = GameObject.Find("Left Wall");
        GameObject rightW = GameObject.Find("Right Wall");
        GameObject floor = GameObject.Find("Floor");
        GameObject ceiling = GameObject.Find("Ceiling");

        Coefficients c = new Coefficients();
        c.frontWall = CalculateWallCoefficient(frontW, assignedAcousticElements[0]);
        c.backWall = CalculateWallCoefficient(backtW, assignedAcousticElements[1]);
        c.leftWall = CalculateWallCoefficient(leftW, assignedAcousticElements[2]);
        c.rightWall = CalculateWallCoefficient(rightW, assignedAcousticElements[3]);
        c.floor = CalculateWallCoefficient(floor, assignedAcousticElements[4]);
        c.ceiling = CalculateWallCoefficient(ceiling, assignedAcousticElements[5]);
        c.isReflection = false;

        return c;
    }
    private void AssignElementToWall(GameObject acousticElement)
    {
        Transform frontW = GameObject.Find("Front Wall").transform;
        Transform backtW = GameObject.Find("Back Wall").transform;
        Transform leftW = GameObject.Find("Left Wall").transform;
        Transform rightW = GameObject.Find("Right Wall").transform;
        Transform floor = GameObject.Find("Floor").transform;
        Transform ceiling = GameObject.Find("Ceiling").transform;

        Vector3 p = acousticElement.transform.position;

        float dfw = Vector3.Distance(p, frontW.position);
        float dbw = Vector3.Distance(p, backtW.position);
        float dlw = Vector3.Distance(p, leftW.position);
        float drw = Vector3.Distance(p, rightW.position);
        float df = Vector3.Distance(p, floor.position);
        float dc = Vector3.Distance(p, ceiling.position);

        float dmin = Mathf.Min(dfw, dbw, dlw, drw, df, dc);

        if (dmin == dfw)
            assignedAcousticElements[0].Add(acousticElement);
        else if (dmin == dbw)
            assignedAcousticElements[1].Add(acousticElement);
        else if (dmin == dlw)
            assignedAcousticElements[2].Add(acousticElement);
        else if (dmin == drw)
            assignedAcousticElements[3].Add(acousticElement);
        else if (dmin == df)
            assignedAcousticElements[4].Add(acousticElement);
        else if (dmin == dc)
            assignedAcousticElements[5].Add(acousticElement);
    }
    private static float CalculateWallCoefficient(GameObject wall, List<GameObject> acousticElements)
    {
        float wallNrc = wall.GetComponent<AcousticElementDisplay>().acousticElement.nrc;
        if (acousticElements == null)
            return wallNrc;

        float wallSurface;
        if (wall.name == "Floor" || wall.name == "Ceiling")
            wallSurface = wall.transform.lossyScale.x * wall.transform.lossyScale.z;
        else
            wallSurface = wall.transform.lossyScale.x * wall.transform.lossyScale.y;

        float s = 0;
        float a = 0;
        foreach (GameObject element in acousticElements)
        {
            float acousticElementNrc = element.GetComponent<AcousticElementDisplay>().acousticElement.nrc;
            float acousticElementSurface = element.transform.lossyScale.x * element.transform.lossyScale.z;
            s += acousticElementSurface;
            a += acousticElementNrc * acousticElementSurface;
        }
        return (wallNrc * (wallSurface - s) + a) / wallSurface;
        // I dont feel like the numbers add up here...
        // return (wallNrc * wallSurface + a) / (wallSurface + s);

    }

    public bool isHighPass;
    public int reflections;

    public List<NativeArray<float>> impulseResponses;
    public NativeList<JobHandle> jobHandles;
    public int nSamples;
    private void Update()
    {
        if (this.calculateImpulseResponse)
        {
            this.calculateImpulseResponse = false;

            Transform t = GameObject.FindGameObjectWithTag("Room").transform;
            Transform player = GameObject.FindGameObjectWithTag("MainCamera").transform;

            GameObject[] audioSources = GameObject.FindGameObjectsWithTag("Audio Source");

            this.receiverPosition = CalculatePositionRelativeToOrigo(t, player.position);

            room.dimension = new Dimension(t.localScale.x, t.localScale.z, t.localScale.y);
            room.coefficients = CalculateCoefficients();
            if (!room.useReverberationTime)
                room.reverberationTime = CalculateReverberationTime();

            int fs = 16000;
            int maxSamples = (int)math.pow(2, 13);
            if (room.reverberationTime * fs > maxSamples)
                nSamples = maxSamples;
            else
                nSamples = (int)(room.reverberationTime * fs);

            //nSamples = (int)math.round(44100 * 1.5f * room.reverberationTime);


            this.sourcePositions = new List<Vector3>(audioSources.Length);
            this.impulseResponses = new List<NativeArray<float>>(audioSources.Length);
            
            jobHandles = new NativeList<JobHandle>(audioSources.Length, Allocator.TempJob);
            for (int i = 0; i < audioSources.Length; i++)
            {              
                this.sourcePositions.Add(CalculatePositionRelativeToOrigo(t, audioSources[i].transform.position));
                this.impulseResponses.Add(new NativeArray<float>(nSamples, Allocator.TempJob));
                RirJob job = new RirJob()
                {
                    impulseResponse = impulseResponses[i],
                    receiverPos = this.receiverPosition,
                    sourcePos = this.sourcePositions[i],
                    room = this.room
                };
                JobHandle jobHandle = job.Schedule();
                jobHandles.Add(jobHandle);
            }
       
            newImpulseResponse = true;
            GameObject.Find("Reverberation Time Text").GetComponent<TextMeshProUGUI>().text = room.reverberationTime.ToString("F2");
            
        }
    }

 
}
