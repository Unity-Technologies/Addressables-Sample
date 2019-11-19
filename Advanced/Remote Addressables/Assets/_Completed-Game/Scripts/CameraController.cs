using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	// store a public reference to the Player game object, so we can refer to it's Transform
	public GameObject player;

	// Store a Vector3 offset from the player (a distance to place the camera from the player at all times)
	private Vector3 offset;

	// At the start of the game..
	void Start ()
	{
		// Create an offset by subtracting the Camera's position from the player's position
		offset = transform.position - player.transform.position;
	}

	// After the standard 'Update()' loop runs, and just before each frame is rendered..
	void LateUpdate ()
	{
		// Set the position of the Camera (the game object this script is attached to)
		// to the player's position, plus the offset amount
		transform.position = player.transform.position + offset;
	}
}