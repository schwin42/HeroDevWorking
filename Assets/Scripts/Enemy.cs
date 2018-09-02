using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
	private const float MAX_HEALTH = 10;
	private float currentHealth;
	
	// Use this for initialization
	void Start ()
	{
		currentHealth = MAX_HEALTH;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void TakeDamage(float damage)
	{
		currentHealth -= damage;
		if (currentHealth <= 0f)
		{
			
		}
	}
}
