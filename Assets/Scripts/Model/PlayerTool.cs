using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.iOS;

public abstract class PlayerTool : MonoBehaviour
{

	protected SteamVR_TrackedController trackedController;
	protected SteamVR_TrackedController otherController;
	protected SteamVR_Controller.Device device;
	protected SteamVR_Controller.Device otherDevice;
	
	//TODO Move into VrPlayer so this doesn't have to be resolved for each tool instance
	public virtual void Initialize(SteamVR_TrackedController[] controllers) {

		trackedController = transform.parent.GetComponent<SteamVR_TrackedController>();
		trackedController.TriggerClicked += OnTriggerClicked;
		trackedController.Gripped += OnGripped;
		trackedController.Ungripped += OnUngripped;
        
		device = SteamVR_Controller.Input((int)trackedController.controllerIndex);
		if(controllers.Length != 2)
		{
			Debug.LogError("Unexpected number of devices: " + controllers.Length);
			return;
		}
		int otherDeviceId = -1;
		Debug.Log("controllers found: " + controllers.Length);
		for(int i = 0; i < controllers.Length; i++)
		{
			Debug.Log("device found: " + controllers[i].controllerIndex);
			if(controllers[i].controllerIndex != device.index)
			{
				Debug.Log("assigning other id: " + controllers[i].controllerIndex);
				otherDeviceId = (int)controllers[i].controllerIndex;
				otherDevice = SteamVR_Controller.Input(otherDeviceId);
				otherController = controllers[i];
			}
		}
		
		if (device.index == 0 && otherDeviceId == -1)
		{
			Debug.LogError("No valid controllers found. Ensure that controllers are on.");
			return;
		}
		else
		{
			Debug.Log("SteamVR_TrackedControllers initialized successfully with IDs: " + device.index + " and " + otherDeviceId);
		}

	}

	//Override to do something. Otherwise, event will be ignored
	protected virtual void OnTriggerClicked(object sender, ClickedEventArgs e) { return; }
	protected virtual void OnGripped(object sender, ClickedEventArgs e) { return; }
	protected virtual void OnUngripped(object sender, ClickedEventArgs e) { return; }
	
	void Start () {
		return; //To avoid race conditions, use Initialize instead	
	}
}
