using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTrigger : MonoBehaviour {

    public ControllerManager manager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerStay(Collider other)
    {
        manager.HandleTriggerStay(other);
    }
}
