using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcousticElementDisplay : MonoBehaviour
{
    public AcousticElement acousticElement;
    Renderer rend; 
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
