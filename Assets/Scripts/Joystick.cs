using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The main <c>Joystick</c> class.
/// </summary>
public class Joystick : MonoBehaviour
{
    public Transform player;
    public float speed = 1.0f;
    private bool touchStart = false;

    /// <summary>
    /// First touch position.
    /// </summary>
    private Vector2 pointA;
    /// <summary>
    /// Second touch position.
    /// </summary>
    private Vector2 pointB;

    private Vector2 initPosition;

    // GUI elements
    public RectTransform circle;
    public RectTransform outerCircle;

    private void Start()
    {
        initPosition = circle.transform.position;
        circle.GetComponent<Image>().enabled = true;
        outerCircle.GetComponent<Image>().enabled = true;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointA = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);

            circle.transform.position = pointA;
            outerCircle.transform.position = pointA;
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
            MoveCharacter(offset);

            circle.transform.position = new Vector2(pointA.x + direction.x, pointA.y + direction.y);
        }
        else
        {
            circle.transform.position = initPosition;
            outerCircle.transform.position = initPosition;
        }

    }
    /// <summary>
    /// Moves the caracter with the given offset.
    /// </summary>
    /// <param name="offset">Offset to move the character</param>
    private void MoveCharacter(Vector2 offset)
    {
        Vector2 direction = Vector2.ClampMagnitude(offset, 1.0f);
        direction *= speed * Time.deltaTime;
        player.Translate(direction.x, 0, direction.y);
        player.position = new Vector3(player.position.x, 0, player.position.z);  
     
    }
}
