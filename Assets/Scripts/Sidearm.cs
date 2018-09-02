using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Sidearm : PlayerTool
{

	[SerializeField] private Transform bulletOrigin;

	private const float RANGE = 10f;
	private const float BLAST_FORCE = 1000f;
	private const ushort HAPTIC_FORCE = 10000;
	
	private AudioSource audioSource;

	public override void Initialize(SteamVR_TrackedController[] controllers)
	{
		base.Initialize(controllers);
		audioSource = GetComponent<AudioSource>();
	}

	protected override void OnTriggerClicked(object sender, ClickedEventArgs e)
	{
		audioSource.Play();
		device.TriggerHapticPulse(HAPTIC_FORCE);

		RaycastHit hit;
		if (Physics.Raycast(bulletOrigin.position, bulletOrigin.forward, out hit, RANGE))
		{
			Debug.Log("hit");
			Rigidbody rb = hit.collider.attachedRigidbody;
			rb.isKinematic = false;
			rb.AddForce(bulletOrigin.forward * BLAST_FORCE);
			rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * BLAST_FORCE);
		}
		else
		{
			Debug.Log("miss");
		}
	}

	void Update()
	{
		Debug.DrawRay(bulletOrigin.position, bulletOrigin.forward, Color.red );
	}
	
	void Start() {
		return; //To avoid race conditions, use Initialize instead	
	}
}
