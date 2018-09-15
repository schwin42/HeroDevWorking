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
	private GravityGunInfluence _influence;
	private GravityGunSource _source;
	
	// Use this for initialization
	void Start ()
	{
		_influence = GetComponentInChildren<GravityGunInfluence>();
		_source = GetComponentInChildren<GravityGunSource>();
		_source.Initialize(this);
	}
	
	// Update is called once per frame
	void Update () {
		if (_isAttracting)
		{
			_influence.AttractVoxels(_sourcePoint.position, attractionForce);
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
	

}
