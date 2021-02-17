using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
public class DropdownAcousticElement : MonoBehaviour
{
    public AcousticElement woodPanel;
    public AcousticElement plaster;
    public AcousticElement marble;
    public AcousticElement plywood;
    public AcousticElement concrete;
    public AcousticElement carpet;

    private TMP_Dropdown frontWallDropdown;
    private TMP_Dropdown backWallDropdown;
    private TMP_Dropdown leftWallDropdown;
    private TMP_Dropdown rightWallDropdown;
    private TMP_Dropdown floorDropdown;
    private TMP_Dropdown ceilingDropdown;

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

    private void ChangeWallElement(string wallName, int value)
    {

        Debug.Log(wallName + " " + GameObject.Find(wallName).name);

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
            default:
                break;
        }
    }

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
            default:
                break;
        }
    }

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
            default:
                break;
        }
    }


}
