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
        Vector3 position = player.position + transform.forward;
        if (Equals(prefab.name, "Carpet"))
            position = player.position - 0.9f * transform.up * room.localScale.y / 2 + transform.forward;
        if (Equals(prefab.name, "Audio Source"))
        {
            Instantiate(prefab, position, prefab.transform.rotation);
            return;
        }

        GameObject spawnedObject = Instantiate(prefab, position, prefab.transform.rotation, room);
        Vector3 newScale = new Vector3(spawnedObject.transform.localScale.x / room.localScale.x, spawnedObject.transform.localScale.y, spawnedObject.transform.localScale.z / room.localScale.z);
        spawnedObject.transform.localScale = newScale;
    }
}
