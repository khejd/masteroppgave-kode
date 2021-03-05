using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private bool isInitialRoom = true;

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

        distances = new List<float>(4);
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
        Vector3 player = GameObject.FindGameObjectWithTag("MainCamera").transform.position;
        distances.Add(Vector3.Distance(player, partner.position));

        foreach (float d in distances)
            Debug.Log(d);
    }

    public void ChangeRoom()
    {
        GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedCeiling : initialCeiling;

        isInitialRoom = !isInitialRoom;
        r.ToggleCalculateImpulseResponse();
    }

}
