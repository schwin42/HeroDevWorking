using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityGun : PlayerTool
{
	public float attractionForce = 1f;
	public float proplusionForce = 1000f;
	
	private bool _isAttracting = false;

	[SerializeField] private Transform _sourcePoint;
	[SerializeField] private GravityGunInfluence _influence;
	[SerializeField] private GravityGunSource _source;
	
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

	protected override void OnTriggerClicked(object sender, ClickedEventArgs e)
	{	
		if (!gameObject.activeSelf) return;
		
		_source.PropelVoxels(proplusionForce);
	}
	
	protected override void OnGripped(object sender, ClickedEventArgs e)
	{	
		if (!gameObject.activeSelf) return;
		
		_isAttracting = true;
	}

	protected override void OnUngripped(object sender, ClickedEventArgs e)
	{
		if (!gameObject.activeSelf) return;
		
		_isAttracting = false;
	}
	
	void AttractVoxels()
	{
		Debug.Log("attracting voxels");
		foreach (Rigidbody rb in _influence.overlappingRigidbodies)
		{
			Debug.Log("attracting rigidbody: " + rb.name);
			rb.AddForce((_sourcePoint.position - rb.position) * attractionForce);
		}
		
	}
}
