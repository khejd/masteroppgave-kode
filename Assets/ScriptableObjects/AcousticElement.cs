using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main <c>AcousticElement</c> class.
/// </summary>
[CreateAssetMenu(fileName = "New Acoustic Element", menuName = "Acoustic Element")]
public class AcousticElement : ScriptableObject
{
    /// <summary>
    /// Name of the acoustic element
    /// </summary>
    public new string name;
    /// <summary>
    /// The NRC value of the acoustic element
    /// </summary>
    public float nrc;
    /// <summary>
    /// Material assigned to the acoustic element
    /// </summary>
    public Material material;
}
