using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Enemy : MonoBehaviour
{
	private const float MOVEMENT_SPEED = 10;
	
	[SerializeField]
	private float _maxHealth = 10;
	private float currentHealth;

	private const float EXPLOSION_FORCE = 1000f;
	private const float EXPLOSION_RADIUS = 1f;
	private const float DISPOSAL_TIME = 1.5f;

	private VoxelRotator[] body;
	
	[SerializeField]
	private ParticleSystem explosion;
	private Light _coreLight;
	
	//State
	private bool _isMoving = true;
	
	// Use this for initialization
	void Start ()
	{
		body = GetComponentsInChildren<VoxelRotator>();
		foreach (VoxelRotator voxel in body)
		{
			voxel.Initialize(this);
		}

		_coreLight = GetComponentInChildren<Light>();
		
		currentHealth = _maxHealth;
	}
	
	// Update is called once per frame
	void Update()
	{
		if (_isMoving) {
			transform.Translate(Vector3.forward * Time.deltaTime * MOVEMENT_SPEED, Space.World);
		}
}

	public void TakeDamage(float damage)
	{
		currentHealth -= damage;
		if (currentHealth <= 0f)
		{
			Explode();
		}
	}

	private void Explode()
	{
		Debug.Log("kilt");
		explosion.gameObject.SetActive(true);
/*		ParticleSystem.EmissionModule emission = explosion.emission;
		emission.enabled = true;*/
		foreach (VoxelRotator voxel in body)
		{
			voxel.Rigidbody.isKinematic = false;
			voxel.transform.SetParent(null);
			voxel.Rigidbody.AddExplosionForce(EXPLOSION_FORCE, transform.position, EXPLOSION_RADIUS);
		}

		_isMoving = false;
		_coreLight.gameObject.SetActive(false);
		StartCoroutine(CleanUp());
	}

	private IEnumerator CleanUp()
	{
		yield return new WaitForSeconds(DISPOSAL_TIME);
		Destroy(gameObject);
	}
}
