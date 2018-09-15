using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	public void AttractVoxels(Vector3 targetPos, float attractionForce)
	{
		for (int i = 0; i < overlappingRigidbodies.Count; i++)
		{
			Rigidbody rb = overlappingRigidbodies[i];
			if (rb == null)
			{
				Debug.LogWarning("Unable to find rigidbody at index: " + i.ToString());
				overlappingRigidbodies.RemoveAt(i);
				continue;
			}
			
			rb.AddForce((targetPos - rb.position) * attractionForce);
		}

	}
	
	private void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.layer == SceneManager.Instance.geometryLayer) return;
		
		Debug.Log("hit collider: " + collider.name);
		overlappingRigidbodies.Add(collider.attachedRigidbody);
	}

	void OnTriggerLeave(Collider collider)
	{
		overlappingRigidbodies.Remove(collider.attachedRigidbody);
	}
}
