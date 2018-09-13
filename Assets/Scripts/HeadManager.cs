using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadManager : MonoBehaviour
{
	private VrPlayer _player;

	public void Initialize(VrPlayer player)
	{
		_player = player;
	}
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.collider.CompareTag("Projectile"))
		{
			_player.TakeDamage(other.collider.GetComponent<Projectile>());
		}

		Destroy(other.gameObject);
	}
}
