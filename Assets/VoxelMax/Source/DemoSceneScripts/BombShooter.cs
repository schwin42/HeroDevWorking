using UnityEngine;
using System.Collections;

public class BombShooter : MonoBehaviour {
    public GameObject objectToInstantiate;
    public float dropForce = 100f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        Cursor.visible = false;
	    if (Input.GetMouseButtonUp(0))
        {
            GameObject newGameObject = Instantiate(objectToInstantiate);
            newGameObject.transform.position = this.transform.position;
            newGameObject.GetComponent<Rigidbody>().AddForce(this.gameObject.transform.TransformDirection(Vector3.forward) * dropForce);
            
        }
	}
}
