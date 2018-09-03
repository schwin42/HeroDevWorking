using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityGunInfluence : MonoBehaviour
{

	public List<Rigidbody> overlappingRigidbodies;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnTriggerEnter(Collider collider)
	{
		overlappingRigidbodies.Add(collider.attachedRigidbody);
	}

	void OnTriggerLeave(Collider collider)
	{
		overlappingRigidbodies.Remove(collider.attachedRigidbody);
	}
}
