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
    
	// Use this for initialization
	void Start () {



    }

    // Update is called once per frame
    void Update () {
        if(isAiming)
        {
            device.TriggerHapticPulse((ushort)(hapticFloor + (hapticVariance * GetAttackVector().sqrMagnitude)));
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
