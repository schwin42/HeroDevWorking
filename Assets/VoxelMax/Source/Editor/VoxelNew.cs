using UnityEditor;
using UnityEngine;
using System.Collections;

namespace VoxelMax
{
    public class VoxelNewStructure : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/New Structure", false, (int)MenuItems.NewStructure)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(VoxelNewStructure));
        }

        private Vector3 sizeVector = new Vector3(10f, 10f, 10f);
        void OnGUI()
        {
#if UNITY_5_0
#else
				this.titleContent.text = "New Voxel Structure";
#endif
            EditorGUILayout.BeginVertical("Box");
            sizeVector = (Vector3)EditorGUILayout.Vector3Field("Dimensions for the new Strucutre", sizeVector);

            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                this.NewVoxelStructure();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Hint: You can convert primitives like spheres with the Model to voxel Feature.", MessageType.None);
        }

        public void NewVoxelStructure()
        {
            GameObject newGameObject = new GameObject();
            VoxelContainer newVoxelContainer = newGameObject.AddComponent<VoxelContainer>();
            for (int i = 0; i < this.sizeVector.x; i++)
            {
                for (int j = 0; j < this.sizeVector.y; j++)
                {
                    for (int z = 0; z < this.sizeVector.z; z++)
                    {
                        Voxel newVoxel = new Voxel();
                        newVoxel.position.x = i;
                        newVoxel.position.y = j;
                        newVoxel.position.z = z;
                        newVoxelContainer.AddVoxel(newVoxel);
                    }
                }
            }
            newVoxelContainer.BuildMesh(false, true, true, true);         
          
            if (SceneView.lastActiveSceneView != null)
            {
                UnityEditor.Selection.activeGameObject = newGameObject;
                SceneView.lastActiveSceneView.pivot = newGameObject.transform.position;
                SceneView.lastActiveSceneView.Repaint();                
            }
        }
    }
}