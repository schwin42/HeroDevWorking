using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityGun : PlayerTool
{
	public float gravityForce = 1f;
	
	private bool _isAttracting = false;

	[SerializeField] private Transform _sourcePoint;
	[SerializeField] private GravityGunInfluence _influence;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (_isAttracting)
		{
			AttractVoxels();
		}
	}

	protected override void OnGripped(object sender, ClickedEventArgs e)
	{
		_isAttracting = true;
	}

	protected override void OnUngripped(object sender, ClickedEventArgs e)
	{
		_isAttracting = false;
	}
	
	void AttractVoxels()
	{
		foreach (Rigidbody rb in _influence.overlappingRigidbodies)
		{
			rb.AddForce((_sourcePoint.position - rb.position) * gravityForce);
		}
		
	}

	void AttachVoxel(VoxelManager voxel)
	{
		
	}
	
	void PropelVoxels()
	{
		
	}
}
