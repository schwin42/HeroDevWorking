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

	public Transform junkContainer { get { return _junkContainer; } }
	private Transform _junkContainer;
	
	public int geometryLayer { get { return _geometryLayer; } }
	private int _geometryLayer;
	
	private void Awake()
	{
		Initialize();
	}

	private static void Initialize()
	{
		_instance = GameObject.FindObjectOfType<SceneManager>();
		
		_instance._junkContainer = new GameObject("JunkContainer").transform;
		_instance._junkContainer.position = Vector3.zero;
		_instance._junkContainer.rotation = Quaternion.identity;

		_instance._geometryLayer = LayerMask.NameToLayer("Geometry");

	}
}
