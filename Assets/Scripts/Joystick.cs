using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Joystick : MonoBehaviour
{
    public Transform player;
    public float speed = 1.0f;
    private bool touchStart = false;
    private Vector2 pointA;
    private Vector2 pointB;

    public RectTransform circle;
    public RectTransform outerCircle;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointA = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);

            circle.transform.position = pointA;
            outerCircle.transform.position = pointA;
            circle.GetComponent<Image>().enabled = true;
            outerCircle.GetComponent<Image>().enabled = true;
        }
        if (Input.GetMouseButton(0))
        {
            touchStart = true;
            pointB = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
        else
        {
            touchStart = false;
        }

    }
    private void FixedUpdate()
    {
        if (touchStart)
        {
            Vector2 offset = pointB - pointA;
            Vector2 direction = Vector2.ClampMagnitude(offset, outerCircle.rect.width * outerCircle.transform.localScale.x / 2);
            moveCharacter(offset);

            circle.transform.position = new Vector2(pointA.x + direction.x, pointA.y + direction.y);
        }
        else
        {
            circle.GetComponent<Image>().enabled = false;
            outerCircle.GetComponent<Image>().enabled = false;
        }

    }
    void moveCharacter(Vector2 offset)
    {
        Vector2 direction = Vector2.ClampMagnitude(offset, 1.0f);
        player.Translate(new Vector3(direction.x, 0, direction.y) * speed * Time.deltaTime);
    }
}
