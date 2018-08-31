using UnityEngine;
using UnityEditor;
using System.IO;

namespace VoxelMax
{
    public class ImporterMagica : EditorWindow
    {
        [MenuItem("Tools/VoxelMax/Magica Importer (.vox)", false, (int)MenuItems.MagicaImporter)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImporterMagica));
        }
        
        public void OnGUI()
        {
#if UNITY_5_0
#else
            this.titleContent.text = "Magica Importer";
#endif

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Import .vox file");
            if (GUILayout.Button("Import"))
            {
                string filePath = EditorUtility.OpenFilePanel("Import file", "", "vox");
                if (filePath != "")
                {
                    //System.Diagnostics.Stopwatch sw =new System.Diagnostics.Stopwatch();
                    //sw.Start();
                    this.LoadVoxFile(filePath);
                    //sw.Stop();
                    //Debug.Log("Import time " + sw.ElapsedMilliseconds + "sec.");
                }
            }
            EditorGUILayout.EndVertical();
        }

        // this is the default palette of voxel colors (the RGBA chunk is only included if the palette is differe)
        private static ushort[] voxColors = new ushort[] { 32767, 25599, 19455, 13311, 7167, 1023, 32543, 25375, 19231, 13087, 6943, 799, 32351, 25183,
        19039, 12895, 6751, 607, 32159, 24991, 18847, 12703, 6559, 415, 31967, 24799, 18655, 12511, 6367, 223, 31775, 24607, 18463, 12319, 6175, 31,
        32760, 25592, 19448, 13304, 7160, 1016, 32536, 25368, 19224, 13080, 6936, 792, 32344, 25176, 19032, 12888, 6744, 600, 32152, 24984, 18840,
        12696, 6552, 408, 31960, 24792, 18648, 12504, 6360, 216, 31768, 24600, 18456, 12312, 6168, 24, 32754, 25586, 19442, 13298, 7154, 1010, 32530,
        25362, 19218, 13074, 6930, 786, 32338, 25170, 19026, 12882, 6738, 594, 32146, 24978, 18834, 12690, 6546, 402, 31954, 24786, 18642, 12498, 6354,
        210, 31762, 24594, 18450, 12306, 6162, 18, 32748, 25580, 19436, 13292, 7148, 1004, 32524, 25356, 19212, 13068, 6924, 780, 32332, 25164, 19020,
        12876, 6732, 588, 32140, 24972, 18828, 12684, 6540, 396, 31948, 24780, 18636, 12492, 6348, 204, 31756, 24588, 18444, 12300, 6156, 12, 32742,
        25574, 19430, 13286, 7142, 998, 32518, 25350, 19206, 13062, 6918, 774, 32326, 25158, 19014, 12870, 6726, 582, 32134, 24966, 18822, 12678, 6534,
        390, 31942, 24774, 18630, 12486, 6342, 198, 31750, 24582, 18438, 12294, 6150, 6, 32736, 25568, 19424, 13280, 7136, 992, 32512, 25344, 19200,
        13056, 6912, 768, 32320, 25152, 19008, 12864, 6720, 576, 32128, 24960, 18816, 12672, 6528, 384, 31936, 24768, 18624, 12480, 6336, 192, 31744,
        24576, 18432, 12288, 6144, 28, 26, 22, 20, 16, 14, 10, 8, 4, 2, 896, 832, 704, 640, 512, 448, 320, 256, 128, 64, 28672, 26624, 22528, 20480,
        16384, 14336, 10240, 8192, 4096, 2048, 29596, 27482, 23254, 21140, 16912, 14798, 10570, 8456, 4228, 2114, 1  };

        private struct MagicaVoxelData
        {
            public byte x;
            public byte y;
            public byte z;
            public byte color;

            public MagicaVoxelData(BinaryReader stream)
            {
                x = (byte)stream.ReadByte();
                y = (byte)stream.ReadByte();
                z = (byte)stream.ReadByte();
                color = stream.ReadByte();
            }
        }
        private Color UShortToColor(ushort color)
        {                     
            byte b = (byte)((color & 0x7C00) >> 10);
            byte g = (byte)((color & 0x03E0) >> 5);
            byte r = (byte)((color & 0x001F));
                                           
            return new Color(r / 31f, g / 31f, b / 31f, 1f);
        }

        private void LoadVoxFile(string aFileName)
        {
            //I dissabled compiler warning, because we want to load some values from the file even if we would not use it later.
            //I think it is cleaner this way
            #pragma warning disable
            //Prepare Object
            GameObject newGameObject = new GameObject();
            newGameObject.name = "ImportedFromMagicaVoxel";
            VoxelContainer newVoxelContainer = newGameObject.AddComponent<VoxelContainer>();

            //Open File
            BinaryReader br=new BinaryReader(File.OpenRead(aFileName));            
              
            string magic = new string(br.ReadChars(4));
            int version = br.ReadInt32();

            if (magic == "VOX ")
            {
                int sizex = 0, sizey = 0, sizez = 0;                

                Color[] colors = null;
                MagicaVoxelData[] voxelData = null;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    char[] chunkId = br.ReadChars(4);
                    int chunkSize = br.ReadInt32();
                    int childChunks = br.ReadInt32();
                    string chunkName = new string(chunkId);
                    
                    if (chunkName == "SIZE")
                    {
                        sizex = br.ReadInt32();
                        sizey = br.ReadInt32();
                        sizez = br.ReadInt32();

                        br.ReadBytes(chunkSize - 4 * 3);
                    }
                    else if (chunkName == "XYZI")
                    {                        
                        int numVoxels = br.ReadInt32();                        
                     
                        voxelData = new MagicaVoxelData[numVoxels];
                        for (int i = 0; i < voxelData.Length; i++)
                            voxelData[i] = new MagicaVoxelData(br);
                    }
                    else if (chunkName == "RGBA")
                    {
                        colors = new Color[256];

                        for (int i = 0; i < 256; i++)
                        {
                            byte r = br.ReadByte();
                            byte g = br.ReadByte();
                            byte b = br.ReadByte();
                            byte a = br.ReadByte();

                            colors[i].r = r / 255f;
                            colors[i].g = g / 255f;
                            colors[i].b = b / 255f;
                            colors[i].a = a / 255f;                            
                        }
                    }
                    else br.ReadBytes(chunkSize);   
                }
                if ((voxelData==null) || (voxelData.Length == 0)) return; 
                
                for (int i = 0; i < voxelData.Length; i++)
                {                    
                    Vector3 position    = new Vector3(voxelData[i].x, voxelData[i].z, voxelData[i].y);
                    if (!newVoxelContainer.voxels.ContainsKey(position))
                    {
                        Voxel newVoxel = new Voxel();
                        newVoxel.position = position;
                        newVoxel.color = (colors == null ? UShortToColor(voxColors[voxelData[i].color - 1]) : colors[voxelData[i].color - 1]);
                        newVoxelContainer.AddVoxel(newVoxel, false);
                    }
                }
                newVoxelContainer.UpdateStructure();
                newVoxelContainer.BuildMesh(true, true, true, true);
            } else
            {
                Debug.LogError("Error durring vox import. Probably this is not a .vox file.");
                return;
            }
            #pragma warning restore
        }
    }
}
