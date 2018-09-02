using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelRotator : MonoBehaviour
{
	private const float ROTATION_SPEED = 40f;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		// Rotate the object around its local X axis at 1 degree per second
		transform.Rotate(Vector3.right * Time.deltaTime * ROTATION_SPEED);

		// ...also rotate around the World's Y axis
		transform.Rotate(Vector3.up * Time.deltaTime * ROTATION_SPEED);
	}
}
