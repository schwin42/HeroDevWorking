using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VoxelManager : MonoBehaviour
{
	
	//Configuration
	private const float ROTATION_SPEED = 40f;
	private const bool ROTATION_ENABLED = false;
	public Rigidbody Rigidbody;
	
	//State
	public Enemy Owner { get; set; } //Owner here means the whole of which the voxel is a part
	private bool _isInitialized = false;

	public void Initialize(Enemy owner)
	{
		Rigidbody = GetComponent<Rigidbody>();
		this.Owner = owner;
		_isInitialized = true;
	}

	private void Start()
	{
		if (!_isInitialized)
		{
			Initialize(null);
		}
	}
	
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
		if (this.Owner != null)
		{
			this.Owner.TakeDamage(damage);
		}

		
		Rigidbody.isKinematic = false;
		Rigidbody.AddForce(damageOrigin.forward * blastForce);
		Rigidbody.AddTorque(new Vector3(Random.value, Random.value, Random.value) * blastForce);

	}
}
