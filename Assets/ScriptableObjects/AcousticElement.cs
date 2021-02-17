using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Acoustic Element", menuName = "Acoustic Element")]
public class AcousticElement : ScriptableObject
{
    public new string name;
    public float nrc;
    public Material material;
}
