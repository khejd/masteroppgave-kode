using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main <c>ToggleActive</c> class.
/// </summary>
public class ToggleActive : MonoBehaviour
{
    /// <summary>
    /// Toggles the object's active state.
    /// </summary>
    public void ToggleObjectActive()
    {
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }

    /// <summary>
    /// Sets the object's active state to true.
    /// </summary>
    public void SetObjectActive()
    {
        this.gameObject.SetActive(true);
    }
}
