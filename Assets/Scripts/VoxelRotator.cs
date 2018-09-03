using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VoxelRotator : MonoBehaviour
{
	
	//Configuration
	private const float ROTATION_SPEED = 40f;
	private const bool ROTATION_ENABLED = false;
	public Rigidbody Rigidbody;
	
	//State
	private Enemy _owner;

	public void Initialize(Enemy owner)
	{
		_owner = owner;
		Rigidbody = GetComponent<Rigidbody>();
	}

	private void Start() { return; } //Use initializer instead
	
	// Update is called once per frame
	private void Update()
	{
		if (ROTATION_ENABLED)
		{
			// Rotate the object around its local X axis at 1 degree per second
			transform.Rotate(Vector3.right * Time.deltaTime * ROTATION_SPEED);

			// ...also rotate around the World's Y axis
			transform.Rotate(Vector3.up * Time.deltaTime * ROTATION_SPEED);
		}
	}

	public void TakeDamage(float damage, Transform damageOrigin, float blastForce)
	{
		_owner.TakeDamage(damage);
		
		Rigidbody.isKinematic = false;
		Rigidbody.AddForce(damageOrigin.forward * blastForce);
		Rigidbody.AddTorque(new Vector3(Random.value, Random.value, Random.value) * blastForce);

	}
}
