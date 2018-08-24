using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class CustomHand : MonoBehaviour {

    [SerializeField]
    public SteamVR_Controller.Device controller;

    private Valve.VR.EVRButtonId trigger = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;

    private Hand hand;
    private Hand otherHand;

    public float stuff = 45359802;

	// Use this for initialization
	void Start () {
        //Init();
        Debug.Log("trigger: " + trigger);
        hand = GetComponent<Hand>();
        Debug.Log("hand: " + (hand == null ? "null" : "thing"));
        otherHand = hand.otherHand;
	}
	
	// Update is called once per frame
	void Update () {
        //bool input = controller.GetPressDown(trigger);
        //Debug.Log("input: " + input);
        //if (input)
        //{
        //    Debug.Log("INPUT");
        //}

        Debug.Log("other hand: " + (otherHand == null ? "null" : "thing"));
        Vector3 attackVector = hand.transform.position - otherHand.transform.position;
        Debug.Log("attackVector: " + attackVector);


    }

    void Init()
    {
        //controller = SteamVR_Controller.Input(0);
        //Debug.Log("controller is a thing???????" + (controller == null ? "null" : "thing"));

    }
}
