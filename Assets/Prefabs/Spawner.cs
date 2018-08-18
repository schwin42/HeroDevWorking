using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public GameObject prefab;
    public float spawnInterval = 2f;

    private float timer = 0f;

    public bool spawnEnabled = true;

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
                go.transform.position = transform.position;
                timer = 0f;
            }
        }
	}
}
