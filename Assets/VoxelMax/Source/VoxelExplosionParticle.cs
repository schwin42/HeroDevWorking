using UnityEngine;
using System.Collections;

public class VoxelExplosionParticle : MonoBehaviour {

    public float fadeoutTimeInMS=1500f;
    private float creationTime;
	
	void Start () {
        this.creationTime = Time.fixedTime;
	}
	
	// Update is called once per frame
	void Update () {
        float elapsedTime=(Time.fixedTime - this.creationTime)*1000f;
        Color curColor=this.gameObject.GetComponent<Renderer>().sharedMaterial.color;
        curColor.a= ((this.fadeoutTimeInMS-elapsedTime) / this.fadeoutTimeInMS);
        this.gameObject.GetComponent<Renderer>().sharedMaterial.color = curColor;
        if (elapsedTime >= fadeoutTimeInMS)
            Destroy(this.gameObject);
	}
}
