using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class SimpleScene : MonoBehaviour
{
    public GameObject addAudioSourceButton;
    public GameObject changeRoomButton;
    public GameObject nextSceneButton;
    public TextMeshProUGUI challengeDescription;
    public GameObject surveyPanel;

    private int challengeNo = 1;

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

    private IEnumerator WaitTimeNextChallenge()
    {
        for (int i = 10; i > 0; i--)
        {
            helpText = "Explore the room by walking around. " + i.ToString();
            challengeDescription.text = helpText;
            yield return new WaitForSeconds(1);
        }
        changeRoomButton.SetActive(true);
        NextChallenge();
    }
    private void NextChallenge()
    {
        if (challengeNo == 1)
        {
            helpText = "Explore the room by walking around.";
            addAudioSourceButton.SetActive(false);
            StartCoroutine(WaitTimeNextChallenge());
        }
        else if (challengeNo == 2)
        {
            helpText = "Change room to experience the change in acoustics.";
        }
        else if (challengeNo == 3)
        {
            changeRoomButton.GetComponent<Button>().onClick.RemoveListener(NextChallenge);
            nextSceneButton.SetActive(true);
            helpText = "Press 'Next scene' button.";
        }
        else if (challengeNo == 4)
        {
            helpText = "Please fill out survey.";
            MuteAll();
            surveyPanel.SetActive(true);
        }
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
        challengeNo++;
    }

    private void MuteAll(bool mute = true)
    {
        foreach (GameObject a in GameObject.FindGameObjectsWithTag("Audio Source"))
            a.GetComponent<AudioSource>().mute = mute;
    }

    private string helpText = "Add audio source to the scene";
    private void Awake()
    {
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
        changeRoomButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
        nextSceneButton.GetComponent<Button>().onClick.AddListener(NextChallenge);
    }
    private void Update()
    {
        if (challengeNo == 1)
        {
            if (GameObject.FindGameObjectWithTag("Audio Source"))
                NextChallenge();
        }
    }
}
