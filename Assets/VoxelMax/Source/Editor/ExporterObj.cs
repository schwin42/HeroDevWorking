using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace VoxelMax
{
    public class ExporterObj : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Obj Exporter (.obj)", false, (int)MenuItems.ObjExporter)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ExporterObj));
        }

        private GameObject voxelObject = null;        
        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Obj Exporter";
#endif

            EditorGUILayout.BeginVertical("Box");
            voxelObject = (GameObject)EditorGUILayout.ObjectField("Object to export", voxelObject, typeof(GameObject), true, null);            

            EditorGUILayout.Space();

            if (GUILayout.Button("Export") && (voxelObject != null))
            {
                string fileName = EditorUtility.SaveFilePanel("File to save", "", "", "obj");
                if (fileName != "")
                    this.ExportObjectToQBFile(fileName);
            }
            EditorGUILayout.EndVertical();
        }

        private void ExportObjectToQBFile(string aFilename)
        {
            MeshFilter meshFilter = this.voxelObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogError("VoxelMax Obj Exporter: Object need to have MeshFilter Component");
                return;
            }
            Mesh sharedMesh = meshFilter.sharedMesh;

            StreamWriter sw = new StreamWriter(File.OpenWrite(aFilename));
            try
            {
                sw.WriteLine("#This file is exporter from VoxelMax");
                for (int i = 0; i < sharedMesh.vertexCount; i++)
                {
                    sw.WriteLine("v " + sharedMesh.vertices[i].x + " " + sharedMesh.vertices[i].y + " " + sharedMesh.vertices[i].z);
                }

                for (int i = 0; i < sharedMesh.normals.Length; i++)
                {
                    sw.WriteLine("vn " + sharedMesh.normals[i].x + " " + sharedMesh.normals[i].y + " " + sharedMesh.normals[i].z);
                }

                for (int i = 0; i < sharedMesh.uv.Length; i++)
                {
                    sw.WriteLine("vt " + sharedMesh.uv[i].x + " " + sharedMesh.uv[i].y);
                }


                for (int i = 0; i < sharedMesh.triangles.Length; i += 3)
                {
                    int polygonA = (sharedMesh.triangles[i] + 1);
                    int polygonB = (sharedMesh.triangles[i + 1] + 1);
                    int polygonC = (sharedMesh.triangles[i + 2] + 1);

                    sw.WriteLine("f " + polygonA + "/" + polygonA + "/" + polygonA + " "
                          + polygonB + "/" + polygonB + "/" + polygonB + " "
                          + polygonC + "/" + polygonC + "/" + polygonC);
                }

                //Copy the texture next to it
                Renderer renderer = this.voxelObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (renderer.sharedMaterial != null)
                    {
                        string texturePath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture);
                        File.Copy(texturePath, Path.GetDirectoryName(aFilename) + "/" + Path.GetFileName(texturePath));
                    }
                }

                Debug.Log("VoxelMax: Obj export finished");
            }
            finally
            {
                sw.Flush();
                sw.Close();
            }           
        }       
    }
}