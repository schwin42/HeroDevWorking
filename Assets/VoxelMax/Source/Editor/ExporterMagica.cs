using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace VoxelMax
{

    public class ExporterMagica : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Magica Exporter (.vox)", false, (int)MenuItems.MagicaExporter)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ExporterMagica));
        }

        private GameObject voxelObject  = null;
        private bool exportChildObjects = true;

        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Magica Exporter";
#endif

            EditorGUILayout.BeginVertical("Box");
            this.voxelObject = (GameObject)EditorGUILayout.ObjectField("Object to export", voxelObject, typeof(GameObject), true, null);
            this.exportChildObjects = EditorGUILayout.Toggle("Export child objects", this.exportChildObjects);
            EditorGUILayout.Space();

            if (GUILayout.Button("Export") && (voxelObject != null))
            {
                string fileName = EditorUtility.SaveFilePanel("File to save", "", "", "vox");
                if (fileName != "")
                    this.ExportObjectToVoxFile(fileName);
            }
            EditorGUILayout.EndVertical();
        }

        private void ExportObjectToVoxFile(string aFilename)
        {
            GameObject tempGameObject = new GameObject();
            VoxelContainer voxelContainer = tempGameObject.AddComponent<VoxelContainer>();
            try
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

                foreach (VoxelContainer curContainer in containers)
                {
                    foreach (Voxel curVoxel in curContainer.voxels.Values)
                    {
                        Voxel newVoxel = new Voxel();
                        newVoxel.color = curVoxel.color;
                        //Transform the positions into the same matrix
                        newVoxel.position = this.voxelObject.transform.InverseTransformPoint(curContainer.gameObject.transform.TransformPoint(curVoxel.position));
                        voxelContainer.AddVoxel(newVoxel, false);
                    }
                }


                if (voxelContainer.voxels.Count == 0)
                {
                    Debug.LogError("Vox Export: Did not found voxels or voxelContainer in the structure! Please make sure you selected an object with voxelcontainer");
                    return;
                }

                if (Path.GetExtension(aFilename).ToUpper() != ".vox".ToUpper())
                {
                    Debug.LogError("Extension must be .vox");
                    return;
                }

                BinaryWriter bw = new BinaryWriter(File.OpenWrite(aFilename));
                try
                {
                    char[] magicalString = "VOX ".ToCharArray();
                    bw.Write(magicalString);
                    int fileVersion = 150;
                    bw.Write(fileVersion);

                    //Main chunk
                    //Chunk header
                    char[] mainChunk = "MAIN".ToCharArray();
                    bw.Write(mainChunk);
                    int chunkSize = 0;//3*4bytes
                    bw.Write(chunkSize);
                    int childChunks = (3 * 12) + 12 + (voxelContainer.voxels.Count * 4) + 4 + (256 * 4);//3 chunk header + size values + voxels values + voxelnumbers + color palette
                    bw.Write(childChunks);

                    //Size chunk
                    //Chunk header
                    char[] sizeChunk = "SIZE".ToCharArray();
                    bw.Write(sizeChunk);
                    chunkSize = 3 * 4;//3*4bytes
                    bw.Write(chunkSize);
                    childChunks = 0;
                    bw.Write(childChunks);

                    //Chunk data
                    Vector3 sizeVector = voxelContainer.GetMaxContainerVector() - voxelContainer.GetMinContainerVector();
                    int size = (int)sizeVector.x;
                    bw.Write(size);//x
                    size = (int)sizeVector.z;
                    bw.Write(size);//y
                    size = (int)sizeVector.y;
                    bw.Write(size);//z                

                    //XYZI chunk
                    //Chunk header
                    char[] XYZIChunk = "XYZI".ToCharArray();
                    bw.Write(XYZIChunk);
                    chunkSize = 4 * voxelContainer.voxels.Count + 4;//the Voxel's preferences are stored on 4 bytes
                    bw.Write(chunkSize);
                    childChunks = 0;
                    bw.Write(childChunks);

                    List<Color> colorPalette = voxelContainer.GetColorPalette();
                    if (colorPalette.Count > 255)
                    {
                        Debug.LogError("Export Failed: MagicaVoxel does not support more then 255 colors.");
                        return;
                    }

                    //Prepare Transform Vector
                    //We need it because, it looks like MagicaVoxel drops everything under 0
                    Vector3 transFormVector = Vector3.zero;
                    Vector3 minVector = voxelContainer.GetMinContainerVector();
                    if (minVector.x < 0)
                        transFormVector.x = minVector.x * -1f;
                    if (minVector.y < 0)
                        transFormVector.y = minVector.y * -1f;
                    if (minVector.z < 0)
                        transFormVector.z = minVector.z * -1f;

                    //Chunk Data
                    int voxelNumber = voxelContainer.voxels.Count;
                    bw.Write(voxelNumber);
                    foreach (Voxel curVoxel in voxelContainer.voxels.Values)
                    {
                        Vector3 voxelPos = curVoxel.position + transFormVector;
                        byte coord = (byte)voxelPos.x;
                        bw.Write(coord);
                        coord = (byte)voxelPos.z;
                        bw.Write(coord);
                        coord = (byte)voxelPos.y;
                        bw.Write(coord);
                        byte colorIndex = (byte)(colorPalette.IndexOf(curVoxel.color) + 1);
                        bw.Write(colorIndex);
                    }

                    //RGBA chunk
                    //Chunk header
                    char[] RGBAChunk = "RGBA".ToCharArray();
                    bw.Write(RGBAChunk);
                    chunkSize = 4 * 256;//Colors are stored in bytes
                    bw.Write(chunkSize);
                    childChunks = 0;
                    bw.Write(childChunks);

                    //Color data
                    for (int j = 0; j < colorPalette.Count; j++)
                    {
                        byte colorR = (byte)(colorPalette[j].r * 255);
                        bw.Write(colorR);
                        byte colorG = (byte)(colorPalette[j].g * 255);
                        bw.Write(colorG);
                        byte colorB = (byte)(colorPalette[j].b * 255);
                        bw.Write(colorB);
                        byte colorA = (byte)(colorPalette[j].a * 255);
                        bw.Write(colorA);
                    }
                    if (colorPalette.Count < 256)
                    {
                        //Fill update the color palette to 255
                        for (int j = colorPalette.Count; j < 256; j++)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                byte color = 255;
                                bw.Write(color);
                            }
                        }
                    }
                }
                finally
                {
                    bw.Flush();
                    bw.Close();
                }
            }
            finally
            {
                DestroyImmediate(tempGameObject);
            }
        }

    }
}
