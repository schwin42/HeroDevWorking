using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace VoxelMax
{
    public class VoxelBomb : MonoBehaviour
    {
        public float explosionRadius = 5f;
        public float explosionForce = 300f;
        public bool triggered = false;        
        public GameObject explosionParticle = null;
        public int maxParticleCount = 30;
        private bool exploded = false;

        public GameObject objectToInstantiate = null;

        // Use this for initialization
        void Start()
        {

        }
        private GameObject rootObjectForParticles = null;
        // Update is called once per frame
        void Update() {
            if ((triggered) && (!this.exploded))
            {
                this.exploded=true;

                //Get the root object
                rootObjectForParticles=GameObject.Find("VoxelMaxExplosionRoot");
                if (rootObjectForParticles==null)
                {
                    rootObjectForParticles = new GameObject();
                    rootObjectForParticles.name = "VoxelMaxExplosionRoot";
                }

                Collider[] colliderList=Physics.OverlapSphere(this.gameObject.transform.position, this.explosionRadius);
                foreach (Collider curCollider in colliderList)
                {
                    if (curCollider.gameObject.tag == "Indestructible") continue;
                    VoxelContainer voxelContainer=curCollider.gameObject.GetComponent<VoxelContainer>();
                    if (voxelContainer != null)
                    {
                        Vector3 torqueVector = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)) * this.explosionForce;
                        Vector3 voxelPoint=voxelContainer.gameObject.transform.InverseTransformPoint(this.gameObject.transform.position);
                        List<Voxel> voxelsToRemove = new List<Voxel>();
                        
                        Vector3 transformedDistance = voxelContainer.gameObject.transform.InverseTransformVector(explosionRadius, 0f, 0f);
                        List<Voxel> voxelList;
                        lock (voxelContainer.voxels)
                        {
                             voxelList = new List<Voxel>(voxelContainer.voxels.Values);
                        }
                        foreach (Voxel curVoxel in voxelList)
                        {
                            float voxDistance = Vector3.Distance(curVoxel.position, voxelPoint);                                
                            if (voxDistance <= transformedDistance.magnitude)
                            {
                                voxelsToRemove.Add(curVoxel);
                            }
                        }          
                        
           
                        //Do not instantiate every voxel on large explosions to prevent frame drop
                        int particleStepper = 0;
                        if (maxParticleCount!=0)
                            particleStepper = Mathf.Max((int)(voxelsToRemove.Count / this.maxParticleCount), 1);                                              

                        for(int i = 0; i < voxelsToRemove.Count; i++) 
                        {
                            Voxel curVoxel = voxelsToRemove[i];
                            if ((particleStepper!=0) && ((i % particleStepper) == 0)) {
                                if (this.explosionParticle != null)
                                {
                                    GameObject newObject = Instantiate(this.explosionParticle);
                                    newObject.transform.parent = rootObjectForParticles.transform;
                                    newObject.transform.localScale = voxelContainer.gameObject.transform.localScale;
                                    newObject.transform.position = voxelContainer.gameObject.transform.TransformPoint(curVoxel.position);

                                    Renderer curRenderer = newObject.GetComponent<Renderer>();
                                    if (curRenderer != null)
                                    {
                                        if (curRenderer.material != null)
                                        {
                                            curRenderer.material.color = curVoxel.color;
                                        }
                                    }
                                    
                                    Rigidbody curRigidBody = newObject.GetComponent<Rigidbody>();
                                    if (curRigidBody != null)
                                    {
                                        curRigidBody.AddForce(voxelContainer.gameObject.transform.TransformDirection(curVoxel.position - voxelPoint).normalized * explosionForce);
                                        curRigidBody.AddRelativeTorque(torqueVector);
                                    }                                
                                }
                            }                            
                        }
                        voxelContainer.AddBuildTaskRemovedVoxels(voxelsToRemove);
                        VoxelPhysics vPhysics = this.GetComponent<VoxelPhysics>();
                        if ((vPhysics != null) && (vPhysics.enabled))
                            voxelContainer.rebuildCollider = false;


                        //Modification for Will
                        if (objectToInstantiate!=null)
                        {
                            GameObject newGameObject= Instantiate<GameObject>(this.objectToInstantiate);
                            newGameObject.transform.position = this.gameObject.transform.position;
                            newGameObject.transform.parent = rootObjectForParticles.transform;
                        }                        
                    }
                    else
                    {
                        Rigidbody curRigidBody = curCollider.gameObject.GetComponent<Rigidbody>();
                        if (curRigidBody != null)
                        {
                            Vector3 torqueVector = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)) * this.explosionForce;
                            Vector3 voxelPoint = curCollider.gameObject.transform.InverseTransformPoint(this.gameObject.transform.position);
                            curRigidBody.AddForce(curCollider.gameObject.transform.TransformDirection(voxelPoint- this.gameObject.transform.position).normalized * explosionForce);
                            curRigidBody.AddRelativeTorque(torqueVector);
                        }
                    }                               
                }
                Destroy(this.gameObject);
            }            
        }
    }
}