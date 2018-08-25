using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ControllerManager : MonoBehaviour {

    //Configuration
    public float propelForce = 100f;
    public float hapticFloor = 200f;
    public float hapticVariance = 100f;
    public Rigidbody projectilePrefab;
    SteamVR_TrackedController trackedController;
    SteamVR_Controller.Device otherDevice;
    SteamVR_Controller.Device device;

    //State
    bool isAiming = false;
    Rigidbody aimingProjectile = null;

    //Debug
    private Vector3 _attackVector;
    
	// Use this for initialization
	void Start () {
        trackedController = GetComponent<SteamVR_TrackedController>();
        // trackedController.TriggerClicked += OnTriggerClicked;
        
        device = SteamVR_Controller.Input((int)trackedController.controllerIndex);
        int otherDeviceId = trackedController.controllerIndex == 3 ? 4 : 3; //TODO Find less brittle way to determine controller indeces
        otherDevice = SteamVR_Controller.Input(otherDeviceId);

        Debug.Log("device: " + device.index);
        Debug.Log("otherDevice: " + otherDevice.index);


    }

    // Update is called once per frame
    void Update () {
        if(isAiming)
        {
            device.TriggerHapticPulse((ushort)(hapticFloor + (hapticVariance * GetAttackVector().sqrMagnitude)));
            _attackVector = GetAttackVector();
            if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                FireProjectile();
                isAiming = false;
            }
        }

    }

    void FireProjectile()
    {
        aimingProjectile.isKinematic = false;
        aimingProjectile.transform.SetParent(null);
        Vector3 attackForce = GetAttackVector() * propelForce;
        aimingProjectile.AddForce(attackForce);
    }

    Vector3 GetAttackVector()
    {
        Vector3 attackVector = otherDevice.transform.pos - device.transform.pos;
        return attackVector;

    }

    // void OnTriggerClicked(object sender, ClickedEventArgs e)
    // {
        //Debug.Log("Trigger clicked on " + trackedController.controllerIndex);
    // }

    public void HandleTriggerStay(Collider collider)
    {
        if (isAiming) return;


        if(collider.tag == "Yolk")
        {
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                Debug.Log("Trigger down in yolk");
                aimingProjectile = Instantiate<Rigidbody>(projectilePrefab, transform);
                aimingProjectile.transform.position = transform.position;
                aimingProjectile.isKinematic = true;
                aimingProjectile.gameObject .tag = "Projectile";
                isAiming = true;
            }
        }
    }
}
