using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Sidearm : PlayerTool
{

	[SerializeField] private Transform bulletOrigin;

	private const float RANGE = 20f;
	private const float BLAST_FORCE = 1000f;
	private const ushort HAPTIC_FORCE = 10000;
	private const float DAMAGE = 10f;
	
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
		Debug.DrawRay(bulletOrigin.position, bulletOrigin.forward, Color.cyan, 3f);
		if (Physics.Raycast(bulletOrigin.position, bulletOrigin.forward, out hit, RANGE))
		{
			if (hit.collider.gameObject.layer != SceneManager.Instance.enemyLayer)
			{
				Debug.Log("hit non-enemy: " + hit.collider.gameObject.name);
				return;
			}
			Debug.Log("hit enemy: " + hit.collider.gameObject.name);
			VoxelManager voxel = hit.collider.GetComponent<VoxelManager>();
			voxel.TakeDamage(DAMAGE, bulletOrigin, BLAST_FORCE);
			
		} else {
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
