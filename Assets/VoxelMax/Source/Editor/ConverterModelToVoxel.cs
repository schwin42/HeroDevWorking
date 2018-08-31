using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace VoxelMax
{
    public class ConverterModelToVoxel : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Model to Voxels", false, (int)MenuItems.ModelToVoxel)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ConverterModelToVoxel));
        }

        private enum ConvertAlgorithm {RayBased, MatrixBased};
        private GameObject objectToConvert = null;
        private float colorTolerance = 0f;
        private bool isRecursive = true;
        private ConvertAlgorithm selectedAlgorithm = ConvertAlgorithm.RayBased;
        private string[] algorithmOptions = new string[] {"Ray based", "Matrix based"};

        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "New Voxel Structure";
#endif
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Converter settings", EditorStyles.boldLabel);
            this.objectToConvert = (GameObject)EditorGUILayout.ObjectField("Object to Convert", this.objectToConvert, typeof(GameObject), true);
            EditorGUILayout.Space();
            selectedAlgorithm    = (ConvertAlgorithm)EditorGUILayout.Popup("Algorithm", (int)selectedAlgorithm, algorithmOptions);
            this.isRecursive     = GUILayout.Toggle(this.isRecursive, "Convert child object");
            this.colorTolerance  = EditorGUILayout.Slider("Color tolerance", this.colorTolerance, 0, 100);                 

            EditorGUILayout.Space();
            if (GUILayout.Button("Convert"))
            {
                if (this.objectToConvert != null)
                {                
                    if (GameObject.Find(this.objectToConvert.name)==null)
                    {
                        EditorUtility.DisplayDialog("Warning", "Please add the object to the scene before converting.", "Ok");
                    }else
                    { 
                        this.ConvertObject(this.objectToConvert);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            if ((this.objectToConvert != null) && (this.objectToConvert.GetComponent<MeshCollider>() != null))
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("Box");
                MeshCollider meshCollider = this.objectToConvert.GetComponent<MeshCollider>();

                EditorGUILayout.HelpBox("This current model with it's current scale will be about "
                    + System.Environment.NewLine
                    + ((int)meshCollider.bounds.size.x) + "X" + ((int)meshCollider.bounds.size.y) + "X" + ((int)meshCollider.bounds.size.z) + " voxel wide."
                    + System.Environment.NewLine
                    + "Please rescale the object if it does not fit your expectations.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.BeginVertical("Box");
            if (GUILayout.Button("Tutorial"))
            {
                Process myProcess = new Process();
                try
                {
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.FileName = "https://www.youtube.com/watch?v=11ckLazjsOY";
                    myProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Could not open webpage: " + e.Message);
                }
            }
            EditorGUILayout.HelpBox("You can scale the orignal model to obtain the required resolution."
                                    + System.Environment.NewLine
                                    + System.Environment.NewLine
                                    + "Also please make sure you have a mesh collider in your source object!"
                                    + System.Environment.NewLine
                                    + "And the object to convert, is have to be part of the scene!", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private GameObject ConvertObject(GameObject aObject)
        {
            if (aObject == null) return null;

            GameObject newVoxelObject = new GameObject();
            newVoxelObject.name = aObject.name + "_Converted";
            
            if (this.isRecursive) 
            { 
                //I do this because the child list always change because
                //I have to removed the child while I convert it since 
                //the mesh collider does not look to be workin as a child for me.
                //Reach out for me if you have better idea :)
                List<Transform> childList=new List<Transform>();
                for (int i = 0; i < aObject.transform.childCount; i++)
                {
                    childList.Add(aObject.transform.GetChild(i));
                }

                for (int i = 0; i < childList.Count; i++)
                {
                    GameObject newChild = this.ConvertObject(childList[i].gameObject);
                    if (newChild != null)
                    {
                        newChild.transform.parent = newVoxelObject.transform;
                        newChild.transform.localPosition = childList[i].localPosition;
                    }
                }          
            }

            MeshCollider meshcollider = aObject.GetComponent<MeshCollider>();
            if (meshcollider == null)
            {
                UnityEngine.Debug.LogError("VoxelMax: Source Object need meshcollider to be converted!");
                return newVoxelObject;
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayCancelableProgressBar("Building Voxel Matrix", "Building", 0f);

            //Clear colorpalette
            this.colorPalette = null;
            this.colorPalette = new List<Color>();

            
            VoxelContainer voxelContainer = newVoxelObject.AddComponent<VoxelContainer>();
            try
            {
                //Get Texture
                Texture2D objectTexture = null;
                Renderer objRenderer = aObject.GetComponent<Renderer>();
                if (objRenderer != null)
                {
                    if (objRenderer.sharedMaterial != null)
                    {
                        objectTexture = (Texture2D)objRenderer.sharedMaterial.mainTexture;
                        if (objectTexture != null)
                        {
                            try
                            {
                                objectTexture.GetPixel(0, 0);
                            }
                            catch
                            {
                                UnityEngine.Debug.Log("Reimporting texture, because it is not readable.");
                                objectTexture = VoxelContainer.ReImportTexture(objectTexture);
                            }
                        }
                    }
                }
                Transform originalParent = aObject.transform.parent;
                aObject.transform.parent = null;
                float maxStepCount = (meshcollider.bounds.max.x - meshcollider.bounds.min.x + 2) *
                    (meshcollider.bounds.max.y - meshcollider.bounds.min.y + 2) *
                    (meshcollider.bounds.max.z - meshcollider.bounds.min.z + 2);

                float eachStep = (meshcollider.bounds.max.y - meshcollider.bounds.min.y + 2) *
                    (meshcollider.bounds.max.z - meshcollider.bounds.min.z + 2);

                
                for (float x = (int)(meshcollider.bounds.min.x - 1f); x <= meshcollider.bounds.max.x + 1f; x += 1f)
                {
                    for (float y = (int)(meshcollider.bounds.min.y - 1f); y <= meshcollider.bounds.max.y + 1f; y += 1f)
                    {
                        for (float z = (int)(meshcollider.bounds.min.z - 1f); z <= meshcollider.bounds.max.z + 1f; z += 1f)                        
                        {
                            Vector3 shootOrigin = new Vector3(x, y, z);
                            if (this.selectedAlgorithm == ConvertAlgorithm.RayBased)
                            {
                                for (int k = 0; k < 6; k++)
                                    this.ShootArray(shootOrigin, StaticValues.sixDirectionArray[k], voxelContainer, meshcollider, objectTexture);
                            }
                            else
                            {
                                this.CheckVoxel(shootOrigin, voxelContainer, meshcollider, objectTexture);
                            }
                        }
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("Building Voxel Matrix", "Building", (eachStep * x) / maxStepCount))
                    {
                        EditorUtility.ClearProgressBar();
                        return null;
                    }
                }
                aObject.transform.parent = originalParent;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

      //      if (this.selectedAlgorithm == ConvertAlgorithm.RayBased)
       //         voxelContainer.FillUpObject();
            voxelContainer.BuildMesh(true, true, true, true);

            if (SceneView.lastActiveSceneView != null)
            {
                UnityEditor.Selection.activeGameObject = newVoxelObject;
                SceneView.lastActiveSceneView.pivot = newVoxelObject.transform.position;
                SceneView.lastActiveSceneView.Repaint();
            }
            return newVoxelObject;
        }

        private Voxel AddVoxelAtPosition(Vector3 aPosition, VoxelContainer aVoxelContainer, MeshCollider aMeshCollider)
        {
            if (aVoxelContainer.voxels.ContainsKey(aPosition)) return aVoxelContainer.voxels[aPosition];

            Voxel newVoxel = new Voxel();
            newVoxel.position = aPosition;
            newVoxel.SetContainer(aVoxelContainer);
            aVoxelContainer.AddVoxel(newVoxel);
            return newVoxel;
        }

        private List<Color> colorPalette;
        private Color GetColorByTolerance(Color aColor)
        {
            if (colorTolerance == 0) return aColor;

            foreach (Color curColor in this.colorPalette)
            {
                if (((Vector4.Distance(curColor, aColor) / Vector4.Distance(Color.white, Color.black)) * 100f) < this.colorTolerance)
                {
                    return curColor;
                }
            }
            this.colorPalette.Add(aColor);
            return aColor;
        }
        private bool CheckIfInnerPoint(Vector3 aPosition, MeshCollider aCollider, Vector3 aDirection)
        {
            int intersectCount = 0;
            Vector3 distanceVector = aCollider.bounds.size;
            distanceVector.x = distanceVector.x + aDirection.x;
            distanceVector.y = distanceVector.y + aDirection.y;
            distanceVector.z = distanceVector.z + aDirection.z;
            float maxDistance = 10000f;

            Vector3 direction = aDirection;
            Vector3 curpos = aPosition;
            Vector3 targetPoint = curpos + (direction * maxDistance);
            while (targetPoint != curpos)
            {
                if (intersectCount > 1000) return false;
                RaycastHit hit;
                if (Physics.Linecast(curpos, targetPoint, out hit))
                {
                    if (hit.collider == aCollider)
                    {
                        intersectCount++;
                    }
                    curpos = hit.point + (direction / 100f);                    
                }
                else
                {
                    curpos = targetPoint;
                }
            }

            while (curpos != aPosition)
            {
                if (intersectCount > 1000) return false;
                RaycastHit hit;
                if (Physics.Linecast(curpos, aPosition, out hit))
                {
                    if (hit.collider == aCollider)
                    {
                        intersectCount++;
                    }
                    curpos = hit.point - (direction / 100f);                    
                }
                else
                {
                    curpos = aPosition;
                }
            }
            if (((intersectCount % 2) == 1))
                return true;
            else
                return false;                
        }
        private void CheckVoxel(Vector3 aPosition, VoxelContainer aContainer, MeshCollider aCollider, Texture2D aObjectTexture)
        {
            //Physics.SphereCastAll(aPosition, 0.05f, adirection, Mathf.Infinity);
            for (int i = 0; i < 6; i++)
            {
                if (!this.CheckIfInnerPoint(aPosition, aCollider, StaticValues.sixDirectionArray[i])) return;                    
            }
            //Coord is inside
             
            Vector3 voxelPosition = this.RoundVector(aPosition);
            
            Color curColor = Color.white;
            float curDistance = -1;
            for (int i = 0; i < 6; i++)
            {
                Vector3 curDirection = StaticValues.sixDirectionArray[i];
                RaycastHit[] hits = Physics.RaycastAll(aPosition + (curDirection * 10000f), -curDirection, Mathf.Infinity);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == aCollider)
                    {
                        if ((curDistance == -1) || (curDistance > Vector3.Distance(voxelPosition, hit.point)))
                        {
                            curDistance = Vector3.Distance(voxelPosition, hit.point);
                            if (aObjectTexture != null)
                            {
                                curColor = this.GetColorByTolerance(aObjectTexture.GetPixel((int)(hit.textureCoord.x * aObjectTexture.width), (int)(hit.textureCoord.y * aObjectTexture.height)));
                            }
                        }
                    }
                }
            }                
            if (!aContainer.voxels.ContainsKey(voxelPosition))
            {
                voxelPosition = voxelPosition - aCollider.transform.position;
                voxelPosition = this.RoundVector(voxelPosition);
                Voxel newVoxel = this.AddVoxelAtPosition(voxelPosition, aContainer, aCollider);
                if (aObjectTexture != null)
                {                        
                    newVoxel.color = curColor;
                }
            }            
        }

        private Vector3 RoundVector(Vector3 aVector)
        {
            Vector3 result = new Vector3();
            result.x = (int)Mathf.RoundToInt(aVector.x);
            result.y = (int)Mathf.RoundToInt(aVector.y);
            result.z = (int)Mathf.RoundToInt(aVector.z);
            return result;
        }
        private void ShootArray(Vector3 aPosition, Vector3 adirection, VoxelContainer aContainer, MeshCollider aCollider, Texture2D aObjectTexture)
        {
            //Physics.SphereCastAll(aPosition, 0.05f, adirection, Mathf.Infinity);
            RaycastHit[] hits = Physics.RaycastAll(aPosition, adirection, Mathf.Infinity);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == aCollider)
                {
                    Vector3 newPosition = hit.point - aCollider.gameObject.transform.position;
                    newPosition.x = (int)Mathf.RoundToInt(newPosition.x);
                    newPosition.y = (int)Mathf.RoundToInt(newPosition.y);
                    newPosition.z = (int)Mathf.RoundToInt(newPosition.z);

                    Voxel newVoxel;
                    if (!aContainer.voxels.ContainsKey(newPosition))
                    {
                        newVoxel = this.AddVoxelAtPosition(newPosition, aContainer, aCollider);
                        if (aObjectTexture != null)
                        {
                            Color palettedColor = this.GetColorByTolerance(aObjectTexture.GetPixel((int)(hit.textureCoord.x * aObjectTexture.width), (int)(hit.textureCoord.y * aObjectTexture.height)));
                            newVoxel.color = palettedColor;
                        }
                    }
                }
            }

        }
    }
}
