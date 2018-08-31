using UnityEngine;
using UnityEditor;
using System.IO;
namespace VoxelMax
{
    public class ImporterQubicle : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Qubicle Importer (.qb)", false, (int)MenuItems.QubicleImporter)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImporterQubicle));
        }
        
        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Qubicle Importer";
#endif

            EditorGUILayout.BeginVertical("Box");            
            EditorGUILayout.LabelField("Import .qb file");
            if (GUILayout.Button("Import"))
            {
                string filePath=EditorUtility.OpenFilePanel("Import .qb file", "", "qb");
                if (filePath!="")
                    this.ImportAssetOptionsQEObject(filePath);
            }
            EditorGUILayout.EndVertical();
        }

        private void ImportAssetOptionsQEObject(string aFilename)
        {
            if (Path.GetExtension(aFilename).ToUpper()!=".qb".ToUpper())
            {
                Debug.LogError("Extension must be .qb");
                return;
            }

            //Consts
            uint CODEFLAG = 2;
            uint NEXTSLICEFLAG = 6;

            //Parent Gameobject
            GameObject parentGameObject = new GameObject();
            parentGameObject.name = Path.GetFileNameWithoutExtension(aFilename);

            BinaryReader br = new BinaryReader(File.Open(aFilename, FileMode.Open));
            try
            {                
                if (br == null)
                {
                    Debug.Log("VoxelMax: Could not open file. Filename: " + aFilename);
                    return;
                }
                uint fileVersion = br.ReadUInt32();
                uint colorFormat = br.ReadUInt32();
                uint zAxisOrientation = br.ReadUInt32();
                uint compressed = br.ReadUInt32();
                uint visibilityMaskEncoded = br.ReadUInt32();
                uint numMatrices = br.ReadUInt32();
                Debug.Log("Qubicle File Import Info\r\n"+
                "File Version: " + fileVersion + "\r\n"+
                "Color Format: " + colorFormat + "\r\n" +
                "zAxisOrientation: " + zAxisOrientation + "\r\n" +
                "visibilityMaskEncoded: " + visibilityMaskEncoded + "\r\n" +
                "Object Count: " + numMatrices);                

                for (uint i = 0; i < numMatrices; i++)
                {
                    // read matrix name
                    byte nameLength = br.ReadByte();
                    string matrixName = new string(br.ReadChars(nameLength));

                    GameObject newGameObject = new GameObject();
                    newGameObject.transform.parent = parentGameObject.transform;
                    newGameObject.name = matrixName;
                    VoxelContainer newVoxelContainer  = newGameObject.AddComponent<VoxelContainer>();

                    // read matrix size 
                    uint sizeX = br.ReadUInt32();
                    uint sizeY = br.ReadUInt32();
                    uint sizeZ = br.ReadUInt32();

                    // read matrix position (in this example the position is irrelevant)
                    int posX = br.ReadInt32();
                    int posY = br.ReadInt32();
                    int posZ = br.ReadInt32();

                    newGameObject.transform.position = new Vector3(posX, posY, posZ);
                    if (compressed == 0) // if uncompressd
                    {
                        for (uint z = 0; z < sizeZ; z++)
                            for (uint y = 0; y < sizeY; y++)
                                for (uint x = 0; x < sizeX; x++)
                                {
                                    uint curColor = br.ReadUInt32();
                                    if (curColor != 0)
                                    {
                                        Voxel newVoxel=new Voxel();
                                        newVoxel.position=new Vector3(x, y, z);
                                        newVoxel.color=this.UIntToColor(curColor);
                                        newVoxelContainer.AddVoxel(newVoxel, false);                                        
                                    }                                    
                                }
                    }
                    else // if compressed
                    {
                        uint z = 0;
     
                        while (z < sizeZ) 
                        {
                            z++;
                            uint index = 0;
       
                            while (true) 
                            {
                                uint data = br.ReadUInt32();
         
                                if (data == NEXTSLICEFLAG)
                                    break;
                                else if (data == CODEFLAG) 
                                {
                                    uint count = br.ReadUInt32();
                                    data = br.ReadUInt32();
           
                                    for(uint j = 0; j < count; j++) 
                                    {
                                        
                                        uint x = index % sizeX + 1; // mod = modulo e.g. 12 mod 8 = 4
                                        uint y = index / sizeX + 1; // div = integer division e.g. 12 div 8 = 1
                                        index++;

                                        if (data != 0)
                                        {
                                            Voxel newVoxel = new Voxel();
                                            newVoxel.position = new Vector3(x, y, z);
                                            newVoxel.color = this.UIntToColor(data);
                                            newVoxelContainer.AddVoxel(newVoxel, false);
                                        }
                                    }
                                }
                                else 
                                {
                                    uint x = index % sizeX + 1;
                                    uint y = index / sizeX + 1;
                                    index++;

                                    if (data != 0)
                                    {
                                        Voxel newVoxel = new Voxel();
                                        newVoxel.position = new Vector3(x, y, z);
                                        newVoxel.color = this.UIntToColor(data);
                                        newVoxelContainer.AddVoxel(newVoxel, false);
                                    }
                                }
                            }
                        }
                    }

                    newVoxelContainer.UpdateStructure();                    
                    newVoxelContainer.BuildMesh(true, true, true, true);                   
                }
            }
            finally
            {
                br.Close();
            }
        }
        private Color UIntToColor(uint color)
        {
            byte r = (byte)(color & 0x000000FF);
            byte g = (byte)((color & 0x0000FF00) >> 8);
            byte b = (byte)((color & 0x00FF0000) >> 16);
            //byte a = (byte)(color >> 24);                                   

            return new Color(r/255f, g/255f, b/255f, 1f);
        }
    }
}