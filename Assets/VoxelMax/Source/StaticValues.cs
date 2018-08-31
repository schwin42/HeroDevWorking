using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelMax
{
    public enum MenuItems {NewStructure, TextureToVoxel=21, SpriteToVoxel=22, ModelToVoxel=23, QubicleImporter = 24, QubicleExporter = 25, MagicaImporter = 26, MagicaExporter = 27, ObjExporter=28, SceneOptimizer = 41,  AboutAndContact=61}
    public sealed class StaticValues
    {
        public const string version = "v1.70219";
        public const string email = "nanoidgames@gmail.com";

        public const string voxelizerRootFolder = "VoxelMax";
        public const string animationPrefabsFolder = "Prefab";
        public const string explodedModels = "ExplodedModels";
        public const string constructedContentFolder = "Constructed";
        public const string colorPaletteFolder = "ColorPalettes";
        public const string materialNamePrefix = "Material_";
        public const string baseObjectNamePrefix = "Voxelized_";        
        public const string meshNamePrefix = "Mesh_";

        public static bool editorBuildFlag = false;

        public const int speedButtonHeight = 32;        

        public const int voxelSpaceSize = 4;
        public const int maxThreadCount = 2;

        public const int maxBrushSize = 15;

        public static readonly Color selectionColor = Color.green;        
        public static readonly Vector3[] sixDirectionArray = new Vector3[6] {Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back};

        private static readonly StaticValues instance = new StaticValues();

        private StaticValues() { }

        public static StaticValues Instance
        {
            get 
            {
                return instance; 
            }
        }

        //Create the neccesary folders 
        public static void CheckAndCreateFolders()
        {
#if UNITY_EDITOR
            if (!System.IO.Directory.Exists("Assets/" + StaticValues.voxelizerRootFolder))
                AssetDatabase.CreateFolder("Assets", StaticValues.voxelizerRootFolder);

            if (!System.IO.Directory.Exists("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.constructedContentFolder))
                AssetDatabase.CreateFolder("Assets/" + StaticValues.voxelizerRootFolder, StaticValues.constructedContentFolder);

            if (!System.IO.Directory.Exists("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.animationPrefabsFolder))
                AssetDatabase.CreateFolder("Assets/" + StaticValues.voxelizerRootFolder, StaticValues.animationPrefabsFolder);

            if (!System.IO.Directory.Exists("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.explodedModels))
                AssetDatabase.CreateFolder("Assets/" + StaticValues.voxelizerRootFolder, StaticValues.explodedModels);
#endif
        }
    }
}

