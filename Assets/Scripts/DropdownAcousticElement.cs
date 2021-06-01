﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

/// <summary>
/// The main <c>DropdownAcousticElement</c> class.
/// Contains all methods for changing acoustic elements on surfaces.
/// </summary>
public class DropdownAcousticElement : MonoBehaviour
{
    /* 
     * The optional materials in the dropdown
     */
  
    public AcousticElement woodPanel;
    public AcousticElement plaster;
    public AcousticElement marble;
    public AcousticElement plywood;
    public AcousticElement concrete;
    public AcousticElement carpet;
    public AcousticElement brick;
    public AcousticElement metal;
    public AcousticElement acousticRoofPanel;

    /* 
     * The dropdowns
     */
    private TMP_Dropdown frontWallDropdown;
    private TMP_Dropdown backWallDropdown;
    private TMP_Dropdown leftWallDropdown;
    private TMP_Dropdown rightWallDropdown;
    private TMP_Dropdown floorDropdown;
    private TMP_Dropdown ceilingDropdown;

    /// <summary>
    /// Finds and assigns the dropdowns to event listeners.
    /// </summary>
    private void Awake()
    {
        List<TMP_Dropdown> dropdowns = Resources.FindObjectsOfTypeAll<TMP_Dropdown>().ToList();

        frontWallDropdown = dropdowns.Find(d => Equals(d.name, "Front Wall Dropdown"));
        backWallDropdown = dropdowns.Find(d => Equals(d.name, "Back Wall Dropdown"));
        leftWallDropdown = dropdowns.Find(d => Equals(d.name, "Left Wall Dropdown"));
        rightWallDropdown = dropdowns.Find(d => Equals(d.name, "Right Wall Dropdown"));
        floorDropdown = dropdowns.Find(d => Equals(d.name, "Floor Dropdown"));
        ceilingDropdown = dropdowns.Find(d => Equals(d.name, "Ceiling Dropdown"));

        frontWallDropdown.onValueChanged.AddListener(delegate { ChangeWallElement("Front Wall", frontWallDropdown.value); });
        backWallDropdown.onValueChanged.AddListener(delegate { ChangeWallElement("Back Wall", backWallDropdown.value); });
        leftWallDropdown.onValueChanged.AddListener(delegate { ChangeWallElement("Left Wall", leftWallDropdown.value); });
        rightWallDropdown.onValueChanged.AddListener(delegate { ChangeWallElement("Right Wall", rightWallDropdown.value); });
        floorDropdown.onValueChanged.AddListener(delegate { ChangeFloorElement(); });
        ceilingDropdown.onValueChanged.AddListener(delegate { ChangeCeilingElement(); });

    }

    /// <summary>
    /// Changes the wall element of a wall given by its name.
    /// </summary>
    /// <param name="wallName">Name of the wall</param>
    /// <param name="value">Value of the element in the dropdown</param>
    private void ChangeWallElement(string wallName, int value)
    {
        switch (value)
        {
            case 0:
                GameObject.Find(wallName).GetComponent<AcousticElementDisplay>().acousticElement = woodPanel;
                break;
            case 1:
                GameObject.Find(wallName).GetComponent<AcousticElementDisplay>().acousticElement = plaster;
                break;
            case 2:
                GameObject.Find(wallName).GetComponent<AcousticElementDisplay>().acousticElement = concrete;
                break;
            case 3:
                GameObject.Find(wallName).GetComponent<AcousticElementDisplay>().acousticElement = brick;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Changes the floor element.
    /// </summary>
    private void ChangeFloorElement()
    {
        switch (floorDropdown.value)
        {
            case 0:
                GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = marble;
                break;
            case 1:
                GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = plywood;
                break;
            case 2:
                GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = concrete;
                break;
            case 3:
                GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = carpet;
                break;
            case 4:
                GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = metal;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Changes the ceiling element.
    /// </summary>
    private void ChangeCeilingElement()
    {
        switch (ceilingDropdown.value)
        {
            case 0:
                GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = plaster;
                break;
            case 1:
                GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = concrete;
                break;
            case 2:
                GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = acousticRoofPanel;
                break;
            default:
                break;
        }
    }


}
