using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Owner : MonoBehaviour
{

	public virtual void TakeDamage(float damage) { }
	
	public virtual void HandleConstituentCollisionEnter(Collision hit) { }
}
