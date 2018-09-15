using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Enemy : Owner
{
	private enum AiMoveDirective
	{
		Idle = 0,
		ApproachFiringRange = 1,
		OrbitObject = 2,
		MoveToDestination = 3,
	}

	private enum AiFiringDirective
	{
		Idle = 0,
		Charging = 1,
	}
	
	//Character stats
	public float movementSpeed = 10;
	public float chargeTime = 2f;
	public float shotDamage = 1;
	public float maxHealth = 10;
	public float firingRange = 1.5f;
	public float pulseVelocity = 0.1f;
	
	//Technical config
	private const float EXPLOSION_FORCE = 1000f;
	private const float EXPLOSION_RADIUS = 1f;
	private const float DISPOSAL_TIME = 1.5f;
	
	//Prefab config
	[SerializeField] private VoxelManager _projectilePrefab;
	
	//Status
	private float currentHealth;
	private float _chargeTimer = 0f;
	private bool _isAlive = true;
	
	//AI State
	public bool enableMovement = true;
	private AiFiringDirective _firingDirective = AiFiringDirective.Charging;

	//Bookkeeping
	private VoxelManager[] body;
	[SerializeField]
	private ParticleSystem explosion;
	private Light _coreLight;
	private AudioSource _audioSource;
	
	// Use this for initialization
	void Start ()
	{
		body = GetComponentsInChildren<VoxelManager>();
		foreach (VoxelManager voxel in body)
		{
			voxel.Initialize(this);
		}

		_coreLight = GetComponentInChildren<Light>();
		_audioSource = GetComponent<AudioSource>();
		
		currentHealth = maxHealth;
	}
	
	// Update is called once per frame
	void Update()
	{
		if (!_isAlive) return; //Only do things if living
		
		
		//Process sense data and update states
		
		//Move according to state
		
		//Make firing progress
		if (_firingDirective == AiFiringDirective.Charging)
		{
			_chargeTimer += Time.deltaTime;
			if (_chargeTimer >= chargeTime)
			{
				FirePulse();
				_chargeTimer = 0f;
			}
		}
		
		if (enableMovement) {
			transform.Translate(Vector3.forward * Time.deltaTime * movementSpeed, Space.World);
		}
}

	public override void TakeDamage(float damage)
	{
		currentHealth -= damage;
		if (currentHealth <= 0f)
		{
			Explode();
		}
	}

	private void FirePulse()
	{
		_audioSource.Play();
		VoxelManager pulse = Instantiate(_projectilePrefab, transform.position, transform.rotation) as VoxelManager;
		pulse.Initialize(this);
		pulse.Rigidbody.velocity = (VrPlayer.Head.transform.position - transform.position) * pulseVelocity;
	}
	
	private void Explode()
	{
		Debug.Log("kilt");
		explosion.gameObject.SetActive(true);
/*		ParticleSystem.EmissionModule emission = explosion.emission;
		emission.enabled = true;*/
		foreach (VoxelManager voxel in body)
		{
			voxel.Rigidbody.isKinematic = false;
			voxel.transform.SetParent(SceneManager.Instance.junkContainer);
			voxel.name = "EnemyChunk";
			voxel.Rigidbody.AddExplosionForce(EXPLOSION_FORCE, transform.position, EXPLOSION_RADIUS);
			voxel.owner = null;
		}

		_isAlive = false;
		_coreLight.gameObject.SetActive(false);
//		StartCoroutine(CleanUp());
	}

	private IEnumerator CleanUp()
	{
		yield return new WaitForSeconds(DISPOSAL_TIME);
		Destroy(gameObject);
	}
}
