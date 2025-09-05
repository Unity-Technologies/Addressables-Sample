using System;
using UnityEngine;

public class Done_Mover : MonoBehaviour
{
	public float speed;

	void Start ()
	{
		GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;
	}
}
