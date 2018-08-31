using UnityEditor;
using UnityEngine;
using System;
using System.Diagnostics;


namespace VoxelMax{
    public class VoxelMaxAbout : EditorWindow {
        [MenuItem("Tools/VoxelMax/About and Contact", false, (int)MenuItems.AboutAndContact)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(VoxelMaxAbout));
        }
        public void OnGUI(){
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.HelpBox("Thank you for buying my product."+System.Environment.NewLine+
                                    "Current version is "+StaticValues.version+System.Environment.NewLine+
                                    "You can contact me by the following email adress: "+System.Environment.NewLine+
                                    StaticValues.email, MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Facebook")){
                Process myProcess = new Process();                
                try
                {
                    myProcess.StartInfo.UseShellExecute = true; 
                    myProcess.StartInfo.FileName = "https://www.facebook.com/NanoidGames";
                    myProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Could not open webpage: "+e.Message);
                }
            }
            if (GUILayout.Button("Twitter")){
                Process myProcess = new Process();                
                try
                {
                    myProcess.StartInfo.UseShellExecute = true; 
                    myProcess.StartInfo.FileName = "https://twitter.com/NanoidGames";
                    myProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Could not open webpage: "+e.Message);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
