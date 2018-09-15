using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slingshot : PlayerTool {

    SteamVR_Controller controller;

	void Start() {
		return; //To avoid race conditions, use Initialize instead	
	}
}
