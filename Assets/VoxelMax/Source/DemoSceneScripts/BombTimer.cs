using UnityEngine;
using System.Collections;

public class BombTimer : MonoBehaviour {
    public float alarmTime = 10f;
    private float startedTime = 0f;
	// Use this for initialization
	void Start () {
        this.startedTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        if ((Time.time - this.startedTime) > this.alarmTime)
        {
            VoxelMax.VoxelBomb bomb=this.gameObject.GetComponent<VoxelMax.VoxelBomb>();
            if (bomb != null) bomb.triggered = true;            
        }
	}
}
