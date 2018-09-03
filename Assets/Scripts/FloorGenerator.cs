using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
	
	[SerializeField] private GameObject _floorVoxel;

	private SteamVR_PlayArea _playArea;
	
	//Debug
	private Vector3[] _verts;
	
	// Use this for initialization
	private void Start ()
	{
		_playArea = FindObjectOfType<SteamVR_PlayArea>();

		_verts = _playArea.vertices;

		float xExtent = Mathf.Abs(_verts[0].x - _verts[1].x);
		float zExtent = Mathf.Abs(_verts[1].z - _verts[2].z);
		Debug.Log("extent: " + xExtent.ToString() + ", " + zExtent);
		
		int xChunksNeeded = (int) Math.Ceiling(xExtent / _floorVoxel.transform.localScale.x);
		int zChunksNeeded = (int) Math.Ceiling(zExtent / _floorVoxel.transform.localScale.z);
		
		Debug.Log("chunks needed: " + xChunksNeeded + " , " + zChunksNeeded);
		for (int x = 0; x < xChunksNeeded; x++)
		{
			for (int z = 0; z < zChunksNeeded; z++)
			{
				Debug.Log("Creating voxel " + x.ToString() + ", " + z.ToString() + " of " + xChunksNeeded.ToString() + ", " + zChunksNeeded.ToString());
				GameObject voxel = Instantiate(_floorVoxel, new Vector3(
					x * _floorVoxel.transform.localScale.x - xExtent / 2,  
					0f,
					z * _floorVoxel.transform.localScale.z - zExtent / 2), 
					Quaternion.identity) as GameObject;
			}
		}

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
