using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace VoxelMax
{   
    public class ConverterSpirteToVoxel : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Sprite to Voxels", false, (int)MenuItems.SpriteToVoxel)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ConverterSpirteToVoxel));
        }

        private List<Sprite> spriteList=new List<Sprite>();
        private Vector2 scrollPosition=Vector2.zero;
        private bool optionalSettings=false;
        private Color alphaColor;
        private int alphaTolerance;

        void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Sprite To Voxel";
#endif
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Converter settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Mathf.Min(200, this.spriteList.Count*50)));
            for (int i = 0; i < this.spriteList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();                                                
                this.spriteList[i] = (Sprite)EditorGUILayout.ObjectField("Frame " + (i + 1), spriteList[i], typeof(Sprite), false, GUILayout.Height(45));
                if (GUILayout.Button("X", GUILayout.Height(45), GUILayout.Width(20)))
                {
                    this.spriteList[i] = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            int curIndex = 0;
            while (curIndex < this.spriteList.Count)
            {
                if (this.spriteList[curIndex] == null)
                {
                    this.spriteList.RemoveAt(curIndex);
                }
                else curIndex++;
            }
            EditorGUILayout.EndScrollView();
            
            Sprite newSprite = null;
            newSprite = (Sprite)EditorGUILayout.ObjectField("New Frame", newSprite, typeof(Sprite), false);
            if (newSprite != null)
                this.spriteList.Add(newSprite);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            this.optionalSettings = EditorGUILayout.Foldout(this.optionalSettings, "Optional Settings");
            if (this.optionalSettings)
            {
                EditorGUI.indentLevel++;
                this.alphaColor = EditorGUILayout.ColorField("Alpha color", this.alphaColor);
                this.alphaTolerance = EditorGUILayout.IntSlider("Color Tolerance", alphaTolerance, 0, 100, null);
                EditorGUI.indentLevel--;
            }                       

            if (GUILayout.Button("Convert"))
            {
                GameObject newGameObject = new GameObject();
                newGameObject.name = this.spriteList[0].texture.name;
                VoxelAnimator animator=newGameObject.AddComponent<VoxelAnimator>();
                

                foreach (Sprite curSprite in this.spriteList)
                {
                    GameObject newObject=this.ConvertSprite(curSprite);
                    animator.voxelContainerPrefabs.Add(newObject);                    
                }               
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.HelpBox("We do not recommend the usage of sprites with a resolution over 128x128.", MessageType.None);
            if (GUILayout.Button("Tutorial"))
            {
                Process myProcess = new Process();
                try
                {
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.FileName = "https://www.youtube.com/watch?v=4wpS0rsmXIg";
                    myProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Could not open webpage: " + e.Message);
                }
            }
            EditorGUILayout.EndVertical();
        }
        private Texture2D GetReadableTexture(Texture2D aTexture)
        {
            //handling input errors
            if (aTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please set the source texture before convert.", "Ok");
                return null;
            }

            //Reimport the texture if it is not readable        
            Texture2D curTexture = aTexture;
            try
            {
                curTexture.GetPixel(0, 0);
            }
            catch
            {
                UnityEngine.Debug.Log("Reimporting texture, because it is not readable.");
                curTexture = VoxelContainer.ReImportTexture(aTexture);
            }
            return curTexture;
        }
   
        private GameObject ConvertSprite(Sprite aSprite)
        {
            Texture2D editableTexture = this.GetReadableTexture(aSprite.texture);          
            
            //Create simple container object
            GameObject rootObject = new GameObject();
            rootObject.transform.position = new Vector3(0f, 0f, 0f);
            rootObject.name = StaticValues.baseObjectNamePrefix + aSprite.name;
            //Create Voxel container component for the root object
            VoxelContainer voxelContainer = rootObject.AddComponent<VoxelContainer>();
            voxelContainer.ConvertTexture(editableTexture, aSprite.textureRect.min, aSprite.textureRect.max, this.alphaColor, this.alphaTolerance);
            voxelContainer.BuildOptimizedMesh(false);

            string assetPath = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.animationPrefabsFolder + "/" + rootObject.name + ".prefab";
            PrefabUtility.CreatePrefab(assetPath, rootObject);            
            DestroyImmediate(rootObject);
#if UNITY_5_0
            return (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
#else
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#endif
        }
    }   
}
