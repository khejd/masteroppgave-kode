using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleActive : MonoBehaviour
{
    public void ToggleObjectActive()
    {
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }

    public void SetObjectActive()
    {
        this.gameObject.SetActive(true);
    }
}
