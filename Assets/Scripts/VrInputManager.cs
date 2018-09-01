using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VrInputManager : MonoBehaviour
{

	public static SteamVR_TrackedController[] devices;

	// Use this for initialization
	void Awake()
	{
		devices = GetComponentsInChildren<SteamVR_TrackedController>();
	}

	// Update is called once per frame
	void Update()
	{

	}
}
