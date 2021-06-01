using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main <c>SimpleChangeRoom</c> class.
/// </summary>
public class SimpleChangeRoom : MonoBehaviour
{
    public AcousticElement initialWall;
    public AcousticElement initialFloor;
    public AcousticElement initialCeiling;

    public AcousticElement changedWall;
    public AcousticElement changedFloor;
    public AcousticElement changedCeiling;

    private bool isInitialRoom = true;
    /// <summary>
    /// Changes the design of the room.
    /// </summary>
    public void ChangeRoom()
    {
        GameObject.Find("Front Wall").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedWall : initialWall;
        GameObject.Find("Back Wall").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedWall : initialWall;
        GameObject.Find("Left Wall").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedWall : initialWall;
        GameObject.Find("Right Wall").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedWall : initialWall;
        GameObject.Find("Floor").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedFloor : initialFloor;
        GameObject.Find("Ceiling").GetComponent<AcousticElementDisplay>().acousticElement = isInitialRoom ? changedCeiling : initialCeiling;

        isInitialRoom = !isInitialRoom;

        GameObject.Find("Room Impulse Response").GetComponent<RoomImpulseResponseJob>().ToggleCalculateImpulseResponse();
    }

   
}
