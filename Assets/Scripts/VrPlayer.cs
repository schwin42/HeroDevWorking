using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum PlayerToolType
//{
//	Sidearm = 0,
//	Slingshot = 1,
//	Shield = 2,
//	Rifle = 3,
//}

public class VrPlayer : MonoBehaviour
{
	//Character config
	public int maxHealth = 10;
	
	//Prefab config
	[SerializeField]
	public PlayerTool[] toolPrefabs;

	//State
	private float currentHealth;
	public SteamVR_TrackedController[] controllers;
	List<List<PlayerTool>> toolsetsByIndex;
	int activeToolsetIndex = 0;

	//Bookkeeping
	public HeadManager head {
		get {
			return _head;
		}
	}
	private HeadManager _head;
	
	// Use this for initialization
	void Start()
	{
		StartCoroutine(DelayedStart());
	}

	public void TakeDamage(Projectile projectile)
	{
		currentHealth -= projectile.damage;
		Debug.Log("Player took " + projectile.damage + ". " + currentHealth + " remaining.");
		//TODO If health is less than zero, die or something?
	}
	
	private void Initialize()
	{
		InitializePlayerTools();

		//Input init
		_head = GetComponentInChildren<HeadManager>();
		_head.Initialize(this);
		
		for (int i = 0; i < controllers.Length; i++)
		{
			controllers[i].PadClicked += OnPadClicked;
		}
		
		//Character init
		currentHealth = maxHealth;
	}

	private IEnumerator DelayedStart()
	{
		//HACK Wait until SteamVR_Controllers have completely initialized
		yield return new WaitForSeconds(0.1f);
		Initialize();
	}
	
	private void InitializePlayerTools() {
		//Create tools
		toolsetsByIndex = new List<List<PlayerTool>>();
		for (int i = 0; i < toolPrefabs.Length; i++)
		{
			toolsetsByIndex.Add(new List<PlayerTool>());
			PlayerTool prefab = toolPrefabs[i];
			for (int j = 0; j < controllers.Length; j++)
			{
				SteamVR_TrackedController controller = controllers[j];
				toolsetsByIndex[i].Add(Instantiate(prefab, controller.transform) as PlayerTool);
				toolsetsByIndex[i][j].transform.localPosition = Vector3.zero;
				toolsetsByIndex[i][j].transform.localRotation = Quaternion.identity;
				toolsetsByIndex[i][j].transform.localScale = Vector3.one;
				toolsetsByIndex[i][j].Initialize(controllers);
				toolsetsByIndex[i][j].gameObject.SetActive(false);
			}
		}

		//Enable default set
		SetToolsetEnabled(activeToolsetIndex, true);
	}

	private void OnPadClicked(object sender, ClickedEventArgs e)
	{
		Debug.Log("on pad touched");
		SwitchTool();
	}
	
	private void SwitchTool() {
		SetToolsetEnabled(activeToolsetIndex, false);
		//Debug.Log("toolset index count: " + toolsetsByIndex.Count);
		activeToolsetIndex = activeToolsetIndex < toolsetsByIndex.Count - 1 ? activeToolsetIndex + 1 : 0;
		//Debug.Log("Active index = " + activeToolsetIndex.ToString());
		SetToolsetEnabled(activeToolsetIndex, true);
	}

	private void SetToolsetEnabled(int index, bool value) {
		toolsetsByIndex[index][0].gameObject.SetActive(value);
		toolsetsByIndex[index][1].gameObject.SetActive(value);
	}
}
