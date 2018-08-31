using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System;

namespace VoxelMax
{
    public class ConverterTextureToVoxel : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Texture to Voxels", false, (int)MenuItems.TextureToVoxel)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ConverterTextureToVoxel));
        }

        private Texture2D sourceTexture;
        private Color alphaColor = new Color(0f, 0f, 0f, 0f);
        private int alphaTolerance = 0;
        private bool optionalSettings=false;
     
        void OnGUI()
        {
#if UNITY_5_0
#else
				this.titleContent.text = "Texture to Voxel";
#endif

            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Converter settings", EditorStyles.boldLabel);            
            sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);
            if ((sourceTexture != null) && (!this.isTextureResolutionInLimits()))
                EditorGUILayout.HelpBox("We do not recommend the usage of textures with a resolution over 128x128.", MessageType.Info);

            //Optional parameters        
            this.optionalSettings=EditorGUILayout.Foldout(optionalSettings, "Optional Settings");
            if (this.optionalSettings)
            {
                EditorGUI.indentLevel++;
                alphaColor = EditorGUILayout.ColorField("Alpha Color", alphaColor);
                alphaTolerance = EditorGUILayout.IntSlider("Color Tolerance", alphaTolerance, 0, 100, null);
                EditorGUI.indentLevel--;
            }            
            //End of Optional Parameters

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Convert"))
            {
                this.ExtrudeWholeTexture();               
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowWarningText(String aWarningText)
        {
            //Maybe a dialog would be nicer
            Debug.LogWarning(aWarningText);
            return;
        }

        void OnInspectorUpdate()
        {
            this.Repaint();
        }
        
        private static T[] GetAtPath<T>(string path)
        {

            ArrayList al = new ArrayList();
            string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
            foreach (string fileName in fileEntries)
            {
                string localPath = "Assets/" + path;
                localPath = localPath + "/" + Path.GetFileName(fileName);
                UnityEngine.Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

                if (t != null)
                    al.Add(t);
            }
            T[] result = new T[al.Count];
            for (int i = 0; i < al.Count; i++)
                result[i] = (T)al[i];

            return result;
        }

        private Texture2D GetTexture()
        {
            //handling input errors
            if (sourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please set the source texture before convert.", "Ok");
                return null;
            }

            //Reimport the texture if it is not readable        
            Texture2D curTexture = this.sourceTexture;
            try
            {
                curTexture.GetPixel(0, 0);
            }
            catch
            {
                Debug.Log("Reimporting texture, because it is not readable.");
                curTexture = VoxelContainer.ReImportTexture(this.sourceTexture);
            }
            return curTexture;
        }


        private void ExtrudeWholeTexture()
        {
            Texture2D curTexture = this.GetTexture();
            if (curTexture != null)
            {                                
                this.ExtrudePart(curTexture, Vector2.zero, new Vector2(curTexture.width, curTexture.height));
            }
        }

        private void ExtrudePart(Texture2D curTexture, Vector2 startPos, Vector2 endPos)
        {                                          
            //Create simple container object
            GameObject rootObject = new GameObject();
            rootObject.transform.position = new Vector3(0f, 0f, 0f);
            rootObject.name = StaticValues.baseObjectNamePrefix + curTexture.name;
            //Create Voxel container component for the root object
            VoxelContainer voxelContainer = rootObject.AddComponent<VoxelContainer>();
            voxelContainer.ConvertTexture(curTexture, startPos, endPos, this.alphaColor, this.alphaTolerance);
        }

        private bool isTextureResolutionInLimits()
        {
            if ((this.sourceTexture.width > 128) || (this.sourceTexture.height > 128))
                return false;
            return true;
        }


    }
}