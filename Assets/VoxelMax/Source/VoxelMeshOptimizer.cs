using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelMax
{
    internal class ParamForOptimizerThread
    {
        public Texture2D voxelTexture;
        public int textureWidth, textureHeight;
    }

    internal class VoxelMeshOptimizer
    {
        VoxelLayer layer;
        Texture2D texture;
        public List<Vector3> vertices;
        public List<Vector2> uvs;
        public List<int> triangles;
        Vector3 vectorX;
        Vector3 vectorY;
        Vector3 offset;
        VoxelContainer ownerContainer;
        int optmalisationMinMergableVoxels;
        int textureWidth;
        int textureHeight;

        public void Init(VoxelLayer aLayer, ParamForOptimizerThread aTextureParam, Vector3 aVectorX, Vector3 aVectorY, Vector3 aOffset, int aOptmalisationMinMergableVoxels, VoxelContainer aContainer)
        {
            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            triangles = new List<int>();

            this.layer = aLayer;
            this.texture = aTextureParam.voxelTexture;
            this.vectorX = aVectorX;
            this.vectorY = aVectorY;
            this.offset = aOffset;
            this.optmalisationMinMergableVoxels = aOptmalisationMinMergableVoxels;
            this.ownerContainer = aContainer;
            this.textureWidth = aTextureParam.textureWidth;
            this.textureHeight = aTextureParam.textureHeight;
      //      this.ownerContainer.PrepareTextureAndUVDictionary(true);
        }

        public void Start()
        {
            while (layer.voxels.Count > 0)
            {
                int maxRemovableCount = 0;
                Vector2 scaleFactor;
                List<Voxel> maxMergableVoxels = null;
                Vector2 maxScaleFactor = Vector2.zero;
                Voxel maxVoxel = null;

                foreach (Voxel curVoxel in this.layer.voxels.Values)
                {
                    List<Voxel> curMergableVoxels = this.GetMergableVoxels(layer, curVoxel, vectorX, vectorY, out scaleFactor);
                    if (curMergableVoxels.Count > maxRemovableCount)
                    {
                        maxVoxel = curVoxel;
                        maxRemovableCount = curMergableVoxels.Count;
                        maxMergableVoxels = curMergableVoxels;
                        maxScaleFactor = scaleFactor;
#if UNITY_EDITOR
                        //                            if (EditorUtility.DisplayCancelableProgressBar("Optimization", aProgressCaption + " " + progressPercentage + "%", ((float)aLayers.IndexOf(curLayer)) / ((float)aLayers.Count)))
                        //return false;
#endif
                        if (((maxRemovableCount > this.optmalisationMinMergableVoxels) && (optmalisationMinMergableVoxels != 0)) || (maxRemovableCount == layer.voxels.Count)) break;
                    }
                }
                if (maxVoxel == null) break;

                List<Voxel> mergableVoxels = maxMergableVoxels;

                ownerContainer.BuildPolygonsForFace(texture, mergableVoxels[0].color, vertices, triangles, uvs, maxVoxel.position + offset, vectorX * maxScaleFactor.x, vectorY * maxScaleFactor.y, textureWidth, textureHeight);
                foreach (Voxel curVoxel in mergableVoxels)
                {
                    layer.voxels.Remove(curVoxel.position);
                }
            }
        }

        private List<Voxel> GetMergableVoxels(VoxelLayer aVoxelLayer, Voxel aStartVoxel, Vector3 aDirectionX, Vector3 aDirectionY, out Vector2 aScaleFactor)
        {
            List<Voxel> result = new List<Voxel>();
            List<Vector2> factorList = new List<Vector2>();
            Vector2 maxSteps = ownerContainer.GetMaxStepsInLayer(aStartVoxel, aVoxelLayer, aDirectionX, aDirectionY);

            for (int factorX = (int)maxSteps.x; factorX >= 0; factorX--)
            {
                for (int factorY = (int)maxSteps.y; factorY >= 0; factorY--)
                {
                    bool allMergableInArea = true;
                    for (int x = 0; x <= factorX; x++)
                    {
                        for (int y = 0; y <= factorY; y++)
                        {
                            Vector3 curPos = aStartVoxel.position + (aDirectionX * x) + (aDirectionY * y);
                            if (ownerContainer.FindVoxelInLayerByPos(aVoxelLayer, curPos) == null)
                            {
                                allMergableInArea = false;
                                break;
                            }
                        }
                        if (!allMergableInArea)
                            break;
                    }
                    if (allMergableInArea)
                    {
                        factorList.Add(new Vector2(factorX, factorY));
                        break;
                    }
                }
                if (factorList.Count > 0) break;
            }


            int maxMergableVoxels = 0;
            Vector2 resultFactor = Vector2.zero;
            foreach (Vector2 curFactor in factorList)
            {
                if (maxMergableVoxels < this.GetMergableVoxelsFromFactor(curFactor))
                {
                    resultFactor = curFactor;
                    maxMergableVoxels = this.GetMergableVoxelsFromFactor(curFactor);
                }
            }

            result.Add(aStartVoxel);

            for (int x = 0; x <= resultFactor.x; x++)
            {
                for (int y = 0; y <= resultFactor.y; y++)
                {
                    Vector3 curPos = aStartVoxel.position + (aDirectionX * x) + (aDirectionY * y);
                    Voxel newVoxel = ownerContainer.FindVoxelInLayerByPos(aVoxelLayer, curPos);
                    if ((newVoxel != null) && (result.IndexOf(newVoxel) < 0))
                    {
                        result.Add(newVoxel);
                    }
                }
            }

            resultFactor += Vector2.one;
            aScaleFactor = resultFactor;
            return result;
        }
        private int GetMergableVoxelsFromFactor(Vector2 aVector)
        {
            return (int)((aVector.x + 1) * (aVector.y + 1));
        }
    }
}