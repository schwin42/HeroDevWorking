using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace VoxelMax
{
    public class SceneOptimizer : EditorWindow
    {
        List<VoxelContainer> voxelContainerList=null;
        List<bool> selectionList=null;
 
        [MenuItem("Tools/VoxelMax/Optimize Scene", false, (int)MenuItems.SceneOptimizer)]
        public static void ShowWindow()
        {            
            EditorWindow.GetWindow(typeof(SceneOptimizer));                                              
        }

        private void PrepareContainerList()
        {
            voxelContainerList = null;
            voxelContainerList = new List<VoxelContainer>();

            voxelContainerList.AddRange(GameObject.FindObjectsOfType<VoxelContainer>());

            selectionList = new List<bool>(voxelContainerList.Count);
            for (int i = 0; i < voxelContainerList.Count; i++)
            {
                selectionList.Add(true);
            }
        }

        Vector2 scrollPos = Vector2.zero;
        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Scene Optimizer";
#endif            

            if (this.voxelContainerList == null)
                this.PrepareContainerList();
            else
                if (((this.voxelContainerList.Count > 0) && (this.voxelContainerList[0] == null)) || (this.voxelContainerList.Count == 0))
                    this.PrepareContainerList();


            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("List of Voxel models:", EditorStyles.boldLabel);
            scrollPos=EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i=0; i<this.voxelContainerList.Count; i++)
            {
                selectionList[i]=EditorGUILayout.Toggle(voxelContainerList[i].name, selectionList[i]);
            }
            EditorGUILayout.EndScrollView();            
            if (GUILayout.Button("Refresh List"))
            {
                this.PrepareContainerList();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                this.selectAll();
            }
            if (GUILayout.Button("Deselect All"))
            {
                this.deSelectAll();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Optimize Selected Items")) 
            {
                this.OptimizeList();
            }
            EditorGUILayout.EndVertical();
        }


        private void OptimizeList()
        {
            int selectedCount = 0;
            for (int i = 0; i < this.voxelContainerList.Count; i++)
            {
                if (this.selectionList[i])
                {
                    selectedCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            int optimizedCount = 0;
            Debug.Log("VoxelMax: The optimization started.");
            try
            {
                for (int i = 0; i < this.voxelContainerList.Count; i++)
                {
                    if (this.selectionList[i])
                    {
                        try
                        {                            
                            if (EditorUtility.DisplayCancelableProgressBar("Optimizing scene", i + "/" + this.voxelContainerList.Count, (float)i / (float)this.voxelContainerList.Count))
                                return;

                            if (this.voxelContainerList[i] == null) continue;


                            this.voxelContainerList[i].BuildMesh(false, true, true, true);
                            this.voxelContainerList[i].BuildOptimizedMesh(false);                            
                        }
                        catch (Exception e) {
                            Debug.LogError("Error durring optimization of " + voxelContainerList[i].name + ". " + e.Message);                            
                        }
                        optimizedCount++;
                        Debug.Log("Optmized " + optimizedCount + "/" + selectedCount);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        private void selectAll()
        {
            for (int i = 0; i < voxelContainerList.Count; i++)
            {
                selectionList[i]=true;
            } 
        }
        private void deSelectAll()
        {
            for (int i = 0; i < voxelContainerList.Count; i++)
            {
                selectionList[i] = false;
            }
        }
    }
}