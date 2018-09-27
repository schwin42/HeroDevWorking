using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{

	public static SceneManager Instance
	{
		get
		{
			if (_instance == null)
			{
				Initialize();
			}

			return _instance;
		}
	}
	private static SceneManager _instance;

	//Actors
	public VrPlayer player { get { return _player; } }
	private VrPlayer _player;
	
	//Containers
	public Transform junkContainer { get { return _junkContainer; } }
	private Transform _junkContainer;
	
	//Layers
	public int geometryLayer { get { return _geometryLayer; } }
	private int _geometryLayer = -1;
	
	public int playerToolLayer { get { return _playerToolLayer; } }
	private int _playerToolLayer = -1;
	
	public int playerProjectileLayer { get { return _playerProjectileLayer; } }
	private int _playerProjectileLayer = -1;
	
	public int playerHurtLayer { get { return _playerHurtLayer; } }
	private int _playerHurtLayer = -1;
	
	public int playerShipLayer { get { return _playerShipLayer; } }
	private int _playerShipLayer = -1;
	
	public int enemyProjectileLayer { get { return _enemyProjectileLayer; } }
	private int _enemyProjectileLayer = -1;
	
	public int enemyLayer { get { return _enemyLayer; } }
	private int _enemyLayer = -1;
	
	private void Awake()
	{
		Initialize();
	}

	private static void Initialize()
	{
		_instance = GameObject.FindObjectOfType<SceneManager>();

		_instance._player = _instance.GetComponent<VrPlayer>();
		
		_instance._junkContainer = new GameObject("JunkContainer").transform;
		_instance._junkContainer.position = Vector3.zero;
		_instance._junkContainer.rotation = Quaternion.identity;

		//TODO Make this less god awful to access and validate
		_instance._geometryLayer = LayerMask.NameToLayer("Geometry");
		_instance._playerToolLayer = LayerMask.NameToLayer("PlayerTool");
		_instance._playerProjectileLayer = LayerMask.NameToLayer("PlayerProjectile");
		_instance._playerHurtLayer = LayerMask.NameToLayer("PlayerHurt");
		_instance._playerShipLayer = LayerMask.NameToLayer("PlayerShip");
		_instance._enemyProjectileLayer = LayerMask.NameToLayer("EnemyProjectile");
		_instance._enemyLayer = LayerMask.NameToLayer("Enemy");

		


	}
}
