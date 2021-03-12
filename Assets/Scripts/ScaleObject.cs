using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class ScaleObject : MonoBehaviour
{
    private Slider scaleWidth;
    private Slider scaleLength;
    private Slider scaleHeight;

    private float yPosInit;

    private void Awake()
    {
        List<Slider> sliders = Resources.FindObjectsOfTypeAll<Slider>().ToList();
        yPosInit = this.transform.position.y;

        scaleWidth = sliders.Find(s => Equals(s.name, "Room Width Slider"));
        scaleLength = sliders.Find(s => Equals(s.name, "Room Length Slider"));
        scaleHeight = sliders.Find(s => Equals(s.name, "Room Height Slider"));

        scaleWidth.onValueChanged.AddListener(delegate { ScaleObjectWidth(); });
        scaleLength.onValueChanged.AddListener(delegate { ScaleObjectLength(); });
        scaleHeight.onValueChanged.AddListener(delegate { ScaleObjectHeight(); });

        scaleWidth.gameObject.SetActive(true);
        scaleLength.gameObject.SetActive(true);
        scaleHeight.gameObject.SetActive(true);
    }

    private void ScaleObjectWidth()
    {
        UpdateSliderText(scaleWidth);
        transform.localScale = new Vector3(scaleWidth.value, transform.localScale.y, transform.localScale.z);
    }
    private void ScaleObjectLength()
    {
        UpdateSliderText(scaleLength);
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, scaleLength.value);
    }
    private void ScaleObjectHeight()
    {
        UpdateSliderText(scaleHeight);
        //float yPos = GameObject.Find("AR Session Origin").GetComponent<ARTapToPlaceObject>().hits[0].pose.position.y;
        float yPos = GameObject.Find("AR Session Origin").GetComponent<ARTapToPlaceObject>().hitPose.position.y;
        transform.localScale = new Vector3(transform.localScale.x, scaleHeight.value, transform.localScale.z);
        transform.position = new Vector3(transform.position.x, yPos + transform.localScale.y / 2, transform.position.z);
    }

    private void UpdateSliderText(Slider slider, string postfix = "m", string value = "")
    {
        TextMeshProUGUI[] textObjects = slider.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI t in textObjects)
        {
            if (Equals(t.tag, "Value"))
            {
                if (value.Equals(""))
                {
                    t.text = slider.value.ToString("F1") + postfix;
                    return;
                }
                t.text = value + postfix;
            }
        }
    }
}
