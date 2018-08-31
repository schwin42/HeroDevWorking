using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;


namespace VoxelMax
{
    public enum EditModes { CursorMode, SelectMode, ExtrudeMode, FloodSelectMode, DrawMode, EraseMode, PaintMode, FloodPaintMode, ExportMode, MoveMode };
    public enum BrushModes { SquareBrush, CircleBrush, SphereBrush, PolyhedronBrush };
    public enum SelectModes { SurfaceMode, DepthMode, GlobalMode };

    public class VoxelContainer : MonoBehaviour, ISerializationCallbackReceiver
    {
        [HideInInspector]
        [SerializeField]
        public EditModes editMode;

        [HideInInspector]
        [SerializeField]
        public BrushModes brushMode = BrushModes.SquareBrush;
        [HideInInspector]
        [SerializeField]
        public int brushSize = 0;

        [HideInInspector]
        public SelectModes selectMode = SelectModes.SurfaceMode;

        [HideInInspector]
        [SerializeField]
        public Color brushColor = Color.white;
        [HideInInspector]
        [SerializeField]
        public bool brushColorFromBackground = false;

        [HideInInspector]
        [SerializeField]
        public string textureFilename = "";
        [HideInInspector]
        [SerializeField]
        public string materialFilename = "";
        [HideInInspector]
        [SerializeField]
        public string modelFilename = "";
        [HideInInspector]
        [SerializeField]
        public string modelName = "";
        [HideInInspector]
        [SerializeField]
        public bool optimized = false;
                
        //variables for the editor
        public int colorPaletteIndex = 0;
        internal bool mustRebuildBeforeUpdate = false;

        #region StandardEventHandling              
        internal bool rebuildCollider = true;
        public void Start()
        {
            int timeCounter = 0;
            while ((this.inDeserialization) && (timeCounter < 3000))
            {
                Thread.Sleep(1);
                timeCounter++;
            }

            if (!this.AllAreasHasTexture())
                this.PrepareTextures();

            this.GenerateAreaBuffers();
        }

        public void Update()
        {
            if (!this.inDeserialization)
            {                                             
                if (this.mustRebuildBeforeUpdate)
                {
                    this.CancelOptimizationInThread = true;
                    //The mesh data is updated from thread
                    this.BuildMesh(false, false, false, this.rebuildCollider); 
                    this.mustRebuildBeforeUpdate = false;

                 //   this.StartOptimizationInThread();
                }

                if ((!this.buildingInThreadFlag) && (this.ingameBuildTask != null))
                {
                    this.CancelOptimizationInThread = true;
                    //The system prepared a calculation package and it is ready to execute it                             
                    this.StartBuildMeshInThread();
                }

                if (optimizationFinnished)
                {
                    this.PushOptmizedDataToBuffers(false);                    
                }

                VoxelPhysics vPhysics = this.GetComponent<VoxelPhysics>();
                if ((vPhysics != null) && (vPhysics.enabled))
                    vPhysics.VoxelPhysicUpdate();

            }
        }
        #endregion

        #region ContainerFunctions
        public Dictionary<Vector3, Voxel> voxels = new Dictionary<Vector3, Voxel>();

        /*
         * aUpdateStructure has to be true except if you are sure that you update the structure in a later step
         * That way uploading the container can be much faster
         */
        public void AddVoxel(Voxel anewVoxel, bool aUpdateStructure = true)
        {
            this.voxels.Add(anewVoxel.position, anewVoxel);
            anewVoxel.SetContainer(this);

            if (aUpdateStructure)
            {
                //Update VoxelAres
                this.GetVoxelAreaForVoxel(anewVoxel).isChanged = true;
                //Update neightbor list 
                anewVoxel.UpdateNeighborList();

                //Update neighbor areas if there are any
                for (int i = 0; i < StaticValues.sixDirectionArray.Length; i++)
                {
                    Voxel nextVoxel = this.GetVoxelByCoordinate(anewVoxel.position + StaticValues.sixDirectionArray[i]);
                    if (nextVoxel != null)
                    {
                        this.GetVoxelAreaForVoxel(nextVoxel).isChanged = true;
                        //Ofcourse the neighbors list also changed
                        nextVoxel.UpdateNeighborList();
                    }
                }
            }
        }

        /*
         * aUpdateStructure has to be true except if you are sure that you update the structure in a later step
         * That way uploading the container can be much faster
         */
        public void RemoveVoxel(Voxel aVoxel, bool aUpdateStructure = true)
        {
            lock (this.voxels)
            {
                this.voxels.Remove(aVoxel.position);


                if (aUpdateStructure)
                {
                    //Update VoxelAres
                    VoxelArea curArea = this.GetVoxelAreaForVoxel(aVoxel);
                    curArea.isChanged = true;
                    curArea.RemoveVoxel(aVoxel);

                    //Update neighbor areas if there are any
                    for (int i = 0; i < StaticValues.sixDirectionArray.Length; i++)
                    {
                        Voxel nextVoxel = this.GetVoxelByCoordinate(aVoxel.position + StaticValues.sixDirectionArray[i]);
                        if (nextVoxel != null)
                        {
                            VoxelArea nextArea = null;
                            if (curArea.ContainsVoxel(nextVoxel))
                                nextArea = curArea;
                            else
                                nextArea = this.GetVoxelAreaForVoxel(nextVoxel);

                            nextArea.isChanged = true;
                            //Ofcourse the neighbors list also changed
                            nextVoxel.UpdateNeighborList();
                        }
                    }
                }
            }
        }

        public Voxel GetVoxelByCoordinate(Vector3 aCoord)
        {
            if (voxels.ContainsKey(aCoord))
            {
                return voxels[aCoord];
            }
            else
            {
                return null;
            }
        }

        public Voxel GetVoxelByCoordinate(Vector3 aCoord, float aMaxDistance)
        {
            Voxel result = null;
            float curSmallestDistance = aMaxDistance * this.transform.lossyScale.magnitude;
            aMaxDistance *= this.transform.lossyScale.magnitude;
            foreach (Voxel curVoxel in this.voxels.Values)
            {

                Vector3 curVoxPos = this.gameObject.transform.TransformPoint(curVoxel.position);
                //Vector3 curVoxPos=this.gameObject.transform.TransformVector(curVoxel.position);
                float curDistance = Vector3.Distance(curVoxPos, aCoord);
                if ((curDistance <= aMaxDistance) && (curSmallestDistance > curDistance))
                {
                    curSmallestDistance = curDistance;
                    result = curVoxel;
                }
            }
            if (curSmallestDistance <= aMaxDistance)
                return result;
            return null;
        }

        public int GetUniqueColorCount()
        {
            int result = 0;
            List<Color> materialList = new List<Color>();
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                int curIndex = materialList.IndexOf(curVoxel.color);
                if (curIndex < 0)
                {
                    result++;
                    materialList.Add(curVoxel.color);
                }
            }
            return result;
        }

        public Vector3 GetMinContainerVector()
        {
            Vector3 result = Vector3.zero;
            bool firstVector = true;
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (firstVector)
                {
                    result = curVoxel.position;
                    firstVector = false;
                }
                if (result.x > curVoxel.position.x)
                {
                    result.x = curVoxel.position.x;
                }
                if (result.y > curVoxel.position.y)
                {
                    result.y = curVoxel.position.y;
                }
                if (result.z > curVoxel.position.z)
                {
                    result.z = curVoxel.position.z;
                }
            }
            return result;
        }

        public Vector3 GetMaxContainerVector()
        {
            Vector3 result = Vector3.zero;
            bool firstVector = true;
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (firstVector)
                {
                    result = curVoxel.position;
                    firstVector = false;
                }
                if (result.x < curVoxel.position.x)
                {
                    result.x = curVoxel.position.x;
                }
                if (result.y < curVoxel.position.y)
                {
                    result.y = curVoxel.position.y;
                }
                if (result.z < curVoxel.position.z)
                {
                    result.z = curVoxel.position.z;
                }
            }
            return result;
        }

        public int GetEstimatedVerticleCount()
        {
            int result = 0;
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                result += curVoxel.GetMustDrawFacesCount() * 4;
            }
            return result;
        }
        
        /*
        This function is usefull when we check if we have to rebuild a container with new mesh,texture,material name
        */
        public bool EqualVoxelContainers(VoxelContainer aContainer)
        {
            if (this.voxels.Count != aContainer.voxels.Count)
                return false;

            foreach (Vector3 key in this.voxels.Keys)
            {
                if (aContainer.voxels.ContainsKey(key)) {
                    if (!this.voxels[key].EqualVoxels(aContainer.voxels[key]))
                        return false;
                } else
                    return false;
            }
            return true;
        }
        #endregion

        #region BuildTexture
        private bool AllAreasHasTexture()
        {
            foreach (VoxelArea curArea in this.voxelAreas.Values)
            {
                if (curArea.GetTexture() == null) return false;
            }
            return true;
        }
        private void PrepareTextures()
        {
            //Prepare the uv coordinate dictionary and texture "pointers",
            //so the system can use them on explosions.
            Texture2D curTexture = this.PrepareTextureAndUVDictionary(false);
            if (curTexture != null)
            {
                foreach (VoxelArea curArea in this.voxelAreas.Values)
                {
                    curArea.SetTexture(curTexture);
                }
            }            
        }
        //This function is for calculating the neccesary size of the texture
        //Decided to use power of two size, since it might be better for mobile devices
        private int GetNearestPowerofTwo(int aPixelCount)
        {
            if (aPixelCount == 0)
                return 0;
            if (aPixelCount == 1)
                return 1;

            bool found = false;
            int result = 2;
            while (!found)
            {
                if (aPixelCount <= result * result)
                {
                    return result;
                }
                result *= 2;
            }
            return result;
        }

        //Function to get unique filename if neccesary
        private string GetTextureFilename()
        {
            string result = this.textureFilename;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {

                bool textureWithTheSameFileName = false;
                VoxelContainer[] containerList = Resources.FindObjectsOfTypeAll<VoxelContainer>();
                foreach (VoxelContainer curContainer in containerList)
                {
                    if ((this != curContainer) && (curContainer.textureFilename == this.textureFilename) && (!curContainer.EqualVoxelContainers(this)))
                    {
                        textureWithTheSameFileName = true;
                        break;
                    }
                }
                if ((result == "") || (textureWithTheSameFileName))
                {
                    result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + this.name + ".png";
                    int i = 1;
                    while (File.Exists(result))
                    {
                        result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + this.name + "_" + i + ".png";
                        i++;
                    }
                }
                this.CheckAndCreateFolderInConstructedFolder(this.name);
            }
#endif
            this.textureFilename = result;
            return result;
        }

        //Creates a folder for the current model
        private void CheckAndCreateFolderInConstructedFolder(String aFolder)
        {
#if UNITY_EDITOR
            if (!System.IO.Directory.Exists("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + aFolder))
                AssetDatabase.CreateFolder("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder, aFolder);
#endif
        }

        //Function to get unique material filename
        private string GetMaterialFileName()
        {
            string result = this.materialFilename;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                bool materialWithTheSameFileName = false;
                VoxelContainer[] containerList = Resources.FindObjectsOfTypeAll<VoxelContainer>();
                foreach (VoxelContainer curContainer in containerList)
                {
                    if ((this != curContainer) && (curContainer.materialFilename == this.materialFilename) && (!curContainer.EqualVoxelContainers(this)))
                    {
                        materialWithTheSameFileName = true;
                        break;
                    }
                }

                if ((result == "") || (materialWithTheSameFileName))
                {
                    result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + this.name + ".mat";
                    int i = 1;
                    while (File.Exists(result))
                    {
                        result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + this.name + "_" + i + ".mat";
                        i++;
                    }
                    this.CheckAndCreateFolderInConstructedFolder(this.name);
                }
            }
#endif
            this.materialFilename = result;
            return result;
        }

        //Function to get unique model filename
        public string GetModelFileName()
        {
            string result = this.modelFilename;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {

                bool modelWithTheSameFileName = false;
                VoxelContainer[] containerList = Resources.FindObjectsOfTypeAll<VoxelContainer>();
                foreach (VoxelContainer curContainer in containerList)
                {
                    if ((this != curContainer) && (curContainer.modelFilename == this.modelFilename) && (!curContainer.EqualVoxelContainers(this)))
                    {
                        modelWithTheSameFileName = true;
                        break;
                    }
                }

                if ((result == "") || (modelWithTheSameFileName))
                {
                    result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + StaticValues.meshNamePrefix + this.name + ".asset";
                    int i = 1;
                    while (File.Exists(result))
                    {
                        result = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder + "/" + this.name + "/" + StaticValues.meshNamePrefix + this.name + "_" + i + ".asset";
                        i++;
                    }
                    this.CheckAndCreateFolderInConstructedFolder(this.name);
                }
            }
#endif
            this.modelFilename = result;
            return result;
        }

        private Dictionary<Color, int> colorListInTexture = null;
        //This function helps in editor mode to decide if we have to rebuild the texture or not on update
        private bool TextureMustChanged(Dictionary<Color, int> aColorList)
        {
            if (this.colorListInTexture != null)
            {
                if (this.GetNearestPowerofTwo(colorListInTexture.Count) != this.GetNearestPowerofTwo(aColorList.Count))
                    return true;
                foreach (Color curColor in aColorList.Keys)
                {
                    if (!this.colorListInTexture.ContainsKey(curColor))
                        return true;
                }                
            }
                       
            return false;
        }

        //Building the texture in editor mode, it only rebuilds it if it is neccesary 
        //If it is not, then it returns the existing texture
        private Texture2D BuildTexture(VoxelContainer aVoxelContainer, out bool oTextureRebuilded, bool aShowProgressBar)
        {
            Texture2D result;
            oTextureRebuilded = false;
#if UNITY_EDITOR
            Dictionary<Color, int> colorDict = new Dictionary<Color, int>();
            int uniqueColorCount = 0;

            foreach (Voxel curVoxel in aVoxelContainer.voxels.Values)
            {
                Color curColor = curVoxel.color;
                if (!colorDict.ContainsKey(curColor))
                {
                    colorDict.Add(curColor, 0);
                    uniqueColorCount++;
                }
            }

            Texture2D existingTexture = null;
            string textureFilename = this.GetTextureFilename();

            bool textureLost = false;
            Renderer curRenderer = this.GetComponent<Renderer>();
            if (curRenderer != null)
            {
                Material texturedMaterial = curRenderer.sharedMaterial;
                if (texturedMaterial != null)
                {
                    if (texturedMaterial.mainTexture == null)
                        textureLost = true;
                    else
                        existingTexture = (Texture2D)texturedMaterial.mainTexture;
                } else
                    textureLost = true;
            } else
                textureLost = true;

            if (this.TextureMustChanged(colorDict) || (this.colorListInTexture == null) || (textureLost))
            {
                int textureDimension = this.GetNearestPowerofTwo(uniqueColorCount) * 3;//3*3 

                //update colorlist
                this.colorListInTexture = null;
                this.colorListInTexture = colorDict;

                //Create texture by inserting colorlist
                result = new Texture2D(textureDimension, textureDimension);
                int u = 0;
                int w = 0;
                int index = 0;
                foreach (Color curColor in colorDict.Keys)
                {
                    if (aShowProgressBar)
                    {
#if UNITY_EDITOR
                        if (EditorUtility.DisplayCancelableProgressBar("Building Texture", "Building", (float)index / (float)colorDict.Count)) return null;
#endif
                        index++;
                    }
                    if (u + 2 >= textureDimension)
                    {
                        u = 0;
                        w += 3;
                    }
                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 3; j++)
                        {
                            result.SetPixel(u + i, w + j, curColor);
                        }
                    u += 3;
                }
                byte[] bytes = result.EncodeToPNG();
                if (File.Exists(textureFilename))
                    File.Delete(textureFilename);

#if !UNITY_WEBPLAYER
                try
                {
                    File.WriteAllBytes(textureFilename, bytes);
                }
                catch
                {
                    this.textureFilename = "";
                    this.modelFilename = "";
                    this.materialFilename = "";
                    textureFilename = this.GetTextureFilename();
                    File.WriteAllBytes(textureFilename, bytes);
                }
#else
                Debug.LogWarning("Please make sure that your target platform is NOT webplayer during editing the voxel structures.");
#endif
                AssetDatabase.ImportAsset(textureFilename);

                oTextureRebuilded = true;
                result = this.LoadTexture(textureFilename);
            }
            else
            {
                oTextureRebuilded = false;
                result = existingTexture;
            }

            return result;
        }

        private Texture2D LoadTexture(string aFilename)
        {
            Texture2D result = null; ;
#if UNITY_5_0
			result = (Texture2D)AssetDatabase.LoadAssetAtPath(textureFilename, typeof(Texture2D));
#else
            result = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFilename);
#endif
#else
	            result = null;
#endif
            return result;
        }

        //Remove the voxels from the inside
        public void HollowObject()
        {
            List<Voxel> voxelsToRemove = new List<Voxel>();
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (curVoxel.IsInnerVoxel())
                {
                    voxelsToRemove.Add(curVoxel);
                }
            }
            foreach (Voxel curVoxel in voxelsToRemove)
            {
                this.RemoveVoxel(curVoxel);
            }
            this.ClearVoxelAreas();
            this.GenerateVoxelAreas();
            this.BuildMesh(true, true, true, true);
        }

        private bool VectorHasSmallerAxes(Vector3 aBaseVector, Vector3 aVector)
        {
            if ((aBaseVector.x > aVector.x) || (aBaseVector.y > aVector.y) || (aBaseVector.z > aVector.z))
                return true;
            return false;
        }

        private bool VectorHasBiggerAxes(Vector3 aBaseVector, Vector3 aVector)
        {
            if ((aBaseVector.x < aVector.x) || (aBaseVector.y < aVector.y) || (aBaseVector.z < aVector.z))
                return true;
            return false;
        }

        private bool CheckIfInCapsulatedPoint(Vector3 aPosition, int aCheckDistance, Vector3 aMinPoint, Vector3 aMaxPoint, ref List<Vector3> resultList, ref HashSet<Vector3> resultListHashSet, ref HashSet<Vector3> anotCapsulated, int depth)
        {
            if (anotCapsulated.Contains(aPosition))
                return false;
            if (!this.voxels.ContainsKey(aPosition))
            {
                if (resultListHashSet.Contains(aPosition))                                    
                    return true;                
            } else
                return false;                              

            List<Vector3> checkedVector = new List<Vector3>();
            bool failed = false;
            foreach (Vector3 curDirection in StaticValues.sixDirectionArray)
            {
                for (int i = 1; i <= aCheckDistance; i++)
                {
                    Vector3 curPos = aPosition + (curDirection * i);            
                    if (this.voxels.ContainsKey(curPos))
                        break;//we reached the "wall"               
                    if (this.VectorHasSmallerAxes(aMinPoint, curPos) || (this.VectorHasBiggerAxes(aMaxPoint, curPos)))
                    {
                        failed = true;
                        break;
                    }
                    if (resultListHashSet.Contains(curPos))
                    {
                        foreach (Vector3 curVector in checkedVector)
                        {
                            resultListHashSet.Add(curVector);
                            resultList.Add(curVector);
                        }
                        resultListHashSet.Add(aPosition);
                        resultList.Add(aPosition);
                        return true;
                    }

                    if (anotCapsulated.Contains(curPos))
                    {
                        failed = true;
                        break;
                    }
                    checkedVector.Add(curPos);
                    if (i == aCheckDistance)
                    {
                        failed = true;
                        break;//did not reached the wall
                    }               
                }
                if (failed)
                    break;
            }
            if (failed)
            {
                foreach(Vector3 curVector in checkedVector)
                    anotCapsulated.Add(curVector);
                return false;
            }

            bool result = true;
            if (depth > 0)
            {
                resultListHashSet.Add(aPosition);
                resultList.Add(aPosition);
                return result;
            }

            foreach (Vector3 curDirection in StaticValues.sixDirectionArray)
            {
                for (int i = 1; i <= aCheckDistance; i++)
                {
                    Vector3 curPos = aPosition + (curDirection * i);
                    if (this.voxels.ContainsKey(curPos))
                        break;//we reached the "wall"      
                    if (this.VectorHasSmallerAxes(aMinPoint, curPos) || (this.VectorHasBiggerAxes(aMaxPoint, curPos)))
                    {
                        result = false;
                        break;
                    }
                    if (resultListHashSet.Contains(curPos))
                        break;                      
                    if (anotCapsulated.Contains(curPos))
                    {
                        result = false;
                        break;
                    }                                             
                    if (!this.CheckIfInCapsulatedPoint(curPos, aCheckDistance, aMinPoint, aMaxPoint, ref resultList, ref resultListHashSet, ref anotCapsulated, depth+1))
                    {
                        result = false;//one node did not reached the wall
                        break;
                    }
                }
                if (!result) break;
            }
            if (!result)
            {
                foreach (Vector3 curVector in resultList)
                    anotCapsulated.Add(curVector);
            } else
            {
                resultListHashSet.Add(aPosition);
                resultList.Add(aPosition);
            }

            return result;
        }

        //Fill up the incapsulated holes in the object
        public void FillUpObject()
        {
            List<Vector3> voxelsToFillUp = new List<Vector3>();
            HashSet<Vector3> voxelsToFillUpHashSet = new HashSet<Vector3>();
            HashSet<Vector3> voxelsNotEncapsulated = new HashSet<Vector3>();
            Vector3 minVector = this.GetMinContainerVector();
            Vector3 maxVector = this.GetMaxContainerVector();
            int checkDistance = (int)Mathf.Max ((Mathf.Round(maxVector.x) - (int)minVector.x),
                (Mathf.Round(maxVector.y) - (int)minVector.y),
                (Mathf.Round(maxVector.z) - (int)minVector.z)); // Mathf.RoundToInt(Vector3.Distance(this.GetMinContainerVector(), this.GetMaxContainerVector()));
            int stepCounter = 0;
            int maxStepCount = (int)((Mathf.Round(maxVector.x) - (int)minVector.x)
                * (Mathf.Round(maxVector.y) - (int)minVector.y)
                * (Mathf.Round(maxVector.z) - (int)minVector.z));

            try {
                for (int x = (int)minVector.x; x < Mathf.Round(maxVector.x); x++)
                {
                    for (int y = (int)minVector.y; y < Mathf.Round(maxVector.y); y++)
                    {
                        for (int z = (int)minVector.z; z < Mathf.Round(maxVector.z); z++)
                        {
#if UNITY_EDITOR
                            if (EditorUtility.DisplayCancelableProgressBar("Fill up", "Filling up ("+stepCounter+"/"+maxStepCount+")", (float)stepCounter / (float)maxStepCount)) return;
#endif
                            stepCounter++;
                            Vector3 curPos = new Vector3(x, y, z);
                            if ((!voxelsToFillUp.Contains(curPos)) && (!this.voxels.ContainsKey(curPos)) && (!voxelsNotEncapsulated.Contains(curPos)))
                            {                                
                                this.CheckIfInCapsulatedPoint(curPos, checkDistance, minVector, maxVector, ref voxelsToFillUp, ref voxelsToFillUpHashSet, ref voxelsNotEncapsulated, 0);                                
                            }
                        }
                    }
                }
            }finally
            {
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
            }         

            //Get Color for the new voxels
            List<Color> colorList = new List<Color>();
            for (int i = 0; i < voxelsToFillUp.Count; i++)
            {
                float smallestDistance=0f;
                Color curColor = Color.white;
                bool firstVoxel = true;
                foreach (Voxel curVoxel in this.voxels.Values)
                {
                    float curDistance = Vector3.Distance(curVoxel.position, voxelsToFillUp[i]);
                    if (firstVoxel)
                    {
                        firstVoxel = false;
                        smallestDistance = curDistance;
                        curColor = curVoxel.color;
                    } 
                    if (curDistance < smallestDistance)
                    {                        
                        smallestDistance = curDistance;
                        curColor = curVoxel.color;
                    }
                }
                colorList.Add(curColor);
            }

            //Add the new Voxels
            for (int i = 0; i < voxelsToFillUp.Count; i++)
            {
                if (!this.voxels.ContainsKey(voxelsToFillUp[i]))
                {
                    Voxel newVoxel = new Voxel();
                    newVoxel.position = voxelsToFillUp[i];
                    newVoxel.color = colorList[i];
                    this.AddVoxel(newVoxel);
                }
            }

            this.ClearVoxelAreas();
            this.GenerateVoxelAreas();
            this.BuildMesh(true, true, true, true);
        }


        //This should modify the settings of a texture, so we can reach it's pixels color
        public static Texture2D ReImportTexture(Texture2D aTexture)
        {
            #if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(aTexture);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
//              importer.textureFormat = TextureImporterFormat.RGBA32;
      //      importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);           

			#if UNITY_5_0 
			Texture2D reImportedTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));  
			#else
			Texture2D reImportedTexture = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(path);  
			#endif

            return reImportedTexture;            
            #else
                return null;         
            #endif
        }

        //This should modify the settings of a texture, so we can reach it's pixels color
        public static Texture2D ReImportTexture(string aTexturename)
        {
            #if UNITY_EDITOR
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(aTexturename);
            if (importer == null)
            {
                Debug.LogError("Could not find texture by filename: " + aTexturename);
                return null;
            }            
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.textureFormat = TextureImporterFormat.RGBA32;
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(aTexturename, ImportAssetOptions.ForceUpdate);

			#if UNITY_5_0 
			Texture2D reImportedTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(aTexturename, typeof(Texture2D));  
			#else
			Texture2D reImportedTexture = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(aTexturename);  
			#endif

           
            return reImportedTexture;
            #else
            return null;
            #endif

        }
        #endregion

        #region BuildMeshFunctions
        //This was a neccesary function for the old versions, but I think it is still more clear like this.        
        private int AddVertex(List<Vector3> aVertices, List<int> aPolygons, List<Vector2> aUVs, Vector3 aPosition, Vector2 aUV)
        {            
            aVertices.Add(aPosition);                
            aUVs.Add(aUV);
            int vertexIndex = aVertices.Count - 1;            
            aPolygons.Add(vertexIndex);
            return vertexIndex;
       } 
        
        //This function calculates a face polygons of a voxel and it's uv coordinates
        public void BuildPolygonsForFace(Texture2D aTexture, Color aVoxelColor, List<Vector3> aVertices, List<int> aPolygons, List<Vector2> aUvs, Vector3 aPosition, Vector3 aOffset1, Vector3 aOffset2, int aTextureWidth=0, int aTextureHeight=0)
        {
            Vector2 curUV = this.GetUvOfColor(aTexture, aVoxelColor);
            if (aTextureWidth == 0) aTextureWidth = aTexture.width;
            if (aTextureHeight == 0) aTextureHeight = aTexture.height;
    
            Vector3 bottomLeft = aPosition - (aOffset1.normalized / 2f) - (aOffset2.normalized / 2f);
            AddVertex(aVertices, aPolygons, aUvs, bottomLeft, curUV);
            int cornerIndex=AddVertex(aVertices, aPolygons, aUvs, bottomLeft + aOffset1, curUV + new Vector2(0f, 1f / (float)aTextureHeight));
            AddVertex(aVertices, aPolygons, aUvs, bottomLeft + aOffset2, curUV + new Vector2(1f / (float)aTextureWidth, 0f));
            
            //AddVertex(aVertices, aPolygons, aUvs, bottomLeft + aOffset1, curUV + new Vector2(0f, 0.99f / (float)aTextureHeight), aOptimize);                                  
            aPolygons.Add(cornerIndex);

            AddVertex(aVertices, aPolygons, aUvs, bottomLeft + aOffset1 + aOffset2, curUV + new Vector2(1f / (float)aTextureWidth, 1f / (float)aTextureHeight));
            AddVertex(aVertices, aPolygons, aUvs, bottomLeft + aOffset2, curUV + new Vector2(1f / (float)aTextureWidth, 0f));
        }

        //Returns the uv coordinate of a color from a dictionary, 
        //If it does not find in the dictionary then it look for it in the texture,
        //and also add the result to the dictionary
        private Vector2 GetUvOfColor(Texture2D aTexture, Color aColor)
        {
            Vector2 result = Vector2.zero;
            if (uvDictionary != null)
            {
                if (uvDictionary.ContainsKey(aColor))
                {
                    return uvDictionary[aColor];
                }
            }
            if (aTexture != null)
            {
                for (int tollerance = 0; tollerance < 10; tollerance++)
                {
                    for (int i = 0; i < aTexture.width; i++)
                    {
                        for (int j = 0; j < aTexture.height; j++)
                        {
                            Color pixelColor = aTexture.GetPixel(i, j);
                            if (IsRGBAColorsEquals(pixelColor, aColor, tollerance))
                            {
                                result = new Vector2((i + 0.99f) / (float)aTexture.width, (j + 0.99f) / (float)aTexture.height);
                                if (!uvDictionary.ContainsKey(aColor))
                                {
                                    uvDictionary.Add(aColor, result);
                                }
                                return result;
                            }
                        }
                    }
                }               
            }
            return Vector2.zero;            
        }

        private bool IsRGBAColorsEquals(Color a, Color b, int aTolerance)
        {
            int r1 = (int)(a.r * 255f);
            int g1 = (int)(a.g * 255f);
            int b1 = (int)(a.b * 255f);
            int a1 = (int)(a.a * 255f);

            int r2 = (int)(b.r * 255f);
            int g2 = (int)(b.g * 255f);
            int b2 = (int)(b.b * 255f);
            int a2 = (int)(b.a * 255f);            
            if ((Mathf.Abs(r1 - r2) <= aTolerance) && (Mathf.Abs(b1 - b2) <= aTolerance) && (Mathf.Abs(g1 - g2) <= aTolerance) && (Mathf.Abs(a1 - a2) <= aTolerance))
                return true;
            else
                return false;
        }

        //Build all the neccesarry faces for a voxel
        //It is only going to build the corresponding face, if the voxel has no neightboor on that side   
        internal void BuildFacesForVoxel(Voxel aVoxel, List<Vector3> @aVertices, List<int> @aPolygons, List<Vector2> @aUvs, Texture2D aTexture, int aTextureWidth=0, int aTextureHeight=0)
        {        
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.down))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.down / 2f), Vector3.left, Vector3.back, aTextureWidth, aTextureHeight);
            }
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.up))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.up / 2f), Vector3.right, Vector3.back, aTextureWidth, aTextureHeight);
            }
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.left))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.left / 2f), Vector3.up, Vector3.back, aTextureWidth, aTextureHeight);
            }
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.right))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.right / 2f), Vector3.down, Vector3.back, aTextureWidth, aTextureHeight);
            }
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.forward))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.forward / 2f), Vector3.right, Vector3.up, aTextureWidth, aTextureHeight);
            }
            if (aVoxel.IsPossibleExtrudeDirection(Vector3.back))
            {
                BuildPolygonsForFace(aTexture, aVoxel.color, aVertices, aPolygons, aUvs, aVoxel.position + (Vector3.back / 2f), Vector3.right, Vector3.down, aTextureWidth, aTextureHeight);
            }
        }

        private void UpdateTheContainersWithTheSameModelName(Mesh aGeneratedMesh, Material aMaterial)
        {
            VoxelContainer[] containerList = Resources.FindObjectsOfTypeAll<VoxelContainer>();
            foreach (VoxelContainer curContainer in containerList)
            {
                if ((this != curContainer) && (curContainer.modelName == this.modelName))
                {
                    if (this.EqualVoxelContainers(curContainer))
                    {
                        MeshFilter curMeshFilter = curContainer.gameObject.GetComponent<MeshFilter>();
                        if (curMeshFilter == null)
                            curMeshFilter = curContainer.gameObject.AddComponent<MeshFilter>();                        
                        curMeshFilter.sharedMesh = Instantiate(aGeneratedMesh);

                        MeshCollider curCollider = curContainer.gameObject.GetComponent<MeshCollider>();
                        if (curCollider == null)
                            curCollider = curContainer.gameObject.AddComponent<MeshCollider>();
                        curCollider.sharedMesh = aGeneratedMesh;                        

                        if (aMaterial != null)
                        {
                            Renderer curRenderer = curContainer.gameObject.GetComponent<Renderer>();
                            if (curRenderer == null)
                                curRenderer = curContainer.gameObject.AddComponent<Renderer>();
                            curRenderer.sharedMaterial = aMaterial;
                        }                        
                    }             
                }
            }                        
        }

        //Return true if there is no container with the same identifier name or 
        //they have the same name and their structure is the same. (So they are not modified)
        private bool CheckEqualitiesByModelName()
        {
            VoxelContainer[] containerList = Resources.FindObjectsOfTypeAll<VoxelContainer>();
            foreach (VoxelContainer curContainer in containerList)
            {
                if ((this!=curContainer) && (curContainer.modelName == this.modelName))
                {
                    if (!this.EqualVoxelContainers(curContainer))
                        return false;
                }
            }

            return true;
        }


        //Flag for the editor
        public bool meshAssetUpdatedInLastBuild = true;

        //The main unoptimized mesh build function
        //VoxelAreas build their mesh data, this function merge them together,
        //and update the texture, polygon and collider data
        public void BuildMesh(bool aDisplayProgressbar, bool aBuildTexture, bool aSaveMesh, bool aUpdateCollider)
        {
            this.WaitForSerialization();

            //flag will be set in the asset save part
            meshAssetUpdatedInLastBuild = false;                

            long startTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            Material texturedMaterial = null;
            Texture2D voxelTexture = null;
            
            //If it does not have assets name yet
            //or the model is modified and it needs new asset files
            if (!Application.isPlaying)
            if ((this.modelName=="") || ((this.modelName != this.gameObject.name) && (!this.CheckEqualitiesByModelName())))
            {
                this.modelName = this.gameObject.name;
                aBuildTexture = true;
                aSaveMesh = true;
                aUpdateCollider = true;
                this.materialFilename = "";
                this.modelFilename = "";
                this.textureFilename = "";
            }            

            //Building texture and material
            if (aDisplayProgressbar)
            {
                #if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayCancelableProgressBar("Building Texture", "Building", 0f);
                #endif
            }
            if (this.GetComponent<Renderer>() == null)
            {
                this.gameObject.AddComponent<MeshRenderer>();
            }
            try
            {
                this.optimized = false;
                //preparing texture and material
                if ((aBuildTexture) || (aSaveMesh))
                {
                    StaticValues.CheckAndCreateFolders();

                    //Prepare texture filename;
                    string textureFilename = this.GetTextureFilename();
                    bool isTextureRebuilded = false;

                    voxelTexture = this.BuildTexture(this, out isTextureRebuilded, aDisplayProgressbar);
                    if (voxelTexture == null)
                        return;//Texture build probably canceled

                    if (isTextureRebuilded)
                    {
                        uvDictionary.Clear();
                        this.PrepareTextureAndUVDictionary(true);
                    }

                    texturedMaterial = this.GetComponent<Renderer>().sharedMaterial;

#if UNITY_EDITOR
                    if (texturedMaterial != null)
                    {
                        if ((AssetDatabase.GetAssetPath(texturedMaterial) != this.GetMaterialFileName()) && (isTextureRebuilded))
                        {
                            texturedMaterial = new Material(texturedMaterial);
                            this.GetComponent<Renderer>().sharedMaterial = texturedMaterial;
                        }
                    }
#endif
                    if ((isTextureRebuilded) || (texturedMaterial == null))
                    {     
                        this.SetAllAreaChanged();//texture build might change uv coordinates

                        string materialShader = "Standard";
                        if (texturedMaterial == null)
                          texturedMaterial = new Material(Shader.Find(materialShader));

                        texturedMaterial.mainTexture = ReImportTexture(textureFilename);
                        this.GetComponent<Renderer>().sharedMaterial = texturedMaterial;

                        aSaveMesh = true;
                        aUpdateCollider = true;
                        this.SetAllAreaChanged();
                    }
                    else
                    {
                        aBuildTexture = false;
                        if (texturedMaterial != null)
                            voxelTexture = (Texture2D)texturedMaterial.mainTexture;
                    }
                }
                else
                {
                    texturedMaterial = this.GetComponent<Renderer>().sharedMaterial;
                    if (texturedMaterial == null) return;
                    voxelTexture = (Texture2D)texturedMaterial.mainTexture;
                }
            }
            catch (UnityException e)
            {
                Debug.LogError("Could not create texture or material for the merged mesh." + e.Message);
                return;
            }
            finally
            {
                #if UNITY_EDITOR
                if (aDisplayProgressbar)
                    EditorUtility.ClearProgressBar();
                #endif
            }

            if ((!Application.isPlaying) || (this.voxelAreas == null))
            {
                //Prepare Voxel Areas
                //But do it only in editor or if it is absoluty neccesary!
                this.GenerateVoxelAreas();
            }

            //Building Mesh
            if (aDisplayProgressbar)
            {
                #if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayCancelableProgressBar("Building Mesh", "Building", 0f);
                #endif
            }
            try
            {
                GameObject baseGameObject = this.gameObject;
                MeshRenderer curRenderer = baseGameObject.GetComponent<MeshRenderer>();
                if (curRenderer == null)
                    curRenderer = baseGameObject.AddComponent<MeshRenderer>();

                int curStep = 0;

                MeshFilter curMeshFilter = baseGameObject.GetComponent<MeshFilter>();
                if (curMeshFilter == null)
                    curMeshFilter = baseGameObject.AddComponent<MeshFilter>();
                Mesh generatedMesh = new Mesh();
                curMeshFilter.mesh = generatedMesh;
                baseGameObject.GetComponent<Renderer>().material = texturedMaterial;

                List<Vector3> vertices = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> triangles = new List<int>();
                int curAreaIndex=0;               
                foreach (VoxelArea curArea in this.voxelAreas.Values)
                {
                    //Force progressbar on slow builds
                    if (((System.DateTime.Now.Ticks / TimeSpan.TicksPerSecond) - startTime) > 30)
                        aDisplayProgressbar = true;

                    if (curArea.isChanged)
                    {
                        curArea.SetTexture(voxelTexture);
                        curArea.GenerateBuffers();
                    }                        

                    int prevVerticeCount = vertices.Count;
                    int prevTriangledCount = triangles.Count;

                    vertices.AddRange(curArea.vertices);
                    triangles.AddRange(curArea.triangles);
                    uvs.AddRange(curArea.uvs);

                    if (vertices.Count > 65000)
                    {
                        Debug.LogWarning("Too many voxels. VoxeMax going to separate the objects. Sorry. :(");
                        this.DivideContainerIntoFragments(this.transform);
                        return;
                    }

                    for (int i = prevTriangledCount; i < triangles.Count; i++)
                    {
                        triangles[i] += prevVerticeCount;
                    }

                    if (aDisplayProgressbar)
                    {
#if UNITY_EDITOR                        
                        if (EditorUtility.DisplayCancelableProgressBar("Building Mesh " + this.gameObject.name, "Building", (float)curAreaIndex / (float)this.voxelAreas.Count))
                            throw new Exception("Mesh build Canceled by the user.");
#endif
                    }
                    curAreaIndex++;
                }
                                
                generatedMesh.vertices = vertices.ToArray();
                generatedMesh.triangles = triangles.ToArray();
                generatedMesh.uv = uvs.ToArray();

                generatedMesh.RecalculateNormals();

                vertices.Clear();
                vertices = null;
                triangles.Clear();
                triangles = null;
                uvs.Clear();
                uvs = null;

                curStep++;
                //Save material
                if (aBuildTexture)
                {
                    #if UNITY_EDITOR
                    string materialFilename = this.GetMaterialFileName();       
                    if (!File.Exists(materialFilename))                                 
                        AssetDatabase.CreateAsset(texturedMaterial, materialFilename);                    
                    #endif
                }
                
                if (aSaveMesh)
                {
                    #if UNITY_EDITOR
                    string meshFilename = this.GetModelFileName();                                        
                    AssetDatabase.CreateAsset(generatedMesh, meshFilename);
                    this.meshAssetUpdatedInLastBuild = true;
                    #endif

                    curMeshFilter.sharedMesh = generatedMesh;
                }

                if (aUpdateCollider)
                {
                    this.UpdateCollider(generatedMesh);
                }

                if ((aBuildTexture) || (aSaveMesh))
                {
                    this.UpdateTheContainersWithTheSameModelName(generatedMesh, texturedMaterial);
                }
            }
            finally
            {
                #if UNITY_EDITOR
                if (aDisplayProgressbar)
                    EditorUtility.ClearProgressBar();
                #endif
            }
        }

        //It saves the mesh data in editor mode, this is used by the builder functions
        public void SaveMeshAndUpdateCollider(){
#if UNITY_EDITOR
            MeshFilter meshFilter = this.gameObject.GetComponent<MeshFilter>();
            if (meshFilter!=null){                
                string meshFilename = this.GetModelFileName();
                AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshFilename);                                
            }
#endif
        }

        //Updates the collider, this function is used by the builder functions.
        private void UpdateCollider(Mesh aMesh)
        {
            //Update Collider
            Collider curCollider = this.gameObject.GetComponent<Collider>();
            if ((curCollider != null) && (curCollider is MeshCollider))
            {             
                ((MeshCollider)curCollider).sharedMesh = aMesh;
            }
            else
            {
                if (curCollider != null)
                {
                    DestroyImmediate(curCollider);
                }
                MeshCollider meshCollider = this.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = aMesh;
            }
        }

        //Slice up the model for smaller areas
        //so the system has easier job to calculate them, also this way it does not
        //need to recalculate the whole model on destruction
        public void GenerateAreaBuffers()
        {
            int changedAreaCount = 0;
           
            foreach (VoxelArea curArea in this.voxelAreas.Values)
            {
                if (curArea.isChanged)
                {
                    curArea.GenerateBuffers();
                    changedAreaCount++;
                }
            }
        
            if (changedAreaCount > 0)
            {
                this.mustRebuildBeforeUpdate = true;
            }
        }        
       
        //Divide the container into two
        private void DivideContainerIntoFragments(Transform aTransformParent)
        {
            Vector3 minVector = this.GetMinContainerVector();
            Vector3 maxVector = this.GetMaxContainerVector();
            Vector3 difVector = maxVector - minVector;

            int axisType = 0;
            if ((difVector.x > difVector.y) && (difVector.x > difVector.z))
            {
                difVector.x = difVector.x / 2;
                axisType = 0;
            } else
            if ((difVector.y > difVector.x) && (difVector.y > difVector.z))
            {
                difVector.y = difVector.y / 2;
                axisType = 1;
            } else
            if (((difVector.z > difVector.x) && (difVector.z > difVector.y)) || (difVector.z == difVector.y))
            {
                difVector.z = difVector.z / 2;
                axisType = 2;
            }
            else
            {
                axisType = 0;
                difVector.x = difVector.x / 2;
            }
            difVector = minVector + difVector;

            GameObject newGameObject = new GameObject();
            newGameObject.name = this.gameObject.name;
            newGameObject.transform.parent = aTransformParent;
            newGameObject.transform.position = this.gameObject.transform.position;
            VoxelContainer curContainer = newGameObject.AddComponent<VoxelContainer>();
            switch (axisType)
            {
                case 0:
                    for (int x = (int)minVector.x; x <= difVector.x; x++)
                    {
                        for (int y = (int)minVector.y; y <= maxVector.y; y++)
                        {
                            for (int z = (int)minVector.z; z <= maxVector.z; z++)
                            {
                                Vector3 curPos = new Vector3(x, y, z);
                                if (this.voxels.ContainsKey(curPos)) {
                                    curContainer.AddVoxel(this.voxels[curPos], false);
                                    this.RemoveVoxel(this.voxels[curPos], false);
                                }
                            }
                        }
                    }
                    break;
                case 1:
                    for (int y = (int)minVector.y; y <= difVector.y; y++)
                    {
                        for (int x = (int)minVector.x; x <= maxVector.x; x++)
                        {
                            for (int z = (int)minVector.z; z <= maxVector.z; z++)
                            {
                                Vector3 curPos = new Vector3(x, y, z);
                                if (this.voxels.ContainsKey(curPos))
                                {
                                    curContainer.AddVoxel(this.voxels[curPos], false);
                                    this.RemoveVoxel(this.voxels[curPos], false);
                                }
                            }
                        }
                    }
                    break;
                case 2:
                    for (int z = (int)minVector.z; z <= difVector.z; z++)
                    {
                        for (int x = (int)minVector.x; x <= maxVector.x; x++)
                        {
                            for (int y = (int)minVector.y; y <= maxVector.y; y++)
                            {
                                Vector3 curPos = new Vector3(x, y, z);
                                if (this.voxels.ContainsKey(curPos))
                                {
                                    curContainer.AddVoxel(this.voxels[curPos], false);
                                    this.RemoveVoxel(this.voxels[curPos], false);
                                }
                            }
                        }
                    }
                    break;
            }
                       
            
            if (curContainer.voxels.Count == 0)
            {
                Destroy(newGameObject);               
            }
            else
            {
                this.UpdateStructure();
                if (curContainer.GetEstimatedVerticleCount() > 40000)
                    curContainer.DivideContainerIntoFragments(aTransformParent);
                else
                    curContainer.BuildMesh(true, true, true, true);
            }


            this.UpdateStructure();
            if (this.GetEstimatedVerticleCount() > 40000)
                this.DivideContainerIntoFragments(aTransformParent);
            else
            {                
                this.BuildMesh(true, true, true, true);
            }
        }


        //Build a new container from the selected voxels
        public void SeparateSelectedVoxels()
        {
            GameObject newGameObject            = new GameObject();
            newGameObject.name                  = this.gameObject.name + "Separated";
            newGameObject.transform.parent      = this.gameObject.transform;
            newGameObject.transform.position    = this.gameObject.transform.position;            
            VoxelContainer newContainer         = newGameObject.AddComponent<VoxelContainer>();
          
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (curVoxel.selected)
                {
                    newContainer.AddVoxel(curVoxel);
                }                
            }
            foreach (Voxel curVoxel in newContainer.voxels.Values)
            {
                this.RemoveVoxel(curVoxel);                
            }
            this.ClearVoxelAreas();            
            this.GenerateVoxelAreas();
            this.BuildMesh(true, true, true, true);
            newContainer.BuildMesh(true, true, true, true);
        }
        

        //Removed the voxel areas
        private void ClearVoxelAreas()
        {
            this.voxelAreas.Clear();
        }

        //Prepare the list of voxel areas 
        private void GenerateVoxelAreas()
        {            
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                this.GetVoxelAreaForVoxel(curVoxel);
            }
        }

        //Create or find the existing voxel area for a voxel
        internal VoxelArea GetVoxelAreaForVoxel(Voxel aVoxel)
        {
            int x = (int)(((int)aVoxel.position.x) / StaticValues.voxelSpaceSize);
            int y = (int)(((int)aVoxel.position.y) / StaticValues.voxelSpaceSize);
            int z = (int)(((int)aVoxel.position.z) / StaticValues.voxelSpaceSize);

            if ((aVoxel.position.x < 0f) && ((((int)aVoxel.position.x) % StaticValues.voxelSpaceSize)!=0))
                x--;
            if ((aVoxel.position.y < 0f) && ((((int)aVoxel.position.y) % StaticValues.voxelSpaceSize)!=0))
                y--;
            if ((aVoxel.position.z < 0f) && ((((int)aVoxel.position.z) % StaticValues.voxelSpaceSize) != 0))
                z--;
            Vector3 newVector = new Vector3(x, y, z);
            if (!voxelAreas.ContainsKey(newVector))
            {
                VoxelArea newVoxelArea = new VoxelArea(newVector, this);             
                this.voxelAreas.Add(newVector, newVoxelArea);
            }
            this.voxelAreas[newVector].AddVoxel(aVoxel);            
            return this.voxelAreas[newVector];
        }
        #endregion

        #region BuildColliderFunctions
        internal class ColliderDescriptor
        {
            public Vector3 point;
            public Vector3 direction;
            public bool IsPositionContained(Vector3 aPosition)
            {                
                Vector3 maxPos = point + (direction - Vector3.one);
                if ((point.x <= aPosition.x) && (point.y <= aPosition.y) && (point.z <= aPosition.z) &&
                    (maxPos.x >= aPosition.x) && (maxPos.y >= aPosition.y) && (maxPos.z >= aPosition.z))
                    return true;
                else
                    return false;
            }
        }

        public void CreateBoxColliders()
        {
            this.ClearColliders();
            List<ColliderDescriptor> descriptorList = this.PrepareColliderDescriptors();
            this.CreateBoxColliders(descriptorList);
        }
        public void ClearBoxColliders()
        {
            BoxCollider[] colliders = this.GetComponents<BoxCollider>();
            foreach (BoxCollider curCollider in colliders)
            {
                DestroyImmediate(curCollider);
            }
        }

        internal void CreateBoxColliders(List<ColliderDescriptor> aColliderDescList)
        {
            this.ClearColliders();
            foreach(ColliderDescriptor curDescriptor in aColliderDescList)
            {
                BoxCollider newCollider = this.gameObject.AddComponent<BoxCollider>();
                newCollider.center = curDescriptor.point + ((curDescriptor.direction - Vector3.one) /2f);
                newCollider.size = curDescriptor.direction;
            }
        }

        internal List<ColliderDescriptor> PrepareColliderDescriptors()
        {
            Dictionary<Vector3, Voxel> localContainer;
            Vector3 smallestCoord = Vector3.zero;
            Vector3 largestCoord = Vector3.zero;
            lock (this.voxels)
            {
                smallestCoord = this.GetMinContainerVector();
                largestCoord = this.GetMaxContainerVector();                
                localContainer = new Dictionary<Vector3, Voxel>(this.voxels);
            }
            List<ColliderDescriptor> result = new List<ColliderDescriptor>();

            bool loopBreaked = false;
            for (int x = (int)smallestCoord.x; x <= largestCoord.x; x++)
            {
                for (int y = (int)smallestCoord.y; y <= largestCoord.y; y++)
                {
                    for (int z = (int)smallestCoord.z; z <= largestCoord.z; z++)
                    {
                        //if (this.GetVoxelCountInColliderDescs(result) < this.voxels.Count)
                       // {
                        Vector3 curPos = new Vector3(x, y, z);
                        if ((!PointContainedInColliderDescList(curPos, result)) && (localContainer.ContainsKey(curPos)))                            
                            this.PrepareColliderDescForPoint(curPos, largestCoord, result, localContainer);
                      //  }
                        /*else
                        {
                            loopBreaked = true;
                            break;
                        }*/
                    }
                    if (loopBreaked) break;
                }
                if (loopBreaked) break;
            }            

            return result;
        }

        private bool PointContainedInColliderDescList(Vector3 aPoint, List<ColliderDescriptor> aColliderDescs)
        {
            foreach (ColliderDescriptor curDesc in aColliderDescs)
            {
                if (curDesc.IsPositionContained(aPoint)) return true;
            }
            return false;
        }

        private int GetVoxelCountInColliderDescs(List<ColliderDescriptor> aColliderDescs)
        {
            int result = 0;
            foreach (ColliderDescriptor curDescriptor in aColliderDescs)
            {
                result += (int) (curDescriptor.direction.x * curDescriptor.direction.y * curDescriptor.direction.z);
            }
            return result;
        }

        private void PrepareColliderDescForPoint(Vector3 aPoint, Vector3 largestCoord, List<ColliderDescriptor> aColliderDescs, Dictionary<Vector3, Voxel> aLocalContainer)
        {
            Vector3 maxDirectionVector = Vector3.zero;


            Vector3 maxDistance = Vector3.zero;
            for (int x=(int)aPoint.x; x<=largestCoord.x; x++)
            {
                Vector3 curPos = aPoint;
                curPos.x = x;
                if (!aLocalContainer.ContainsKey(curPos))
                    break;
                maxDistance.x = x;
            }

            for (int y = (int)aPoint.y; y <= largestCoord.y; y++)
            {
                Vector3 curPos = aPoint;
                curPos.y = y;
                if (!aLocalContainer.ContainsKey(curPos))
                    break;
                maxDistance.y = y;
            }

            for (int z = (int)aPoint.z; z <= largestCoord.z; z++)
            {
                Vector3 curPos = aPoint;
                curPos.z = z;
                if (!aLocalContainer.ContainsKey(curPos))
                    break;
                maxDistance.z = z;
            }

            maxDistance = maxDistance - aPoint;

            for (int factorx = 0; factorx <= maxDistance.x; factorx++)
                for (int factory = 0; factory <= maxDistance.y; factory++)
                    for (int factorz = 0; factorz <= maxDistance.z; factorz++) { 
                        bool searchBreaked = false;
                        for (int x = (int)aPoint.x; x <= aPoint.x + factorx; x++)
                        {
                            for (int y = (int)aPoint.y; y <= aPoint.y + factory; y++)
                            {
                                for (int z = (int)aPoint.z; z <= aPoint.z + factorz; z++)
                                {
                                    Vector3 curPos = new Vector3(x, y, z);
                                    if ((!aLocalContainer.ContainsKey(curPos)) || (PointContainedInColliderDescList(curPos, aColliderDescs)))
                                    {
                                        searchBreaked = true;
                                        break;

                                    }
                                    Vector3 curMaxDirectionVector = new Vector3(x - aPoint.x, y - aPoint.y, z - aPoint.z);
                                    if (curMaxDirectionVector.magnitude > maxDirectionVector.magnitude)
                                        maxDirectionVector = curMaxDirectionVector;
                                }
                                if (searchBreaked) break;
                            }
                            if (searchBreaked) break;
                        }
                    }

            maxDirectionVector = maxDirectionVector + Vector3.one;

            ColliderDescriptor newColliderDesc = new ColliderDescriptor();
            newColliderDesc.point = aPoint;
            newColliderDesc.direction = maxDirectionVector;
            aColliderDescs.Add(newColliderDesc);
        }

        internal void ClearColliders()
        {
            Collider[] colliders = this.GetComponents<Collider>();
            foreach(Collider curCollider in colliders)
            {
                DestroyImmediate(curCollider);
            }
        }        
        #endregion

        #region EditorFunctions
        public List<Voxel> GetSelectedVoxels()
        {
            List<Voxel> result = new List<Voxel>();
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (curVoxel.selected)
                {
                    result.Add(curVoxel);
                }
            }
            return result;
        }

        public bool HasSelectedVoxels()
        {
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (curVoxel.selected)
                {
                    return true;
                }
            }
            return false;
        }

        public void SelectAllVoxels()
        {
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                curVoxel.selected = true;
            }
        }

        public void DeselectAllVoxels()
        {
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                curVoxel.selected = false;
            }
        }

        public List<Voxel> FloodSelect(Voxel aVoxel, float aColorTolerance)
        {
            Dictionary<Vector3, Voxel> result = new Dictionary<Vector3, Voxel>();
            this.SelectSimilarColoredVoxels(aVoxel, aVoxel.color, result, aColorTolerance);
            //return null;
            return new List<Voxel>(result.Values);
        }

        private void SelectSimilarColoredVoxels(Voxel aVoxel, Color aVoxelColor, Dictionary<Vector3, Voxel> voxelList, float aColorTolerance)
        {            
            if (aVoxel == null) return;                           

            if (this.selectMode != SelectModes.GlobalMode)
            {
                voxelList.Add(aVoxel.position, aVoxel);
                Vector3[] directionVectors = new Vector3[]{ Vector3.left, -Vector3.left, Vector3.up, -Vector3.up, Vector3.forward, -Vector3.forward };
                Dictionary<Vector3, Voxel> newVoxelList = new Dictionary<Vector3, Voxel>();
                newVoxelList.Add(aVoxel.position, aVoxel);

                while (newVoxelList.Count > 0)
                {
                    Dictionary<Vector3, Voxel> nextList = new Dictionary<Vector3, Voxel>();
                    foreach (Voxel curVoxel in newVoxelList.Values)
                    {
                        for (int i = 0; i < directionVectors.Length; i++) {
                            Vector3 newPosition = curVoxel.position + directionVectors[i];
                            if ((!nextList.ContainsKey(newPosition)) && (!newVoxelList.ContainsKey(newPosition) && (!voxelList.ContainsKey(newPosition))))
                            {
                                Voxel neighbour = this.GetVoxelByCoordinate(curVoxel.position + directionVectors[i]);                                
                                if ((neighbour != null) && (Vector4.Distance(aVoxelColor, neighbour.color)<=(Vector4.Distance(Vector4.zero, Vector4.one)*aColorTolerance)))
                                {
                                    nextList.Add(neighbour.position, neighbour);                            
                                }
                            }
                        }                     

                        if (!voxelList.ContainsKey(curVoxel.position)) voxelList.Add(curVoxel.position, curVoxel);
                    }
                    newVoxelList = null;
                    newVoxelList = nextList;
                }                                        
            }
            else
            {
                foreach (Voxel curVoxel in this.voxels.Values)
                {
                    if (Vector4.Distance(aVoxelColor, curVoxel.color) <= (Vector4.Distance(Vector4.zero, Vector4.one) * aColorTolerance)) 
                        voxelList.Add(curVoxel.position, curVoxel);
                }
            }            
        }

        public void DeleteSelectedVoxels()
        {
            List<Voxel> selectionList = this.GetSelectedVoxels();
            foreach (Voxel curVoxel in selectionList)
            {
                this.RemoveVoxel(curVoxel);
            }
        }

        public List<Color> GetColorPalette()
        {
            Dictionary<Color, int> colorList = new Dictionary<Color, int>();
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (!colorList.ContainsKey(curVoxel.color))
                {
                    colorList.Add(curVoxel.color, 1);
                }
                else
                {
                    colorList[curVoxel.color] = colorList[curVoxel.color] + 1;
                }

            }

            List<Color> result = new List<Color>();
            foreach (Color curColor in colorList.Keys)
            {
                result.Add(curColor);
            }
            
            return result;

        }
        public List<Color> GetOrderedColorPalette()
        {
            Dictionary<Color, int> colorList = new Dictionary<Color, int>();           
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (!colorList.ContainsKey(curVoxel.color))
                {
                    colorList.Add(curVoxel.color, 1);                    
                }
                else
                {
                    colorList[curVoxel.color]= colorList[curVoxel.color] + 1;
                }

            }
            List<Color> result = new List<Color>();
            while (colorList.Count > 0)
            {
                int curNumber = 0;
                Color maxColor=new Color();
                foreach(Color curColor in colorList.Keys)
                {
                    if (colorList[curColor] > curNumber)
                    {
                        curNumber = colorList[curColor];
                        maxColor = curColor;
                    }
                }
                result.Add(maxColor);
                colorList.Remove(maxColor);                
            }
            return result;
        }

        public void SetAllAreaChanged()
        {            
            foreach (VoxelArea curArea in this.voxelAreas.Values)
            {
                curArea.isChanged = true;
            }
        }

        public void UpdateAllNeighborList()
        {         
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                curVoxel.UpdateNeighborList();
            }
        }

        public bool IsChanged()
        {
            foreach (VoxelArea curArea in this.voxelAreas.Values)
            {
                if (curArea.isChanged) return true;
            }
            return false;
        }

        /*
         * You should call this function if you modified the voxel list          
         * without using the neccesary update functions per voxel.
         */
        public void UpdateStructure()
        {
            this.ClearVoxelAreas();
            this.GenerateVoxelAreas();
            this.UpdateAllNeighborList();
        }
        #endregion

        #region MeshOptimizerFunctions
        internal void AssignUVDictionary(VoxelContainer aVoxelContainer)
        {
            this.uvDictionary = null;
            this.uvDictionary = new Dictionary<Color, Vector2>(aVoxelContainer.uvDictionary);
            /*this.uvDictionary.Clear();
            foreach (Color curColor in aVoxelContainer.uvDictionary.Keys)
            {
                this.uvDictionary.Add(curColor, aVoxelContainer.uvDictionary[curColor]);
            }*/
        }

        public void ClearUVDictionary()
        {
            if (this.uvDictionary != null)
                this.uvDictionary.Clear();
        }

        public Texture2D PrepareTextureAndUVDictionary(bool aForceRebuildUvMapCoords)
        {
            Material texturedMaterial = this.GetComponent<Renderer>().sharedMaterial;
            Texture2D voxelTexture = null;

            if ((texturedMaterial == null))
            {
#if UNITY_EDITOR
                //      AssetDatabase.Refresh();
#endif
                string materialShader = "Standard";
                texturedMaterial = new Material(Shader.Find(materialShader));
                texturedMaterial.mainTexture = ReImportTexture(textureFilename);
                voxelTexture = (Texture2D)texturedMaterial.mainTexture;
                this.GetComponent<Renderer>().sharedMaterial = texturedMaterial;

                this.SetAllAreaChanged();
            }
            else
            {
                if (texturedMaterial != null)
                    voxelTexture = (Texture2D)texturedMaterial.mainTexture;
            }
            //prepare the color list so the thread will not try to get it from the texture
            List<Color> colors = this.GetColorPalette();           
            foreach (Color curColor in colors)
            {
                this.GetUvOfColor(voxelTexture, curColor);
            }
            
            return voxelTexture;
        }

        private void StartOptimizationInThread()
        {
            optimizationFinnished = false;

            this.optmizedVertices = null;
            this.optmizedUvs = null;
            this.optimzedTriangles = null;

            Material texturedMaterial = this.GetComponent<Renderer>().sharedMaterial;
            Texture2D voxelTexture = null;
            if (texturedMaterial != null)
            {
                voxelTexture = (Texture2D)texturedMaterial.mainTexture;
            }
            if (voxelTexture == null)
                return;

            ParamForOptimizerThread param = new ParamForOptimizerThread();
            param.voxelTexture = voxelTexture;
            param.textureWidth = voxelTexture.width;
            param.textureHeight = voxelTexture.height;
            ThreadPool.QueueUserWorkItem(OptimizeInThread, param);
        }

 
        private void OptimizeInThread(System.Object aParam)
        {            
            this.BuildOptimizedMesh(false, false, (ParamForOptimizerThread)aParam);
        }

        public void BuildOptimizedMesh(bool aShowProgressBar, bool aPushToBuffers = true, System.Object aTextureParam = null)
        {
            CancelOptimizationInThread = false;            
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUvs = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            List<Vector3> xVectors = new List<Vector3>() { Vector3.up, Vector3.up, Vector3.right, Vector3.forward, Vector3.right, Vector3.right};
            List<Vector3> yVectors = new List<Vector3>() { Vector3.back, Vector3.forward, Vector3.back, Vector3.left, Vector3.up, Vector3.down };
            List<Vector3> offsets  = new List<Vector3>() { Vector3.left, -Vector3.left, Vector3.up, -Vector3.up, Vector3.forward, -Vector3.forward };

            try
            {
                ParamForOptimizerThread textureParam = (ParamForOptimizerThread)aTextureParam;
                if (aTextureParam == null)
                {
                    textureParam = new ParamForOptimizerThread();
                    textureParam.voxelTexture  = this.PrepareTextureAndUVDictionary(true);
                    textureParam.textureWidth  = textureParam.voxelTexture.width;
                    textureParam.textureHeight = textureParam.voxelTexture.height;
                }

                for (int i = 0; i < 6; i++)
                {
                    List<VoxelLayer> layers = new List<VoxelLayer>();
                    BuildLayerList(layers, i / 2, offsets[i]);
                    if (!this.BuildMeshFromLayerList(layers, textureParam, newVertices, newUvs, newTriangles, xVectors[i], yVectors[i], offsets[i]/2f))
                        return;
                    if (aShowProgressBar)
                    {
#if UNITY_EDITOR
                        if (EditorUtility.DisplayCancelableProgressBar("Building optimized mesh", (i / 6f).ToString("0.00") + "% ", (i / 6f)))
                            return;
#endif
                    }
                    if (CancelOptimizationInThread) return;
                }
                this.optmizedVertices = newVertices.ToArray();
                this.optmizedUvs = newUvs.ToArray();
                this.optimzedTriangles = newTriangles.ToArray();
                if (aPushToBuffers)
                    this.PushOptmizedDataToBuffers(true);
            }
            finally
            {
#if UNITY_EDITOR
                if (aShowProgressBar) EditorUtility.ClearProgressBar();
#endif
                optimizationFinnished = true;
            }
        }

        private bool CancelOptimizationInThread = false;
        private Vector3[] optmizedVertices = null;
        private Vector2[] optmizedUvs = null;
        private int[] optimzedTriangles = null;
        private bool optimizationFinnished = false;
        private void PushOptmizedDataToBuffers(bool aUpdateContent = true)
        {
            MeshFilter curMeshFilter = this.gameObject.GetComponent<MeshFilter>();
            if (curMeshFilter == null)
                curMeshFilter = this.gameObject.AddComponent<MeshFilter>();
            Mesh generatedMesh = new Mesh();
            curMeshFilter.mesh = generatedMesh;

            generatedMesh.vertices = this.optmizedVertices;
            generatedMesh.uv = this.optmizedUvs;
            generatedMesh.triangles = this.optimzedTriangles;

            generatedMesh.RecalculateNormals();

#if UNITY_EDITOR
            if (aUpdateContent)
            {                
                string meshFilename = this.GetModelFileName();
                AssetDatabase.CreateAsset(generatedMesh, meshFilename);                
                this.UpdateTheContainersWithTheSameModelName(generatedMesh, null);                
            }
#endif
        //    curMeshFilter.mesh = generatedMesh;
            //if(aUpdateContent)
          

            this.UpdateCollider(generatedMesh);

            this.optimized = true;
            this.optimizationFinnished = false;
        }

        private void BuildLayerList(List<VoxelLayer> aLayers, int aDimensionIndex, Vector3 aFaceDirection)
        {
            Vector3 smallestCoord = this.GetSmallestCoord();
            Vector3 largestCoord = this.GetLargestCoord();

            for (int i = (int)smallestCoord[aDimensionIndex]; i <= largestCoord[aDimensionIndex]; i++)
            {
                foreach (Voxel curVoxel in this.voxels.Values)
                {
                    if ((((int)curVoxel.position[aDimensionIndex]) == i) && (curVoxel.IsPossibleExtrudeDirection(aFaceDirection)))
                    {
                        VoxelLayer curLayer = this.FindVoxelLayerByLayerDepthAndColor(aLayers, i, curVoxel.color);
                        if (curLayer == null)
                        {
                            curLayer = new VoxelLayer(i, curVoxel.color);
                            aLayers.Add(curLayer);
                        }
                        curLayer.voxels.Add(curVoxel.position, curVoxel);
                    }
                }
            }
        }

  
        public int optmalisationMinMergableVoxels = 250;
        private bool BuildMeshFromLayerList(List<VoxelLayer> aLayers, ParamForOptimizerThread aTextureParam, List<Vector3> aVertices, List<Vector2> aUvs, List<int> aTriangles, Vector3 aVectorX, Vector3 aVectorY, Vector3 aOffset)
        {    
            List<VoxelMeshOptimizer> threadStorage = new List<VoxelMeshOptimizer>();

            foreach (VoxelLayer curLayer in aLayers)
            {
                VoxelMeshOptimizer newThreadStorage = new VoxelMeshOptimizer();
                newThreadStorage.Init(curLayer, aTextureParam, aVectorX, aVectorY, aOffset, this.optmalisationMinMergableVoxels, this);
                threadStorage.Add(newThreadStorage);
                if (CancelOptimizationInThread) return false;
            }
                
            Thread[] optimizerThreads = new Thread[StaticValues.maxThreadCount];

            //Handle the thread's work
            int i = 0;
            while (i < threadStorage.Count)
            {
                for (int j = 0; j < StaticValues.maxThreadCount; j++)
                {                        
                    if ((optimizerThreads[j] != null) && (optimizerThreads[j].ThreadState == ThreadState.Stopped))
                    {
                        optimizerThreads[j] = null;
                    }
                    if (optimizerThreads[j] == null)
                    {
                        optimizerThreads[j] = new Thread(new ThreadStart(threadStorage[i].Start));
                        optimizerThreads[j].Start();
                        i++;
                        if (i >= threadStorage.Count) break;
                    }
                }
            }
            //Wait for all the threads
            for (int j = 0; j < StaticValues.maxThreadCount; j++)
            {
                if ((optimizerThreads[j] != null) && (optimizerThreads[j].ThreadState != ThreadState.Stopped))
                {
                    optimizerThreads[j].Join();                     
                }
            }
            //Merge the array
            foreach (VoxelMeshOptimizer curThreadStoreage in threadStorage)
            {
                int originalVertexCount=aVertices.Count;
                aVertices.AddRange(curThreadStoreage.vertices);
                aUvs.AddRange(curThreadStoreage.uvs);          
                foreach (int curInt in curThreadStoreage.triangles)
                {
                    aTriangles.Add(curInt + originalVertexCount);
                }                                                 
            }                       
                    
            return true;
        }

        private Vector3 GetSmallestCoord()
        {
            Vector3 result = Vector3.zero;
            bool isFirst = true;
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (isFirst)
                {
                    isFirst = false;
                    result = curVoxel.position;
                }

                if (curVoxel.position.x < result.x)
                {
                    result.x = curVoxel.position.x;
                }
                if (curVoxel.position.y < result.y)
                {
                    result.y = curVoxel.position.y;
                }
                if (curVoxel.position.z < result.z)
                {
                    result.z = curVoxel.position.z;
                }
            }
            return result;
        }

        private Vector3 GetLargestCoord()
        {
            Vector3 result = Vector3.zero;
            bool isFirst = true;
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                if (isFirst)
                {
                    isFirst = false;
                    result = curVoxel.position;
                }
                if (curVoxel.position.x > result.x)
                {
                    result.x = curVoxel.position.x;
                }
                if (curVoxel.position.y > result.y)
                {
                    result.y = curVoxel.position.y;
                }
                if (curVoxel.position.z > result.z)
                {
                    result.z = curVoxel.position.z;
                }
            }
            return result;
        }

        private VoxelLayer FindVoxelLayerByLayerDepthAndColor(List<VoxelLayer> aLayerList, int aLayerDepth, Color aColor)
        {
            foreach (VoxelLayer curLayer in aLayerList)
            {
                if ((curLayer.layerDepth == aLayerDepth) && (curLayer.voxelColor == aColor))
                {
                    return curLayer;
                }
            }
            return null;
        }

        internal Voxel FindVoxelInLayerByPos(VoxelLayer aLayer, Vector3 aPosition)
        {
            if (aLayer.voxels.ContainsKey(aPosition))
                return aLayer.voxels[aPosition];
            else
                return null;
        }

        internal Vector2 GetMaxStepsInLayer(Voxel aStartVoxel, VoxelLayer aVoxelLayer, Vector3 aDirectionX, Vector3 aDirectionY)
        {
            Vector2 resultVector = Vector2.zero;
            for (int i = 0; i < aVoxelLayer.voxels.Count; i++)
            {
                Vector3 curPos = aStartVoxel.position + (aDirectionX * i);
                if (this.FindVoxelInLayerByPos(aVoxelLayer, curPos) == null)
                {
                    break;
                }
                resultVector.x = i;
            }
            for (int i = 0; i < aVoxelLayer.voxels.Count; i++)
            {
                Vector3 curPos = aStartVoxel.position + (aDirectionY * i);
                if (this.FindVoxelInLayerByPos(aVoxelLayer, curPos) == null)
                {
                    break;
                }
                resultVector.y = i;
            }
            return resultVector;
        }

        private List<Voxel> GetMergableVoxels(VoxelLayer aVoxelLayer, Voxel aStartVoxel, Vector3 aDirectionX, Vector3 aDirectionY, out Vector2 aScaleFactor)
        {
            List<Voxel> result = new List<Voxel>();
            List<Vector2> factorList = new List<Vector2>();
            Vector2 maxSteps = this.GetMaxStepsInLayer(aStartVoxel, aVoxelLayer, aDirectionX, aDirectionY);

            for (int factorX = (int)maxSteps.x; factorX >=0 ; factorX--)
            {
                for (int factorY = (int)maxSteps.y; factorY >=0; factorY--)
                {
                    bool allMergableInArea = true;
                    for (int x = 0; x <= factorX; x++)
                    {
                        for (int y = 0; y <= factorY; y++)
                        {
                            Vector3 curPos = aStartVoxel.position + (aDirectionX * x) + (aDirectionY * y);
                            if (this.FindVoxelInLayerByPos(aVoxelLayer, curPos) == null)
                            {
                                allMergableInArea = false;
                                break;
                            }
                        }
                        if (!allMergableInArea)
                            break;
                    }
                    if (allMergableInArea)
                    {
                        factorList.Add(new Vector2(factorX, factorY));
                        break;
                    }                    
                }
                if (factorList.Count > 0) break;
            }


            int maxMergableVoxels = 0;
            Vector2 resultFactor = Vector2.zero;
            foreach (Vector2 curFactor in factorList)
            {
                if (maxMergableVoxels < this.GetMergableVoxelsFromFactor(curFactor))
                {
                    resultFactor = curFactor;
                    maxMergableVoxels = this.GetMergableVoxelsFromFactor(curFactor);
                }
            }

            result.Add(aStartVoxel);

            for (int x = 0; x <= resultFactor.x; x++)
            {
                for (int y = 0; y <= resultFactor.y; y++)
                {
                    Vector3 curPos = aStartVoxel.position + (aDirectionX * x) + (aDirectionY * y);
                    Voxel newVoxel = this.FindVoxelInLayerByPos(aVoxelLayer, curPos);
                    if ((newVoxel != null) && (result.IndexOf(newVoxel) < 0))
                    {
                        result.Add(newVoxel);
                    }
                }
            }

            resultFactor += Vector2.one;
            aScaleFactor = resultFactor;
            return result;
        }
        private int GetMergableVoxelsFromFactor(Vector2 aVector)
        {
            return (int)((aVector.x + 1) * (aVector.y + 1));
        }

        #endregion
      
        #region SerializationSupport

        [SerializeField]
        private List<Voxel> serializedValues = new List<Voxel>();
        [SerializeField]
        private List<Color> serializedColors = new List<Color>();
        [SerializeField]
        private List<Vector2> serializedVUS = new List<Vector2>();
        [SerializeField]
        private List<VoxelArea> serializedAreas = new List<VoxelArea>();

        private Dictionary<Color, Vector2> uvDictionary = new Dictionary<Color, Vector2>();
        internal Dictionary<Vector3, VoxelArea> voxelAreas = new Dictionary<Vector3, VoxelArea>();

        public void OnBeforeSerialize()
        {
            this.WaitForSerialization();

            serializedAreas.Clear();
            foreach (VoxelArea curArea in this.voxelAreas.Values)
            {                
                serializedAreas.Add(curArea);
            }

            serializedValues.Clear();
            foreach (Voxel curVoxel in this.voxels.Values)
            {
                serializedValues.Add(curVoxel);
            }

            serializedColors.Clear();
            foreach (Color curColor in this.uvDictionary.Keys)
            {
                serializedColors.Add(curColor);
            }

            serializedVUS.Clear();
            foreach (Vector2 curUV in this.uvDictionary.Values)
            {
                serializedVUS.Add(curUV);
            }         
        }

        private bool inDeserialization = false;

        public void WaitForSerialization()
        {
            while (inDeserialization)
            {                
                Thread.Sleep(1);                
            }
        }

        //This field is for backward compatibility
        [SerializeField]
        private bool neighborsSerialized = false;
        
        //System.Object aParam
        private void SerializationThreadCallBack()
        {
            this.inDeserialization = true;
            try
            {
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();                
                this.voxels = null;
                this.voxels = new Dictionary<Vector3, Voxel>();
                for (int i = 0; i != serializedValues.Count; i++)
                {
                    this.AddVoxel(serializedValues[i], false);
                }
                
                this.uvDictionary = null;
                this.uvDictionary = new Dictionary<Color, Vector2>();
                for (int i = 0; i < serializedColors.Count; i++)
                {
                    this.uvDictionary.Add(serializedColors[i], serializedVUS[i]);
                }                              
                
                this.voxelAreas = null;
                this.voxelAreas = new Dictionary<Vector3, VoxelArea>();
                for (int i = 0; i < serializedAreas.Count; i++)
                {
                    this.serializedAreas[i].SetContainer(this);
                    this.serializedAreas[i].ClearVoxelList();
                    this.voxelAreas.Add(this.serializedAreas[i].position, this.serializedAreas[i]);
                }
                

                //sw.Reset();
                //sw.Start();
                //This is going to fill up the areas with the voxels but does not clear the prepared mesh data
                this.GenerateVoxelAreas();
                //sw.Stop();
                //Debug.Log("GenerateVoxelAreas " + sw.ElapsedMilliseconds);

                //sw.Reset();
                //sw.Start();
                if (!neighborsSerialized)
                {
                    this.UpdateAllNeighborList();
                    this.neighborsSerialized = true;
                }
//                sw.Stop();
//                Debug.Log("UpdateAllNeighborList " + sw.ElapsedMilliseconds);
            }
            finally
            {
                this.inDeserialization = false;
            }
        }

        public void OnAfterDeserialize()
        {                       
            this.WaitForSerialization();
            this.inDeserialization = true;
            /*
            bool runTimeSerialization = false;
            if (!Application.isLoadingLevel)
                runTimeSerialization = Application.isPlaying;            

            if (runTimeSerialization)
                ThreadPool.QueueUserWorkItem(this.SerializationThreadCallBack);  
            else*/
            //this.SerializationThreadCallBack(null);

            //this.SerializationThreadCallBack();
            Thread newthread=new Thread(new ThreadStart(this.SerializationThreadCallBack));
            newthread.Start();
        }
        #endregion

        #region ConvertFunctions
        public void ConvertTexture(Texture2D curTexture, Vector2 startPos, Vector2 endPos, Color alphaColor, float alphaTolerance)
        {
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayCancelableProgressBar("Building Voxel Matrix", "building", 0f);
#endif
            //   GameObject prefabCube=null;
            try
            {                
                //calculating stepcount
                Vector2 differenceVector = endPos - startPos;
                int stepCount =(int)(differenceVector.x*differenceVector.y);

                for (int i = (int)startPos.x; i < endPos.x; i++)
                {
                    for (int j = (int)startPos.y; j < endPos.y; j++)
                    {
                        Color curPixelColor = curTexture.GetPixel(i, j);
                        if (curPixelColor.a != 0f)
                        {
                            float colorDistance = Vector4.Distance(curPixelColor, alphaColor);
                            if (colorDistance > (alphaTolerance / 100f))
                            {                                
                                Voxel curVoxel = new Voxel();
                                curVoxel.color = curPixelColor;
                                curVoxel.position = new Vector3(i, j, 0) - new Vector3(startPos.x, startPos.y, 0);
                                this.AddVoxel(curVoxel);
                            }
                        }
                    }
#if UNITY_EDITOR
                    if (EditorUtility.DisplayCancelableProgressBar("Building Voxel Matrix", "Building", (i * curTexture.height) / (float)stepCount))
                    {                        
                        return;
                    }
#endif
                }
                this.BuildMesh(true, true, true, true);
            }
            finally
            {
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
            }
        }
        #endregion

        #region BuildInThread
        internal bool buildingInThreadFlag = false;
        internal VoxelIngameBuildTask ingameBuildTask = null;
        public object mutex=new object(); 
        
        public void AddBuildTaskRemovedVoxel(Voxel aRemovedVoxel)
        {
            if (this.ingameBuildTask == null)
                this.ingameBuildTask = new VoxelIngameBuildTask();

            this.ingameBuildTask.voxelsToRemove.Add(aRemovedVoxel);
        }

        public void AddBuildTaskRemovedVoxels(List<Voxel> aRemovedVoxels)
        {
            if (this.ingameBuildTask == null)
                this.ingameBuildTask = new VoxelIngameBuildTask();

            this.ingameBuildTask.voxelsToRemove.AddRange(aRemovedVoxels);
        }

        private void ThreadcallBack(System.Object threadContext)
        {
            lock (this.voxels)
            {
                try
                {
                    buildingInThreadFlag = true;
                    VoxelIngameBuildTask localTask = this.ingameBuildTask;
                    if (localTask == null) return;
                    this.ingameBuildTask = null;

                    foreach (Voxel curVoxel in localTask.voxelsToRemove)
                    {
                        this.RemoveVoxel(curVoxel);
                    }
                    this.GenerateAreaBuffers();
                }
                finally
                {
                    buildingInThreadFlag = false;
                }
            }
        }

        private void StartBuildMeshInThread()
        {           
            ThreadPool.QueueUserWorkItem(this.ThreadcallBack, this.ingameBuildTask);            
        }
        #endregion
    }
}
