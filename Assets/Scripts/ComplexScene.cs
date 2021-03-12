using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class ComplexScene : MonoBehaviour
{
    public GameObject audio1Prefab;
    public GameObject audio2Prefab;
    public GameObject scenePrefab;
    public GameObject partnerPrefab;

    public AcousticElement initialCeiling;
    public AcousticElement changedCeiling;

    private Vector3 audio1;
    private Vector3 audio2;
    private Vector3 scene;
    private Transform partner;

    private int room = 0;

    private ConvolutionJob c;
    private RoomImpulseResponseJob r;

    private void Awake()
    {
        audio1 = GameObject.Find("Audio1").transform.position;
        audio2 = GameObject.Find("Audio2").transform.position;
        scene = GameObject.Find("Scene").transform.position;
        partner = GameObject.Find("Partner").transform;

        c = GameObject.Find("Convolution").GetComponent<ConvolutionJob>();
        r = GameObject.Find("Room Impulse Response").GetComponent<RoomImpulseResponseJob>();

        distances = new List<float>(4) { 0, 0, 0, 0 };
    }
    public void AddAudio()
    {
        Instantiate(audio1Prefab, audio1, audio1Prefab.transform.rotation);
        Instantiate(audio2Prefab, audio2, audio2Prefab.transform.rotation);
        Instantiate(scenePrefab, scene, scenePrefab.transform.rotation);
        Instantiate(partnerPrefab, partner.position + transform.up * partner.lossyScale.y / 2 + transform.forward * partner.lossyScale.z / 2, partnerPrefab.transform.rotation);

        ConvolutionJob c = GameObject.Find("Convolution").GetComponent<ConvolutionJob>();
        c.AddAudioSource(audio1Prefab.GetComponent<AudioSource>().clip);
        c.AddAudioSource(audio2Prefab.GetComponent<AudioSource>().clip);
        c.AddAudioSource(scenePrefab.GetComponent<AudioSource>().clip);
        c.AddAudioSource(partnerPrefab.GetComponent<AudioSource>().clip);

        r.ToggleCalculateImpulseResponse();
    }

    private List<float> distances;
    public void RegisterDistance()
    {
        Vector3 player = GameObject.Find("AR Camera").transform.position;
        distances[room] = Vector3.Distance(player, partner.position);
    }

    public GameObject panel;
    public TextMeshProUGUI room1AR;
    public TextMeshProUGUI room1NoAR;
    public TextMeshProUGUI room2AR;
    public TextMeshProUGUI room2NoAR;

    public GameObject prefab;
    public void ChangeRoom()
    {
        room++;

        GameObject.Find("AR Camera").transform.position = new Vector3(0, 0, 0);
        if (room == 1)
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = changedCeiling;
        else if (room == 2)
        {
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = initialCeiling;
            GameObject cam = GameObject.Find("AR Camera");
            cam.GetComponent<Camera>().enabled = false;
            cam.GetComponent<ARPoseDriver>().enabled = false;
            cam.GetComponent<ARCameraManager>().enabled = false;

            Instantiate(prefab, cam.transform, false);


            GameObject.Find("Top View Camera").GetComponent<Camera>().enabled = true;
            gameObject.GetComponent<Joystick>().enabled = true;
        }
        else if (room == 3)
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = changedCeiling;
        else if (room == 4)
        {
            this.panel.SetActive(true);
            this.room1AR.text = "Room 1: " + distances[0].ToString("F2") + "m";
            this.room1NoAR.text = "Room 1: " + distances[2].ToString("F2") + "m";
            this.room2AR.text = "Room 2: " + distances[1].ToString("F2") + "m";
            this.room2NoAR.text = "Room 2: " + distances[3].ToString("F2") + "m";
            return;
        }

        r.ToggleCalculateImpulseResponse();
    }

}
