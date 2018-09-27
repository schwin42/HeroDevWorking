using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCollider : MonoBehaviour
{

	private GridSquare gridSquare;
	
	// Use this for initialization
	void Start ()
	{
		gridSquare = transform.parent.GetComponent<GridSquare>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.layer == SceneManager.Instance.playerProjectileLayer)
		{
			gridSquare.CollectResource(other.gameObject);
		}
	}
}
