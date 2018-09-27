using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSquare : MonoBehaviour

{

	[SerializeField] private GameObject _modulePrefab;

	private int _resourceCount = 0;
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void CollectResource(GameObject other)
	{
		_resourceCount++;
		Destroy(other);
		if (_resourceCount >= 5)
		{
			GameObject go = Instantiate(_modulePrefab) as GameObject;
			go.transform.SetParent(transform, false);
			go.transform.localPosition = Vector3.zero;
		}
	}
	

}

