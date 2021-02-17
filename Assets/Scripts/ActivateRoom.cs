using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ActivateRoom : MonoBehaviour
{
    private Toggle activeRoomToggle;
    private ARTapToPlaceObject arTapToPlaceObject;
    private void Awake()
    {
        List<Toggle> togglers = Resources.FindObjectsOfTypeAll<Toggle>().ToList();
        activeRoomToggle = togglers.Find(t => Equals(t.tag, "Room"));
        arTapToPlaceObject = GameObject.Find("AR Session Origin").GetComponent<ARTapToPlaceObject>();

        activeRoomToggle.onValueChanged.AddListener(delegate { ToggleRoomActive(); });
    }

    private void ToggleRoomActive()
    {
        if (activeRoomToggle.isOn)
        {
            this.GetComponent<Lean.Touch.LeanSelectable>().Select();
            arTapToPlaceObject.enabled = true;
        }
        else
        {
            this.GetComponent<Lean.Touch.LeanSelectable>().Deselect();
            arTapToPlaceObject.enabled = false;
        }
    }

}
