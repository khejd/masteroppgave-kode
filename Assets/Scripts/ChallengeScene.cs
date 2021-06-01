using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The main <c>ChallengeScene</c> class.
/// </summary>
public class ChallengeScene : MonoBehaviour
{
    // GUI elements in the scene
    public GameObject addButton;
    public GameObject optionsButton;
    public GameObject menuButton;
    public TextMeshProUGUI reverberationTime;
    public AcousticElement challengeMaterial;
    public GameObject surveyPanel;

    public TMP_Dropdown FW, BW, LW, RW, F, C;

    private int challengeNo = 1;
    private float initReverberationTime = -1;

    /// <summary>
    /// Flashing animation for the current task text.
    /// The animation goes from neon green and big text to smaller and white text.
    /// </summary>
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

    /// <summary>
    /// Flag for starting/stopping the pulse animation.
    /// </summary>
    private bool keepGoing;
    /// <summary>
    /// Controls the <c>keepGoing</c> flag.
    /// </summary>
    public void SetKeepGoingFalse()
    {
        this.keepGoing = false;
        addButton.transform.localScale = Vector3.one;
        optionsButton.transform.localScale = Vector3.one;
    }
    /// <summary>
    /// Pulsing animation for button.
    /// </summary>
    /// <param name="button">The button to pulse</param>
    private IEnumerator Pulse(GameObject button)
    {
         // Grow parameters
        float approachSpeed = 0.02f;
        float growthBound = 1.2f;
        float shrinkBound = 0.8f;
        float currentRatio = 1;

        while (keepGoing)
        {
            while (currentRatio != growthBound)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, growthBound, approachSpeed);
                button.transform.localScale = Vector3.one * currentRatio;
                yield return new WaitForEndOfFrame();
            }
            while (currentRatio != shrinkBound)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, shrinkBound, approachSpeed);
                button.transform.localScale = Vector3.one * currentRatio;
                yield return new WaitForEndOfFrame();
            }
        }
    }
    /// <summary>
    /// Starts a one minute count down.
    /// </summary>
    private IEnumerator CountDown()
    {
        int start = 60;
        while (start > 0)
        {
            yield return new WaitForSecondsRealtime(1);
            start--;
        }
        NextChallenge();
    }

    /// <summary>
    /// Changes the current task.
    /// </summary>
    public void NextChallenge()
    {
        switch (challengeNo)
        {
            // Add audio source finished
            case 1:
                optionsButton.SetActive(true);
                keepGoing = true; 
                StartCoroutine(Pulse(optionsButton)); 
                helpText = "Change front and back wall material to brick."; 
                break;
            // Change room materials finished
            case 2: 
                addButton.SetActive(true);
                keepGoing = true;
                StartCoroutine(Pulse(addButton));
                optionsButton.SetActive(false);
                break;
            // Reduce reverberation time finished
            case 3:
                helpText = "You now have one minute to explore...";
                optionsButton.SetActive(true); 
                FW.interactable = true;
                BW.interactable = true;
                LW.interactable = true;
                RW.interactable = true;
                F.interactable = true;
                C.interactable = true;
                StartCoroutine(CountDown());
                break;
            case 4:
                helpText = "You have successfully finished the scene.";
                surveyPanel.SetActive(true);
                menuButton.SetActive(true);
                MuteAll(); 
                break;
            default: break;
        }
        challengeDescription.text = helpText;
        StartCoroutine(FlashTextAnimation());
        challengeNo++;
    }

    /// <summary>
    /// The <c>help text</c> pane on top of the screen.
    /// </summary>
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
            {
                GameObject.Find("Room Impulse Response").GetComponent<RoomImpulseResponseJob>().ToggleCalculateImpulseResponse();
                FW.interactable = false;
                BW.interactable = false;
                NextChallenge();
            }

        }
        else if (challengeNo == 3)
        {
            if (initReverberationTime < 0)
                initReverberationTime = float.Parse(reverberationTime.text.Split(' ')[2]);
            float threshold = 2 * initReverberationTime / 3.0f;
            helpText = "Get the reverberation time below " + threshold.ToString("F2") + " seconds";
            challengeDescription.text = helpText;
            if (float.Parse(reverberationTime.text.Split(' ')[2]) <= threshold)
            {
                NextChallenge();
            }
        }
    }

    /// <summary>
    /// Mutes all audio in the scene.
    /// </summary>
    /// <param name="mute">Mute (<c>true</c>) or unmute (<c>false</c>)</param>
    public void MuteAll(bool mute = true)
    {
        foreach (GameObject a in GameObject.FindGameObjectsWithTag("Audio Source"))
            a.GetComponent<AudioSource>().mute = mute;
    }
}
