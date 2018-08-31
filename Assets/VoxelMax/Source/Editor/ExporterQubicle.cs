using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace VoxelMax
{
    public class ExporterQubicle : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Qubicle Exporter (.qb)", false, (int)MenuItems.QubicleImporter)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ExporterQubicle));
        }

        private GameObject voxelObject = null;
        private bool exportChildObjects = true;
        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Qubicle Exporter";
#endif

            EditorGUILayout.BeginVertical("Box");           
            voxelObject = (GameObject)EditorGUILayout.ObjectField("Object to export", voxelObject, typeof(GameObject), true, null);
            this.exportChildObjects = EditorGUILayout.Toggle("Export child objects", this.exportChildObjects);

            EditorGUILayout.Space();

            if (GUILayout.Button("Export") && (voxelObject != null))
            {
                string fileName = EditorUtility.SaveFilePanel("File to save", "", "", "qb");
                if (fileName!="")
                    this.ExportObjectToQBFile(fileName);
            }
            EditorGUILayout.EndVertical();
        }

        private void ExportObjectToQBFile(string aFilename)
        {
            List<VoxelContainer> containers = new List<VoxelContainer>();
            VoxelContainer rootContainer = this.voxelObject.GetComponent<VoxelContainer>();
            if (this.exportChildObjects)
            {
                containers.AddRange(this.voxelObject.transform.GetComponentsInChildren<VoxelContainer>());
            }
            else
            {
                if (rootContainer != null)
                {
                    containers.Add(rootContainer);
                }
            }                          
            
            if (containers.Count==0)
            {
                Debug.LogError("QB Export: Did not found voxelContainer in the structure! Please make sure you selected an object with voxelcontainer");
                return;
            }

            if (Path.GetExtension(aFilename).ToUpper() != ".qb".ToUpper())
            {
                Debug.LogError("Extension must be .qb");
                return;
            }

            BinaryWriter bw = new BinaryWriter(File.OpenWrite(aFilename));
            try
            {
                //Header
                uint version = 257;
                bw.Write(version);
                uint colorFormat = 0;//RGBA
                bw.Write(colorFormat);
                uint zAxisOrientation = 0;
                bw.Write(zAxisOrientation);
                uint compression = 0;//Not compressed
                bw.Write(compression);
                uint visibilityMask = 0;//Not encoded
                bw.Write(visibilityMask);
                uint matrixNumber = (uint)containers.Count;
                bw.Write(matrixNumber);


                for (int i = 0; i < matrixNumber; i++)
                {
                    VoxelContainer curContainer = containers[i];
                    //Data
                    string matrixName = curContainer.name;
                    byte nameLength = (byte)matrixName.Length;
                    bw.Write(nameLength);
                    char[] charArrayOfName = matrixName.ToCharArray();
                    bw.Write(charArrayOfName);

                    Vector3 minVector = curContainer.GetMinContainerVector();
                    Vector3 maxVector = curContainer.GetMaxContainerVector();

                    uint sizeX = (uint)(maxVector.x - minVector.x + 1);
                    uint sizeY = (uint)(maxVector.y - minVector.y + 1);
                    uint sizeZ = (uint)(maxVector.z - minVector.z + 1);                                      
                    
                    bw.Write(sizeX);
                    bw.Write(sizeY);
                    bw.Write(sizeZ);

                    //
                    int posx = (int) (curContainer.transform.position.x + minVector.x);
                    int posy = (int) (curContainer.transform.position.y + minVector.y);
                    int posz = (int) (curContainer.transform.position.z + minVector.z);
                                                                                                                      
                    bw.Write(posx);
                    bw.Write(posy);
                    bw.Write(posz);

                    for (int z = (int)minVector.z; z <= maxVector.z; z++)
                    {                         
                        for (int y = (int)minVector.y; y <= maxVector.y; y++)
                        {
                            for (int x = (int)minVector.x; x <= maxVector.x; x++)
                            {                             
                                Vector3 curPosition = new Vector3(x, y, z);
                                if (curContainer.voxels.ContainsKey(curPosition))
                                {
                                    uint voxelColor = this.ColorToUInt(curContainer.voxels[curPosition].color);
                                    bw.Write(voxelColor);
                                }
                                else
                                {
                                    uint noVoxel = 0;
                                    bw.Write(noVoxel);
                                }
                            }
                        }
                    }
                }
                Debug.Log("Export Finished");
            }finally
            {
                bw.Flush();
                bw.Close();
            }
        }

        private void ExportObjectToQBStream(BinaryWriter aBw)
        {

        }
             
        private Color UIntToColor(uint color)
        {
            byte r = (byte)(color & 0x000000FF);
            byte g = (byte)((color & 0x0000FF00) >> 8);
            byte b = (byte)((color & 0x00FF0000) >> 16);
            //byte a = (byte)(color >> 24);                                   

            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
        private uint ColorToUInt(Color color)
        {
            int r = (int)(color.r * 255);
            int g = (int)(color.g * 255);
            int b = (int)(color.b * 255);
            int a = 255;
            return (uint)((r) | (g << 8) |
                          (b << 16) | (a << 24));
        }
       
    }
}