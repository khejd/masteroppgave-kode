using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
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
    private Vector3 partner;

    private int challengeNo = 1;

    private ConvolutionJob c;
    private RoomImpulseResponseJob r;

    public TextMeshProUGUI challengeDescription;
    private string helpText = "Add audio to scene.";
    private void Awake()
    {
        audio1 = GameObject.Find("Audio1").transform.position;
        audio2 = GameObject.Find("Audio2").transform.position;
        scene = GameObject.Find("Scene").transform.position;
        partner = GameObject.Find("Meghan@Sitting").transform.position;

        c = GameObject.Find("Convolution").GetComponent<ConvolutionJob>();
        r = GameObject.Find("Room Impulse Response").GetComponent<RoomImpulseResponseJob>();

        distances = new List<float>(4) { 0, 0, 0, 0 };
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
    }
    public void AddAudio()
    {
        Instantiate(audio1Prefab, audio1, audio1Prefab.transform.rotation);
        Instantiate(audio2Prefab, audio2, audio2Prefab.transform.rotation);
        Instantiate(scenePrefab, scene, scenePrefab.transform.rotation);
        Instantiate(partnerPrefab, partner + transform.up * partnerPrefab.transform.lossyScale.y / 2, partnerPrefab.transform.rotation);

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
        distances[room] = Vector3.Distance(player, partner);
    }

    public GameObject nextScenePanel;
    public TextMeshProUGUI room1AR;
    public TextMeshProUGUI room1NoAR;
    public TextMeshProUGUI room2AR;
    public TextMeshProUGUI room2NoAR;

    public GameObject prefab;

    public GameObject registerDistanceButton;
    public GameObject surveyPanel;
    private void NextChallenge()
    {
        if (challengeNo == 1)
        {
            helpText = "Walk closer to your guest.";
        }
        else if (challengeNo == 2)
        {
            helpText = "Register distance when you feel close enough.";
            registerDistanceButton.SetActive(true);
            registerDistanceButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
        }
        else if (challengeNo == 3)
        {
            helpText = "Distance registered. You can now change the room.";
            registerDistanceButton.GetComponent<Button>().onClick.RemoveListener(NextChallenge);
        }
        else if (challengeNo == 4)
        {
            helpText = "Register distance when you feel close enough.";
            registerDistanceButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
        }
        else if (challengeNo == 5)
        {
            helpText = "Distance registered. You can now change the room.";
            registerDistanceButton.GetComponent<Button>().onClick.RemoveListener(NextChallenge);
        }
        else if (challengeNo == 6)
        {
            helpText = "Register distance when you feel close enough.";
            registerDistanceButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
            surveyPanel.SetActive(true);
            MuteAll();
        }
        else if (challengeNo == 7)
        {
            helpText = "Distance registered. You can now change the room.";
            registerDistanceButton.GetComponent<Button>().onClick.RemoveListener(NextChallenge);
        }
        else if (challengeNo == 8)
        {
            helpText = "Register distance when you feel close enough.";
            registerDistanceButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
        }
        else if (challengeNo == 9)
        {
            helpText = "Distance registered. You can now change the room.";
            registerDistanceButton.GetComponent<Button>().onClick.RemoveListener(NextChallenge);
        }
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
        challengeNo++;
    }
    private IEnumerator FlashTextAnimation()
    {
        Color startColor = challengeDescription.color;
        Color32 flashColor = new Color32(11, 232, 129, 255);
        float stopSize = challengeDescription.fontSize;
        float deltaSize = 10;
        float stepSize = 0.5f;
        float startSize = stopSize + deltaSize;
        float flashTime = 0.5f;

        challengeDescription.fontSize = startSize;
        challengeDescription.color = flashColor;

        while (stopSize < startSize)
        {
            challengeDescription.fontSize = startSize;
            yield return new WaitForSeconds(flashTime / deltaSize * stepSize);
            startSize -= stepSize;
        }
        challengeDescription.fontSize = stopSize;

        challengeDescription.color = startColor;

    }

    private int room = 0;
    public void ChangeRoom()
    {
        room++;
        if (room == 1)
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = changedCeiling;
        else if (room == 2)
        {
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = initialCeiling;
            GameObject cam = GameObject.Find("AR Camera");
            cam.transform.position = new Vector3(0, 0, 0);
            cam.GetComponent<Camera>().enabled = false;
            cam.GetComponent<ARPoseDriver>().enabled = false;
            cam.GetComponent<ARCameraManager>().enabled = false;

            Instantiate(prefab, cam.transform, false);

            GameObject.Find("Top View Camera").GetComponent<Camera>().enabled = true;
            gameObject.GetComponent<Joystick>().enabled = true;
        }
        else if (room == 3)
        {
            GameObject.Find("AR Camera").transform.position = new Vector3(0, 0, 0);
            GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = changedCeiling;
        }
        else if (room == 4)
        {
            MuteAll();
            this.nextScenePanel.SetActive(true);
            this.room1AR.text = "Room 1: " + distances[0].ToString("F2") + "m";
            this.room1NoAR.text = "Room 1: " + distances[2].ToString("F2") + "m";
            this.room2AR.text = "Room 2: " + distances[1].ToString("F2") + "m";
            this.room2NoAR.text = "Room 2: " + distances[3].ToString("F2") + "m";
            return;
        }
        NextChallenge();
        r.ToggleCalculateImpulseResponse();
    }

    public void MuteAll(bool mute = true)
    {
        foreach (GameObject a in GameObject.FindGameObjectsWithTag("Audio Source"))
            a.GetComponent<AudioSource>().mute = mute;
    }

    private void Update()
    {
        if (challengeNo == 1)
        {
            if (GameObject.FindGameObjectWithTag("Audio Source"))
                NextChallenge();
        }
        else if (challengeNo == 2)
        {
            Vector3 player = GameObject.Find("AR Camera").transform.position;
            if (Vector3.Distance(player, partner) <= 6)
                NextChallenge();
        }
    }

}
