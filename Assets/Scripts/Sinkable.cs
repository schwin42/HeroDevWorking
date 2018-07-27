using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sinkable : MonoBehaviour {

    Rigidbody rb;

    public int health;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {

	}

    private void OnCollisionEnter(Collision collision)
    {

        health -= 5;
        Debug.Log("current HP: " + health.ToString());

        if (health <= 0)
        {
            rb.isKinematic = false;
        }


    }
}
