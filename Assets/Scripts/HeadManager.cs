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

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Projectile"))
		{
			_player.TakeDamage(other.GetComponent<Projectile>());
		}

		Destroy(other.gameObject);
	}
}
