using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ChallengeScene : MonoBehaviour
{
    public GameObject addButton;
    public GameObject optionsButton;
    public GameObject menuButton;
    public TextMeshProUGUI reverberationTime;
    public AcousticElement challengeMaterial;
    public GameObject surveyPanel;

    private int challengeNo = 1;
    private float initReverberationTime = -1;

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

    public void NextChallenge()
    {
        switch (challengeNo)
        {
            // Add audio source finished
            case 1: optionsButton.SetActive(true); helpText = "Change front- and back wall material to brick."; break;
            // Change room materials finished
            case 2: addButton.SetActive(true); break;
            // Reduce reverberation time finished
            case 3: menuButton.SetActive(true); surveyPanel.SetActive(true); helpText = "You have successfully finished the scene."; MuteAll(); break;
            default: break;
        }
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
        challengeNo++;
    }

    public TextMeshProUGUI challengeDescription;
    private string helpText = "Add an audio source to the scene.";

    private void Awake()
    {
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
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
            if (GameObject.Find("Front Wall").GetComponent<AcousticElementDisplay>().acousticElement == challengeMaterial && GameObject.Find("Back Wall").GetComponent<AcousticElementDisplay>().acousticElement == challengeMaterial)
                NextChallenge();

        }
        else if (challengeNo == 3)
        {
            if (initReverberationTime < 0)
                initReverberationTime = float.Parse(reverberationTime.text.Split(' ')[2]);
            float threshold = 2 * initReverberationTime / 3.0f;
            helpText = "Get the reverberation time below " + threshold.ToString("F2") + " seconds";
            challengeDescription.text = helpText;
            if (float.Parse(reverberationTime.text.Split(' ')[2]) <= threshold)
                NextChallenge();
        }
    }

    public void MuteAll(bool mute = true)
    {
        foreach (GameObject a in GameObject.FindGameObjectsWithTag("Audio Source"))
            a.GetComponent<AudioSource>().mute = mute;
    }
}
