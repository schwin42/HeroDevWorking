using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

namespace VoxelMax
{
    public class VoxelPhysics : MonoBehaviour
    {
        private enum PhysicsState {WaitingForTask, Fragmanatation, ObjectCreation, MovingDataToObjects, Finalisation};
        private VoxelContainer container = null;        
        private int lastVoxelCount = 0;        
        public int minAggregatedVoxelCount = 1;
        [NonSerialized]
        private PhysicsState currentState = PhysicsState.WaitingForTask;

        public bool showDebugMessages = false;
        public bool tensionCalculation = false;
        public int layerWidthBreakPoint = 1;
        // Use this for initialization
        void Start()
        {
            this.container = this.gameObject.GetComponent<VoxelContainer>();
            if (this.container == null)
                Debug.LogError("VoxelPhysics need a voxelcontainer on the same gameobject to work.");
            else
            {
                lock (this.container.voxels)
                {
                    this.lastVoxelCount = container.voxels.Count;                    
                }
            }
        }
                
        // Update is called once per frame
        public void VoxelPhysicUpdate()
        {
            this.container = this.gameObject.GetComponent<VoxelContainer>();
            if (this.container == null) return;

            switch (this.currentState)
            {
                case PhysicsState.WaitingForTask:
                    this.DoWaitForTask();
                    return;
                case PhysicsState.Fragmanatation:
                    //Fragmantation started by the waiting for Aggregation
                    return;
                case PhysicsState.ObjectCreation:
                    this.DoObjectCreation();
                    return;
                case PhysicsState.MovingDataToObjects:                    
                    return;
                case PhysicsState.Finalisation:
                    this.DoFinalization();
                    return;
            }           
        }

        private void DoFinalization()
        {         
            if ((physicsBuildTask != null) && (physicsBuildTask.isReady))
            {
                if (physicsBuildTask.voxelContainerList != null)
                {
                    for (int i = 0; i < physicsBuildTask.voxelContainerList.Count; i++)
                    {
                        physicsBuildTask.voxelContainerList[i].enabled = true;                        
                        physicsBuildTask.voxelContainerList[i].gameObject.GetComponent<Rigidbody>().mass = physicsBuildTask.voxelContainerList[i].voxels.Count;
                        physicsBuildTask.voxelContainerList[i].CreateBoxColliders(physicsBuildTask.colliders[i]);
                        ///physicsBuildTask.voxelContainerList[i].gameObject.GetComponent<Rigidbody>().drag = physicsBuildTask.voxelContainerList[i].voxels.Count;
                        MeshRenderer meshRenderer = physicsBuildTask.voxelContainerList[i].GetComponent<MeshRenderer>();
                        meshRenderer.enabled = true;
                        physicsBuildTask.voxelContainerList[i].mustRebuildBeforeUpdate = true;
                    }
                }
                this.container.CreateBoxColliders(physicsBuildTask.mainBodyCollider);
                physicsBuildTask = null;                
                return;
            }

            if (physicsBuildTask == null)
            {
                if ((!this.container.mustRebuildBeforeUpdate) && (this.IsAllFragmentsRebuilded()))
                {
                    if (showDebugMessages)
                        Debug.Log("VoxelMax Physics: Reenabling modules");

                    this.container.mustRebuildBeforeUpdate = true;

                    foreach (VoxelContainer voxContainer in this.fragmentContainers)
                    {
                        Rigidbody rigidBody = voxContainer.GetComponent<Rigidbody>();
                        MeshCollider meshCollider = voxContainer.GetComponent<MeshCollider>();                        
                        VoxelPhysics voxelPhysics = voxContainer.GetComponent<VoxelPhysics>();
                        rigidBody.useGravity = true;
                        if (meshCollider != null)
                            meshCollider.enabled = true;                                                
                        voxelPhysics.enabled = true;
                    }

                    //  this.gameObject.GetComponent<MeshCollider>().enabled = true;
                    if (this.gameObject.GetComponent<Rigidbody>() != null)
                        this.gameObject.GetComponent<Rigidbody>().useGravity = true;

                    this.currentState = PhysicsState.WaitingForTask;                    
                }               
            }
        }
        private Boolean IsAllFragmentsRebuilded()
        {
            foreach (VoxelContainer voxContainer in this.fragmentContainers)
            {
                MeshFilter curMeshFilter = voxContainer.gameObject.GetComponent<MeshFilter>();
                if (curMeshFilter == null) return false;
                if (curMeshFilter.sharedMesh == null) return false;
            }
            return true;
        }

        private void DoWaitForTask()
        {
            if ((this.container.voxels.Count != lastVoxelCount))
            {
                if (showDebugMessages)
                    Debug.Log("VoxelMax Physics: Fragmentation update");

                this.lastVoxelCount = this.container.voxels.Count;                
                this.DoFragmentation();                
            }
        }

        private void StructurePhysicsUpdate(object param)
        {
            //Steps:
            //1. Get "ground voxels"
            //2. Get the furthest voxel distance
            //3. Loop through the voxels from the furthest to the closest
            //   bring the sums of the forces throug the list
            //   If a voxel reach the max accumulated force then remove the voxel from the container
            //   and break the loop.            
           // List<Voxel> groundVoxels = this.GetGroundVoxels();
            //if (groundVoxels.Count == 0) return;   
            Vector3 minVector = this.container.GetMinContainerVector();
            Vector3 maxVector = this.container.GetMaxContainerVector();

            for (int i = (int)maxVector.y - 1; i >= (int)minVector.y; i--)
            {
                if (!this.IsLayerHaveMoreVoxesThenLimit(i))
                {
                    for (int x = (int)minVector.x; x <= maxVector.x; x++)
                    {
                        for (int z = (int)minVector.z; z <= maxVector.z; z++)
                        {
                            Vector3 coordinate = new Vector3(x, i, z);
                            if (this.container.voxels.ContainsKey(coordinate))
                            {
                             //   lock (this.container.voxels)
                                {                                    
                                    this.container.RemoveVoxel(this.container.voxels[coordinate], true);                                    
                                }
                            }
                        }
                    }
                    break;
                }
            }
            
        }

        private bool IsLayerHaveMoreVoxesThenLimit(int aLayer)
        {
            Vector3 minVector = this.container.GetMinContainerVector();
            Vector3 maxVector = this.container.GetMaxContainerVector();

            int numberOfVoxelsInLayer = 0;
            for (int x=(int)minVector.x; x<=maxVector.x; x++)
            {
                for (int z =(int) minVector.z; z <= maxVector.z; z++)
                {
                    Vector3 coordinate = new Vector3(x, aLayer, z);
                    if (this.container.voxels.ContainsKey(coordinate))
                    {
                        numberOfVoxelsInLayer++;
                    }
                    if (numberOfVoxelsInLayer>this.layerWidthBreakPoint)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<Voxel> GetGroundVoxels()
        {
            throw new NotImplementedException();
        }

        private List<VoxelContainer> fragmentContainers = new List<VoxelContainer>();
        private List<List<Voxel>> physicsAggregatedList = null;
        private void MoveAggregatedVoxelsIntoNewContainers(object aBuildTask)
        {
            PhysicsBuildTask buildTask = (PhysicsBuildTask)aBuildTask;
                   
            int gameObjectIndex = 0;
            try
            {
                if (buildTask.physicsAggregatedList != null)
                {                       
                    while (buildTask.physicsAggregatedList.Count > 1)
                    {                     
                        int smallestIndex = 0;
                        VoxelContainer newContainer = buildTask.voxelContainerList[gameObjectIndex];
                        newContainer.AssignUVDictionary(this.container);
                        foreach (Voxel curVoxel in buildTask.physicsAggregatedList[smallestIndex])
                        {
                            newContainer.AddVoxel(curVoxel, true);
                            lock (this.container.voxels)
                            {
                                this.container.RemoveVoxel(curVoxel, true);
                            }
                        }
                        

                        Texture2D curTexture = null;
                        int curTextureWidth = 0;
                        int curTextureHeight = 0;
                        foreach (VoxelArea curArea in this.container.voxelAreas.Values)
                        {
                            curTexture = curArea.GetTexture();
                            curTextureWidth = curArea.GetTextureWidth();
                            curTextureHeight = curArea.GetTextureHeight();
                            if (curTexture != null)
                                break;
                        }
                        
                        foreach (VoxelArea curArea in newContainer.voxelAreas.Values)
                        {
                            curArea.SetTexture(curTexture, curTextureWidth, curTextureHeight);
                        }

                        foreach (VoxelArea curArea in newContainer.voxelAreas.Values)
                        {
                            curArea.SetTexture(curTexture, curTextureWidth, curTextureHeight);
                        }

                        newContainer.GenerateAreaBuffers();

                        newContainer.rebuildCollider = false;
                        List<VoxelContainer.ColliderDescriptor> colliders = newContainer.PrepareColliderDescriptors();
                        buildTask.colliders.Add(colliders);

                        gameObjectIndex++;
                        buildTask.physicsAggregatedList.RemoveAt(smallestIndex);
                    }
                    buildTask.mainBodyCollider = this.container.PrepareColliderDescriptors();
                    lock (this.container.voxels)
                    {                                         
                        this.container.rebuildCollider = false;                        
                        this.container.GenerateAreaBuffers();
                        this.container.mustRebuildBeforeUpdate = false;
                        this.container.ingameBuildTask = null;
                    }
                    
                    this.currentState = PhysicsState.Finalisation;
                    buildTask.isReady = true;
                }
            }
            catch(Exception e)
            {
                Debug.LogError("VoxelPhysics error: " + e.Message);
            }                                  
        }
        
        private void FragmentationThread(object param)
        {                                
            try
            {
                lock (this.container.voxels)
                {
                    if (this.tensionCalculation)
                        StructurePhysicsUpdate(null);
                    this.physicsAggregatedList = this.GetAggregations((VoxelIngameBuildTask)param);
                }
                this.OrderAggregatedListByAltitude();

                this.lastVoxelCount = this.container.voxels.Count;
                this.currentState = PhysicsState.ObjectCreation;
            }
            catch (Exception e)
            {
                Debug.LogError("Voxel Physics error: " + e.Message);
            }
            /*finally
            {
              //  this.currentState = PhysicsState.ObjectCreation;                
            }   */                      
        }

        private class PhysicsBuildTask
        {
            public bool isReady = false;
            public List<GameObject> newGameObjectList;
            public List<List<Voxel>> physicsAggregatedList;
            public List<VoxelContainer> voxelContainerList;
            public List<List<VoxelContainer.ColliderDescriptor>> colliders;
            public List<VoxelContainer.ColliderDescriptor> mainBodyCollider;
            public PhysicsBuildTask(List<GameObject> aGameObjectList, List<List<Voxel>> aAgregatedList, List<VoxelContainer> aContainerList)
            {
                this.newGameObjectList = aGameObjectList;
                this.physicsAggregatedList = aAgregatedList;
                this.voxelContainerList = aContainerList;
                this.colliders = new List<List<VoxelContainer.ColliderDescriptor>>();
            }
        }

        private PhysicsBuildTask physicsBuildTask = null;
        
        public void DoFragmentation()
        {
            this.currentState = PhysicsState.Fragmanatation;                      

            VoxelIngameBuildTask ingameBuildTask = container.ingameBuildTask;            
            ThreadPool.QueueUserWorkItem(FragmentationThread, ingameBuildTask);
        }

        public void DoObjectCreation()
        {
            this.currentState = PhysicsState.ObjectCreation;
            if (showDebugMessages)
                Debug.Log("VoxelMax Physics: Object creation update");
           

            if (this.physicsAggregatedList == null)
            {
                if (showDebugMessages)
                    Debug.Log("VoxelMax Physics: No fragmented object to create");
                this.currentState = PhysicsState.WaitingForTask;
                return;
            }

            if (this.physicsAggregatedList.Count > 1)
            {
                //Prepare GameObjectList because it can be only done in the main thread
                List<GameObject> newGameObjectList = new List<GameObject>();
                List<VoxelContainer> newContainerList = new List<VoxelContainer>();
                for (int i = 0; i < this.physicsAggregatedList.Count - 1; i++)
                {
                    GameObject newGameObject = new GameObject();
                    newGameObject.transform.parent = this.transform;

                    VoxelContainer newContainer = newGameObject.AddComponent<VoxelContainer>();
                    newContainer.enabled        = false;                    
                    newContainer.modelFilename  = this.container.modelFilename;
                    newContainer.modelName      = this.container.modelName;
                    newContainer.textureFilename = this.container.textureFilename;
                    //newContainer.AssignUVDictionary(this.container);
                    newContainerList.Add(newContainer);
                    this.fragmentContainers.Add(newContainer);

                    MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
                    Rigidbody rigidBody = newGameObject.AddComponent<Rigidbody>();                    
                    newContainer.enabled = false;
                    meshRenderer.enabled = false;
                    rigidBody.useGravity = false;
                    newGameObject.transform.localScale   = new Vector3(1f, 1f, 1f);
                    newGameObject.transform.rotation     = this.gameObject.transform.rotation;
                    newGameObject.transform.position     = this.gameObject.transform.position;

                    //Add physics to fragments
                    VoxelPhysics newVoxelPhysics         = newGameObject.AddComponent<VoxelPhysics>();
                    newVoxelPhysics.tensionCalculation   = this.tensionCalculation;
                    newVoxelPhysics.layerWidthBreakPoint = this.layerWidthBreakPoint;
                    newVoxelPhysics.enabled              = false;

                    newGameObject.GetComponent<Renderer>().sharedMaterial = this.gameObject.GetComponent<MeshRenderer>().sharedMaterial;

                    newGameObjectList.Add(newGameObject);
                }

                //  this.gameObject.GetComponent<MeshCollider>().enabled = false;
                if (this.gameObject.GetComponent<Rigidbody>() != null)
                    this.gameObject.GetComponent<Rigidbody>().useGravity = false;

                this.physicsBuildTask = new PhysicsBuildTask(newGameObjectList, this.physicsAggregatedList, newContainerList);

                this.DoMoveDataToObjects();
            }
            else
            {
                if (this.physicsAggregatedList.Count == 1)
                {
                    if (this.gameObject.GetComponent<Rigidbody>() == null)
                    {
                     //   Rigidbody curRigidBody = this.gameObject.AddComponent<Rigidbody>();
                     //   curRigidBody.mass = this.container.voxels.Count;
                    }

                    MeshCollider meshCollider = this.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider != null)
                        Destroy(meshCollider);
                    this.physicsBuildTask = new PhysicsBuildTask(null, this.physicsAggregatedList, null);

                    this.DoMoveDataToObjects();                                     
                }
            }            
        }

        private void DoMoveDataToObjects()
        {
            this.currentState = PhysicsState.MovingDataToObjects;
            if (showDebugMessages)
                Debug.Log("VoxelMax Physics: Move data to objects");
            ThreadPool.QueueUserWorkItem(this.MoveAggregatedVoxelsIntoNewContainers, physicsBuildTask);
        }

        private int GetBottomLayerY()
        {
            int i = 0;
            int result = 0;
            foreach(Voxel curVoxel in this.container.voxels.Values)
            {
                if (i == 0)
                    result = (int)curVoxel.position.y;
                i++;
                if (result > curVoxel.position.y)
                {
                    result = (int)curVoxel.position.y;
                }
            }
            return result;
        }

        private void OrderAggregatedListByAltitude()
        {
            List<int> altitudes = new List<int>();
            for (int i = 0; i < physicsAggregatedList.Count; i++)
            {
                if (physicsAggregatedList[i].Count > 0) {
                    int curMinY = (int)physicsAggregatedList[i][0].position.y;
                    for (int j = 0; j < physicsAggregatedList[i].Count; j++)
                    {                        
                        if (physicsAggregatedList[i][j].position.y < curMinY)
                            curMinY = (int)physicsAggregatedList[i][j].position.y;
                    }
                    altitudes.Add(curMinY);
                }
            }
            for (int i = (altitudes.Count - 1); i >= 1; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (altitudes[j] < altitudes[j + 1])
                    {                       
                        int mover = altitudes[j];
                        altitudes[j] = altitudes[j + 1];
                        altitudes[j + 1] = mover;

                        List<Voxel> listmover = physicsAggregatedList[j];
                        physicsAggregatedList[j] = physicsAggregatedList[j + 1];
                        physicsAggregatedList[j+1] = listmover;
                    }
                }
            }
        }

        private List<List<Voxel>> GetAggregations(VoxelIngameBuildTask ingameBuildTask)
        {
            List<List<Voxel>> resultset = new List<List<Voxel>>();
            List<Voxel> listOfUnListedVoxels = new List<Voxel>(this.container.voxels.Values);

            if (ingameBuildTask != null)
            {
                foreach (Voxel curVoxel in ingameBuildTask.voxelsToRemove)
                {
                    listOfUnListedVoxels.Remove(curVoxel);
                }
            }

            HashSet<Vector3> curAggregatePos = new HashSet<Vector3>();
            while (listOfUnListedVoxels.Count > 0)
            {
                List<Voxel> curAggregate = new List<Voxel>();                
                GetSurroundingVoxels(listOfUnListedVoxels[0], ref curAggregate, ref curAggregatePos);
                if (curAggregate.Count >= this.minAggregatedVoxelCount)
                    resultset.Add(curAggregate);
                else
                {
                    foreach (Voxel curVoxel in curAggregate)
                    {
                        this.container.RemoveVoxel(curVoxel, true);
                    }                    
                }                
                foreach (Voxel curVoxel in curAggregate)
                {
                    listOfUnListedVoxels.Remove(curVoxel);
                }
            }
            return resultset;
        }

        private void GetSurroundingVoxels(Voxel aVoxel, ref List<Voxel> aAggregates, ref HashSet<Vector3> aAggregatesPos)
        {
            if (aAggregatesPos.Contains(aVoxel.position)) return;
            aAggregatesPos.Add(aVoxel.position);
            aAggregates.Add(aVoxel);

            foreach (Vector3 curDirection in StaticValues.sixDirectionArray)
            {
                if (!aVoxel.IsPossibleExtrudeDirection(curDirection))
                {
                    Voxel curVoxel = this.container.GetVoxelByCoordinate(aVoxel.position + curDirection);
                    if (curVoxel != null)
                    {
                        this.GetSurroundingVoxels(curVoxel, ref aAggregates, ref aAggregatesPos);
                    }
                }
            }
        }        
    }
}
