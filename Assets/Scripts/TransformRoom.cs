using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TransformRoom : MonoBehaviour
{
    private Transform frontWall;
    private Transform backWall;
    private Transform leftWall;
    private Transform rightWall;
    private Transform floor;
    private Transform ceiling;

    void Start()
    {
        frontWall = gameObject.transform.Find("Front Wall");
        backWall = gameObject.transform.Find("Back Wall");
        leftWall = gameObject.transform.Find("Left Wall");
        rightWall = gameObject.transform.Find("Right Wall");
        floor = gameObject.transform.Find("Floor");
        ceiling = gameObject.transform.Find("Ceiling");
    }

    // Update is called once per frame
    void Update()
    {
        frontWall = gameObject.transform.Find("Front Wall");
        backWall = gameObject.transform.Find("Back Wall");
        leftWall = gameObject.transform.Find("Left Wall");
        rightWall = gameObject.transform.Find("Right Wall");
        floor = gameObject.transform.Find("Floor");
        ceiling = gameObject.transform.Find("Ceiling");

        Vector3 c = findCenterPosition();
        transform.localPosition = c;
    }

    private Vector3 findCenterPosition()
    {
        Vector3 center = new Vector3();

        center.x = leftWall.position.x - rightWall.position.x;
        center.y = ceiling.position.y - floor.position.y;
        center.z = frontWall.position.z - backWall.position.z;

        return center;
    }
}
