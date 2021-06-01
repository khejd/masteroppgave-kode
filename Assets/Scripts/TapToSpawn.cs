using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The main <c>TapToSpawn</c> class.
/// </summary>
[RequireComponent(typeof(Button))]
public class TapToSpawn : MonoBehaviour
{
    /// <summary>
    /// The prefab object to be spawned.
    /// </summary>
    public GameObject prefab;

    /// <summary>
    /// Dropdown menu for selecting a wall to assign the prefab.
    /// </summary>
    public TMP_Dropdown wallDropdown;

    /// <summary>
    /// Adds event listener on button click to run method <c>Spawn</c> on awake.
    /// </summary>
    private void Awake()
    {
        this.GetComponent<Button>().onClick.AddListener(Spawn);
    }

    /// <summary>
    /// Game object of the last spawned prefab.
    /// </summary>
    private GameObject lastSpawned;

    /// <summary>
    /// Spawns the prefab to the scene.
    /// </summary>
    public void Spawn()
    {
        Transform room = GameObject.FindGameObjectWithTag("Room").transform;
        Transform player = GameObject.FindGameObjectWithTag("MainCamera").transform;
        Vector3 position = player.position + 1.5f * transform.forward * Mathf.Cos((player.localEulerAngles.y + 3) * Mathf.Deg2Rad) + 1.5f * transform.right * Mathf.Sin(player.localEulerAngles.y * Mathf.Deg2Rad) + 0.5f * transform.right;
        if (Equals(prefab.name, "Carpet") || Equals(prefab.name, "Door"))
        {
            Transform floor = GameObject.Find("Floor").transform;
            position = floor.position + 2 * transform.up * floor.localScale.y;
        }

        if (Equals(prefab.tag, "Audio Source"))
        {
            Instantiate(prefab, position, prefab.transform.rotation);
            GameObject.Find("Convolution").GetComponent<ConvolutionJob>().AddAudioSource(prefab.GetComponent<AudioSource>().clip);
            return;
        }

        lastSpawned = Instantiate(prefab, position, prefab.transform.rotation, room);
        Vector3 newScale = new Vector3(lastSpawned.transform.localScale.x / room.localScale.x, lastSpawned.transform.localScale.y / room.localScale.y, lastSpawned.transform.localScale.z / room.localScale.z);
        lastSpawned.transform.localScale = newScale;
    }

    /// <summary>
    /// Attaches the spawned object to a wall.
    /// </summary>
    public void AttachToWall()
    {
        Vector3 pos = new Vector3();
        Quaternion rot1 = new Quaternion();
        Quaternion rot2 = new Quaternion();

        rot1.eulerAngles = new Vector3(0, 0, 0);
        rot2.eulerAngles = new Vector3(0, 90, 0);

        switch (wallDropdown.value)
        {
            case 0: break;
            case 1: 
                Transform frontWall = GameObject.Find("Front Wall").transform; 
                pos = frontWall.position - transform.forward * frontWall.lossyScale.z; 
                lastSpawned.transform.rotation = rot1; 
                break;
            case 2: 
                Transform backWall = GameObject.Find("Back Wall").transform; 
                pos = backWall.position + transform.forward * backWall.lossyScale.z; 
                lastSpawned.transform.rotation = rot1; 
                break;
            case 3: 
                Transform leftWall = GameObject.Find("Left Wall").transform; 
                pos = leftWall.position + transform.right * leftWall.lossyScale.z; 
                lastSpawned.transform.rotation = rot2; 
                break;
            case 4: 
                Transform rightWall = GameObject.Find("Right Wall").transform; 
                pos = rightWall.position - transform.right * rightWall.lossyScale.z; 
                lastSpawned.transform.rotation = rot2; 
                break;
            default: 
                break;
        }
        lastSpawned.transform.position = pos;
    }
}
