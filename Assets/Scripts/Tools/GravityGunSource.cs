using System.Collections.Generic;
using UnityEngine;

public class GravityGunSource : Owner {

	private List<VoxelManager> _collectedVoxels;

	private GravityGun _gun;

	public override void HandleConstituentCollisionEnter(Collision hit)
	{
		if (hit.gameObject.layer == SceneManager.Instance.playerToolLayer || hit.gameObject.layer == SceneManager.Instance.playerProjectileLayer) return;
		Debug.Log("constituent trigger: " + LayerMask.LayerToName( hit.gameObject.layer));
		Collect(hit.collider);
	}

	public void Initialize(GravityGun gun)
	{
		_gun = gun;
		
		_collectedVoxels = new List<VoxelManager>();
	}

	private void OnTriggerEnter(Collider other)
	{
		Debug.Log("source trigger");
		//TODO Restrict relevant collisions to loose voxels
		Collect(other);

	}

	public void PropelVoxels(float force)
	{
		foreach (VoxelManager voxel in _collectedVoxels)
		{
			voxel.Rigidbody.isKinematic = false;
			voxel.Rigidbody.AddForce(-transform.up  * force);
			voxel.transform.SetParent(SceneManager.Instance.junkContainer, true);
			voxel.owner = null;


		}
		_collectedVoxels.Clear();
	}

	private void Collect(Collider other)
	{
		VoxelManager voxel = other.GetComponent<VoxelManager>(); //TODO Replace voxel GetComponents with voxel registry
		voxel.Rigidbody.isKinematic = true;
//		voxel.Rigidbody.velocity = Vector3.zero;
		voxel.transform.SetParent(_gun.transform, true);
		voxel.owner = this;
		voxel.gameObject.layer = SceneManager.Instance.playerProjectileLayer;
		_collectedVoxels.Add(voxel);
	}
}
