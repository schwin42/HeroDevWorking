using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Enemy : MonoBehaviour
{
	private const float MOVEMENT_SPEED = 10;
	private const float MAX_HEALTH = 10;
	private float currentHealth;
	
	// Use this for initialization
	void Start ()
	{
		currentHealth = MAX_HEALTH;
	}
	
	// Update is called once per frame
	void Update () {
		transform.Translate(Vector3.forward * Time.deltaTime * MOVEMENT_SPEED, Space.World);
	}

	public void TakeDamage(float damage)
	{
		currentHealth -= damage;
		if (currentHealth <= 0f)
		{
			
		}
	}
}
