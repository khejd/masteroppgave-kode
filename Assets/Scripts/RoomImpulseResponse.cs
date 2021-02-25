using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using TMPro;

public class RoomImpulseResponse : MonoBehaviour
{
    /*
    Program     : Room Impulse Response Generator
    Description : Computes the response of an acoustic source to one or more
                  microphones in a reverberant room uMathf.Sing the image method [1,2].
                  [1] J.B. Allen and D.A. Berkley,
                  Image method for efficiently simulating small-room acoustics,
                  Journal Acoustic Society of America, 65(4), April 1979, p 943.
                  [2] P.M. Peterson,
                  Simulating the response of multiple microphones to a Mathf.Single
                  acoustic source in a reverberant room, Journal Acoustic
                  Society of America, 80(5), November 1986.
    Author      : dr.ir. E.A.P. Habets (e.habets@ieee.org)
    Version     : 2.2.20201022
    History     : 1.0.20030606 Initial version
                  1.1.20040803 + Microphone directivity
                               + Improved phase accuracy [2]
                  1.2.20040312 + Reflection order
                  1.3.20050930 + Reverberation Time
                  1.4.20051114 + Supports multi-channels
                  1.5.20051116 + High-pass filter [1]
                               + Microphone directivity control
                  1.6.20060327 + Minor improvements
                  1.7.20060531 + Minor improvements
                  1.8.20080713 + Minor improvements
                  1.9.20090822 + 3D microphone directivity control
                  2.0.20100920 + Calculation of the source-image position
                                 changed in the code and tutorial.
                                 This ensures a proper response to reflections
                                 in case a directional microphone is used.
                  2.1.20120318 + Avoid the use of unallocated memory
                  2.1.20140721 + Fixed computation of alpha
                  2.1.20141124 + The window and Mathf.Sinc are now both centered
                                 around t=0
                  2.2.20201022 + Fixed arrival time
    MIT License
    Copyright (C) 2003-2020 E.A.P. Habets
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARIMathf.SinG FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
    */
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
    public struct Coefficients
    {
        public float frontWall, backWall, leftWall, rightWall, floor, ceiling;
        public string type;
        public Coefficients(float frontWall, float backWall, float leftWall, float rightWall, float floor, float ceiling, string type = "reflection")
        {
            if (!Equals(type, "absorption") && !Equals(type, "reflection"))
                throw new InvalidDataException("Type must be absorption or reflection.");

            this.frontWall = frontWall;
            this.backWall = backWall;
            this.leftWall = leftWall;
            this.rightWall = rightWall;
            this.floor = floor;
            this.ceiling = ceiling;
            this.type = type;
            //if (Equals(type, "absorption"))
            //    ToReflection();
        }
        public void ToReflection()
        {
            this.frontWall = AbsorptionToReflection(this.frontWall);
            this.backWall = AbsorptionToReflection(this.backWall);
            this.leftWall = AbsorptionToReflection(this.leftWall);
            this.rightWall = AbsorptionToReflection(this.rightWall);
            this.floor = AbsorptionToReflection(this.floor);
            this.ceiling = AbsorptionToReflection(this.ceiling);
            this.type = "reflection";
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
        }
        public Dimension dimension;
        public Coefficients coefficients;
        public float reverberationTime;
    }

    public Vector3 receiverPosition = new Vector3();
    public Vector3 sourcePosition = new Vector3();

    public Room room = new Room();
    [System.NonSerialized]
    public float[] roomImpulseResponse;

    /// <summary>
    /// Returns reflection coefficient given an absorption coefficient.
    /// </summary>
    /// <param name="alpha">Absorption coefficient</param>
    /// <returns>Reflection coefficient.</returns>
    public static float AbsorptionToReflection(float alpha)
    {
        return Mathf.Sqrt(Mathf.Abs(1 - alpha));
    }

    /// <summary>
    /// Returns the sinc of angle x
    /// </summary>
    /// <param name="x">Value in radians</param>
    /// <returns></returns>
    float Sinc(float x)
    {
        if (x == 0)
            return 1.0f;
        else
            return (Mathf.Sin(x) / x);
    }

    /// <summary>
    /// Gain function for simulation microphone
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="microphone_angle"></param>
    /// <param name="mtype">'b', 'h', 'c' or 's'</param>
    /// <returns>Gain from microphone</returns>
    float SimMicrophone(float x, float y, float z, float[] microphone_angle, char mtype)
    {
        if (mtype == 'b' || mtype == 'c' || mtype == 's' || mtype == 'h')
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
                case 'b':
                    rho = 0;
                    break;
                case 'h':
                    rho = 0.25f;
                    break;
                case 'c':
                    rho = 0.5f;
                    break;
                case 's':
                    rho = 0.75f;
                    break;
                default:
                    rho = 1;
                    break;
            };

            vartheta = Mathf.Acos(z / Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2)));
            varphi = Mathf.Atan2(y, x);

            gain = Mathf.Sin(Mathf.PI / 2 - microphone_angle[1]) * Mathf.Sin(vartheta) * Mathf.Cos(microphone_angle[0] - varphi) + Mathf.Cos(Mathf.PI / 2 - microphone_angle[1]) * Mathf.Cos(vartheta);
            gain = rho + (1 - rho) * gain;

            return gain;
        }
        else
        {
            return 1;
        }
    }

    float[] ComputeRIR(float c, float fs, float[] rr, int nMicrophones, int nSamples, float[] ss, float[] LL, float[] beta, char microphone_type, int nOrder, float[] microphone_angle, int isHighPassFilter)
    {
        float[] imp = new float[nSamples];
        // Temporary variables and constants (high-pass filter)
        float W = 2 * Mathf.PI * 100 / fs; // The cut-off frequency equals 100 Hz
        float R1 = Mathf.Exp(-W);
        float B1 = 2 * R1 * Mathf.Cos(W);
        float B2 = -R1 * R1;
        float A1 = -(1 + R1);
        float X0;
        float[] Y = new float[3];

        // Temporary variables and constants (image-method)
        float Fc = 0.5f; // The normalized cut-off frequency equals (fs/2) / fs = 0.5
        int Tw = (int)(2 * Mathf.Round(0.004f * fs)); // The width of the low-pass FIR equals 8 ms
        float cTs = c / fs;
        float[] LPI = new float[Tw];
        float[] r = new float[3];
        float[] s = new float[3];
        float[] L = new float[3];
        float[] Rm = new float[3];
        float[] Rp_plus_Rm = new float[3];
        float[] refl = new float[3];
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

            n1 = (int)Mathf.Ceil(nSamples / (2 * L[0]));
            n2 = (int)Mathf.Ceil(nSamples / (2 * L[1]));
            n3 = (int)Mathf.Ceil(nSamples / (2 * L[2]));

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
                            refl[0] = Mathf.Pow(beta[0], Mathf.Abs(mx - q)) * Mathf.Pow(beta[1], Mathf.Abs(mx));

                            for (j = 0; j <= 1; j++)
                            {
                                Rp_plus_Rm[1] = (1 - 2 * j) * s[1] - r[1] + Rm[1];
                                refl[1] = Mathf.Pow(beta[2], Mathf.Abs(my - j)) * Mathf.Pow(beta[3], Mathf.Abs(my));

                                for (k = 0; k <= 1; k++)
                                {
                                    Rp_plus_Rm[2] = (1 - 2 * k) * s[2] - r[2] + Rm[2];
                                    refl[2] = Mathf.Pow(beta[4], Mathf.Abs(mz - k)) * Mathf.Pow(beta[5], Mathf.Abs(mz));

                                    dist = Mathf.Sqrt(Mathf.Pow(Rp_plus_Rm[0], 2) + Mathf.Pow(Rp_plus_Rm[1], 2) + Mathf.Pow(Rp_plus_Rm[2], 2));

                                    if (Mathf.Abs(2 * mx - q) + Mathf.Abs(2 * my - j) + Mathf.Abs(2 * mz - k) <= nOrder || nOrder == -1)
                                    {
                                        fdist = Mathf.Floor(dist);
                                        if (fdist < nSamples)
                                        {
                                            gain = SimMicrophone(Rp_plus_Rm[0], Rp_plus_Rm[1], Rp_plus_Rm[2], microphone_angle, microphone_type)
                                                * refl[0] * refl[1] * refl[2] / (4 * Mathf.PI * dist * cTs);

                                            for (n = 0; n < Tw; n++)
                                            {
                                                float t = (n - 0.5f * Tw + 1) - (dist - fdist);
                                                LPI[n] = 0.5f * (1.0f + Mathf.Cos(2.0f * Mathf.PI * t / Tw)) * 2.0f * Fc * Sinc(Mathf.PI * 2.0f * Fc * t);
                                            }
                                            startPosition = (int)fdist - (Tw / 2) + 1;
                                            for (n = 0; n < Tw; n++)
                                                if (startPosition + n >= 0 && startPosition + n < nSamples)
                                                {
                                                    imp[idxMicrophone + nMicrophones * (startPosition + n)] += gain * LPI[n];
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
                    X0 = imp[idxMicrophone + nMicrophones * idx];
                    Y[2] = Y[1];
                    Y[1] = Y[0];
                    Y[0] = B1 * Y[1] + B2 * Y[2] + X0;
                    imp[idxMicrophone + nMicrophones * idx] = Y[0] + A1 * Y[1] + R1 * Y[2];
                }
            }
        }
        return imp;

    }

    /// <summary>
    /// Returns a simulated impulse response.
    /// </summary>
    /// <param name="receiver_pos">Position of receiver.</param>
    /// <param name="source_pos">Position of source.</param>
    /// <param name="room_dimensions">Room dimensions in meters.</param>
    /// <param name="beta_input">Reflection coefficients for walls, floor and ceiling.</param>
    /// <param name="micType">Type of microphone.</param>
    /// <returns>Simulated impulse response.</returns>
    float[] RirGenerator(Vector3 receiverPos, Vector3 sourcePos, Room r, char micType, int reflectionOrder = -1, bool hp_filter = false)
    {
        float c = 343;
        float fs = 16000;
        int nSamples;
        // int roomDim = 3;
        float[] beta = new float[6];
        int nMic = 1;
        float[] micOrientation = { 0, 0 };

        float[] room_dimensions = { r.dimension.x, r.dimension.y, r.dimension.z };

        if (Equals(r.coefficients.type, "absorption"))
            r.coefficients.ToReflection();

        float[] beta_input = { r.coefficients.frontWall, r.coefficients.backWall, r.coefficients.leftWall, r.coefficients.rightWall, r.coefficients.floor, r.coefficients.ceiling };
        float[] receiver_pos = { receiverPos.x, receiverPos.y, receiverPos.z };
        float[] source_pos = { sourcePos.x, sourcePos.y, sourcePos.z };

        // Reflection coefficients or reverberation time?
        float V = room_dimensions[0] * room_dimensions[1] * room_dimensions[2];
        float reverberation_time = r.reverberationTime;
        if (reverberation_time > -1)
        {
            float S = 2 * (room_dimensions[0] * room_dimensions[2] + room_dimensions[1] * room_dimensions[2] + room_dimensions[0] * room_dimensions[1]);

            float alfa = 24 * V * Mathf.Log(10.0f) / (c * S * reverberation_time);
            if (alfa > 1)
            {
                UnityEngine.Debug.Log("Error: The reflection coefficients cannot be calculated using the current " +

                                "room parameters, i.e. room size and reverberation time.\n           Please " +

                                "specify the reflection coefficients or change the room parameters.");
            }
            for (int i = 0; i < 6; i++)
                beta[i] = Mathf.Sqrt(1 - alfa);
            
            if (reverberation_time == 0)
            {
                for (int i = 0; i < 6; i++)
                    beta[i] = 0;
            }
        }
        else
        {
            for (int i = 0; i < 6; i++)
                beta[i] = beta_input[i];

            float alpha = ((1 - Mathf.Pow(beta[0], 2)) + (1 - Mathf.Pow(beta[1], 2))) * room_dimensions[0] * room_dimensions[1] +
                    ((1 - Mathf.Pow(beta[2], 2)) + (1 - Mathf.Pow(beta[3], 2))) * room_dimensions[1] * room_dimensions[2] +
                    ((1 - Mathf.Pow(beta[4], 2)) + (1 - Mathf.Pow(beta[5], 2))) * room_dimensions[0] * room_dimensions[2];
            reverberation_time = 24 * Mathf.Log(10.0f) * V / (c * alpha);
            if (reverberation_time < 0.128)
                reverberation_time = 0.128f;
        }

        int maxSamples = (int)Mathf.Pow(2, 14);
        if (reverberation_time * fs > maxSamples)
            nSamples = maxSamples;
        else
            nSamples = (int)(reverberation_time * fs);
        //nSamples = 4096;

        GameObject.Find("Reverberation Time Text").GetComponent<TextMeshProUGUI>().text = reverberation_time.ToString("F2");

        return ComputeRIR(c, fs, receiver_pos, nMic, nSamples, source_pos, room_dimensions, beta, micType, reflectionOrder, micOrientation, hp_filter ? 1 : 0);
    }

    private void Start()
    {
        //float[] rr = { 4, 9, 2 };
        //float[] ss = { 9, 10, 2 };
        //float[] LL = { 10, 15, 3 };

        //float[] alpha = { 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f };
        //float[] alpha = { 0.99f, 0.99f, 0.99f, 0.99f, 0.99f, 0.99f };
        //float[] beta = new float[6];
        //int i = 0;
        //foreach (float a in alpha)
        //{
        //    beta[i] = AbsorptionToReflection(a);
        //    i++;
        //}
        // float[] beta = { 0.99f };
        // float[] beta = { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };
        /*
        receiverPosition = new Position(4, 9, 2);
        sourcePosition = new Position(9, 10, 2);
        room.dimension = new Dimension(10, 15, 3);
        room.coefficients = new Coefficients(0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, type: "absorption");
        */       
    }

    public bool CalculateImpulseResponse = false;
    [System.NonSerialized]
    public bool newImpulseResponse = false;
    public void ToggleCalculateImpulseResponse()
    {
        this.CalculateImpulseResponse = !this.CalculateImpulseResponse;
    }

    private Vector3 CalculatePositionRelativeToOrigo(Transform t, Vector3 position)
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
        c.type = "absorption";

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

        //UnityEngine.Debug.Log(assignedAcousticElements[0]);

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
    private float CalculateWallCoefficient(GameObject wall, List<GameObject> acousticElements)
    {
        float wallNrc = wall.GetComponent<AcousticElementDisplay>().acousticElement.nrc;
        float wallSurface = 0; 
        if (wall.name == "Floor" || wall.name == "Ceiling")
            wallSurface = wall.transform.lossyScale.x * wall.transform.lossyScale.z;
        else
            wallSurface = wall.transform.lossyScale.x * wall.transform.lossyScale.y;

        if (acousticElements == null)
            return wallNrc;

        float s = 0;
        float a = 0;
        foreach (GameObject element in acousticElements)
        {
            float acousticElementNrc= element.GetComponent<AcousticElementDisplay>().acousticElement.nrc;
            float acousticElementSurface = element.transform.lossyScale.x * element.transform.lossyScale.z;
            UnityEngine.Debug.Log(element.name + ": " + acousticElementSurface);
            s += acousticElementSurface;
            a += acousticElementNrc * acousticElementSurface;
        }
        return (wallNrc * (wallSurface - s) + a) / wallSurface;
        // I dont feel like the numbers add up here...
        // return (wallNrc * wallSurface + a) / (wallSurface + s);
        
    }
 
    public bool isHighPass;
    public int reflections;
    private void Update()
    {
        if (CalculateImpulseResponse)
        {
            CalculateImpulseResponse = false;

            Transform t = GameObject.FindGameObjectWithTag("Room").transform;
            Transform player = GameObject.FindGameObjectWithTag("MainCamera").transform;

            this.sourcePosition = CalculatePositionRelativeToOrigo(t, GameObject.FindGameObjectWithTag("Audio Source").transform.position);
            this.receiverPosition = CalculatePositionRelativeToOrigo(t, player.position);

            room.dimension = new Dimension(t.localScale.x, t.localScale.z, t.localScale.y);
            room.coefficients = CalculateCoefficients();
            //room.coefficients = new Coefficients(room.coefficients.frontWall, room.coefficients.backWall, room.coefficients.leftWall, room.coefficients.rightWall, room.coefficients.floor, room.coefficients.ceiling, room.coefficients.type);

            char microphone_type = 'b';

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            float[] imp = RirGenerator(this.receiverPosition, this.sourcePosition, this.room, microphone_type, reflections, isHighPass);
            stopwatch.Stop();
            UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);

            roomImpulseResponse = imp;
            newImpulseResponse = true;
            
            
            string s = "";
            foreach (float f in imp)
            {
                string temp = f.ToString().Replace(',', '.');
                s += temp + ";";
            }
            File.WriteAllText(Application.dataPath + "/output.txt", s);
            
        }
    }
}
