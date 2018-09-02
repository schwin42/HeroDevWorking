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
	[SerializeField]
	public PlayerTool[] toolPrefabs;

	//State
	public SteamVR_TrackedController[] controllers;
	List<List<PlayerTool>> toolsetsByIndex;
	int activeToolsetIndex = 0;

	// Use this for initialization
	void Start()
	{
		StartCoroutine(DelayedStart());
	}

	void Initialize()
	{
		InitializePlayerTools();

		for (int i = 0; i < controllers.Length; i++)
		{
			controllers[i].PadClicked += OnPadClicked;
		}
	}

	IEnumerator DelayedStart()
	{
		//HACK Wait until SteamVR_Controllers have completely initialized
		yield return new WaitForSeconds(0.1f);
		Initialize();
	}
	
	void InitializePlayerTools() {
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
