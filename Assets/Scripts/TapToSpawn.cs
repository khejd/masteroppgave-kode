using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TapToSpawn : MonoBehaviour
{
    public GameObject prefab;

    private void Awake()
    {
        this.GetComponent<Button>().onClick.AddListener(Spawn);
    }

    public void Spawn()
    {
        Transform room = GameObject.FindGameObjectWithTag("Room").transform;
        Transform player = GameObject.FindGameObjectWithTag("MainCamera").transform;
        Vector3 position = player.position + 1.5f * transform.forward * Mathf.Cos((player.localEulerAngles.y + 3) * Mathf.Deg2Rad) + 1.5f * transform.right * Mathf.Sin(player.localEulerAngles.y * Mathf.Deg2Rad);
        if (Equals(prefab.name, "Carpet") || Equals(prefab.name, "Door"))
            position -=  0.5f * transform.up * room.localScale.y / 2;
        if (Equals(prefab.tag, "Audio Source"))
        {
            Instantiate(prefab, position, prefab.transform.rotation);
            GameObject.Find("Convolution").GetComponent<ConvolutionJob>().AddAudioSource(prefab.GetComponent<AudioSource>().clip);
            return;
        }

        GameObject spawnedObject = Instantiate(prefab, position, prefab.transform.rotation, room);
        Vector3 newScale = new Vector3(spawnedObject.transform.localScale.x / room.localScale.x, spawnedObject.transform.localScale.y / room.localScale.y, spawnedObject.transform.localScale.z / room.localScale.z);
        spawnedObject.transform.localScale = newScale;
    }
}
