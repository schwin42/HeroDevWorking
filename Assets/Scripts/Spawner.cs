using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour {

    public GameObject prefab;
    public float spawnInterval = 2f;

    private float timer = 0f;

    public bool spawnEnabled = true;

	public float yVariance = 1f;
	public float xVariance = 1f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(spawnEnabled)
        {
            timer += Time.deltaTime;
            if(timer > spawnInterval)
            {
                GameObject go = Instantiate<GameObject>(prefab) as GameObject;
	            Vector3 spawnPosition = new Vector3((float)(transform.position.x + (Random.value - 0.5f) * xVariance), Random.value * yVariance, transform.position.z);
                go.transform.position = spawnPosition;
                timer = 0f;
            }
        }
	}
}
