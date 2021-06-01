using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main <c>AcousticElementDisplay</c> class.
/// Displays the acoustic element on the assigned object.
/// </summary>
public class AcousticElementDisplay : MonoBehaviour
{
    /// <summary>
    /// The acoustic element to be rendered.
    /// </summary>
    public AcousticElement acousticElement;

    /// <summary>
    /// The assigned object's renderer.
    /// </summary>
    private Renderer rend; 
    private void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = acousticElement.material;
    }

    private void Update()
    {
        rend.material = acousticElement.material;
    }
}
