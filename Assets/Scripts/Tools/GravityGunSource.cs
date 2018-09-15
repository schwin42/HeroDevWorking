using System.Collections.Generic;
using UnityEngine;

public class GravityGunSource : MonoBehaviour {

	private List<VoxelManager> _collectedVoxels;

	private GravityGun _gun;

	public void Initialize(GravityGun gun)
	{
		_gun = gun;
		
		_collectedVoxels = new List<VoxelManager>();
	}

	private void OnTriggerEnter(Collider other)
	{
		//TODO Restrict relevant collisions to loose voxels
		
		VoxelManager voxel = other.GetComponent<VoxelManager>(); //TODO Replace voxel GetComponents with voxel registry
		Debug.Log("voxel: " + (voxel == null ? "null" : "thing"));
		voxel.Rigidbody.isKinematic = true;
//		voxel.Rigidbody.velocity = Vector3.zero;
		voxel.transform.SetParent(_gun.transform, true);
		_collectedVoxels.Add(voxel);

	}

	public void PropelVoxels(float force)
	{
		foreach (VoxelManager voxel in _collectedVoxels)
		{
			voxel.Rigidbody.isKinematic = false;
			voxel.Rigidbody.AddForce(-transform.up  * force);
			voxel.transform.SetParent(SceneManager.Instance.junkContainer, true);


		}
		_collectedVoxels.Clear();
	}
}
