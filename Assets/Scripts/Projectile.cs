using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

	public float damage = 1f;

	[SerializeField] private GameObject blast;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void OnCollisionEnter(Collision hit)
	{
		Debug.Log("projec collide");
		if (hit.gameObject.layer == SceneManager.Instance.playerHurtLayer)
		{
			SceneManager.Instance.player.TakeDamage(this);
			Destroy();
		} else if(hit.gameObject.layer == SceneManager.Instance.enemyLayer)
		{
			Debug.Log("enemy");
			Enemy enemy = hit.gameObject.GetComponent<Enemy>();
			enemy.TakeDamage(damage);
			Destroy();
		}
	}
	
	public void Destroy()
	{
		blast.SetActive(true);
		blast.transform.SetParent(SceneManager.Instance.junkContainer);
		Destroy(gameObject);
	}
}
