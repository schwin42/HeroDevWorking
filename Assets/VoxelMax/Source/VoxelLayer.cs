using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelMax
{
    internal class VoxelLayer
    {
        public int layerDepth;
        public Dictionary<Vector3, Voxel> voxels;
        public Color voxelColor;

        public VoxelLayer(int aLayerDepth, Color aVoxelColor)
        {
            this.layerDepth = aLayerDepth;
            this.voxelColor = aVoxelColor;
            this.voxels = new Dictionary<Vector3, Voxel>();            
        }
    }
}
