using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace VoxelMax
{
    [System.Serializable]
    internal class VoxelArea
    {
        [System.NonSerialized]
        private VoxelContainer container;
        [System.NonSerialized]
        private List<Voxel> voxelList = new List<Voxel>();

        public Vector3 position;
        public List<Vector3> vertices;
        public List<Vector2> uvs;
        public List<int> triangles;

        public bool isChanged = true;

        public VoxelArea(Vector3 aPosition, VoxelContainer aContainer)
        {
            this.position = aPosition;
            this.container = aContainer;
        }

        [System.NonSerialized]
        private Texture2D texture;
        [System.NonSerialized]
        private int textureWidth, textureHeight;

        public void SetTexture(Texture2D aTexture)
        {
            this.texture = aTexture;
            this.textureWidth = aTexture.width;
            this.textureHeight = aTexture.height;
        }

        public void SetTexture(Texture2D aTexture, int aTextureWidht, int aTextureHeight)
        {
            this.texture = aTexture;
            this.textureWidth = aTextureWidht;
            this.textureHeight = aTextureHeight;
        }


        public Texture2D GetTexture()
        {
            return this.texture;
        }

        public int GetTextureWidth()
        {
            return this.textureWidth;
        }

        public int GetTextureHeight()
        {
            return this.textureHeight;
        }

        public void SetContainer(VoxelContainer acontainer)
        {
            this.container = acontainer;
            if (this.voxelList == null)
                this.voxelList = new List<Voxel>();
        }

        public void AddVoxel(Voxel aVoxel)
        {
            if (this.voxelList == null)            
                this.voxelList = new List<Voxel>();
            
            if (!this.voxelList.Contains(aVoxel))
                this.voxelList.Add(aVoxel);
        }

        public void RemoveVoxel(Voxel aVoxel)
        {
            this.voxelList.Remove(aVoxel);
        }

        public bool ContainsVoxel(Voxel aVoxel)
        {
            return this.voxelList.Contains(aVoxel);
        }

        public void ClearVoxelList()
        {
            this.voxelList.Clear();
        }

        
        public void ClearAreaBuffers()
        {
                        
        }

        public void GenerateBuffers()
        {
            try
            {
                this.vertices = null;
                this.uvs = null;
                this.triangles = null;

                this.vertices = new List<Vector3>();
                this.uvs = new List<Vector2>();
                this.triangles = new List<int>();

                if (this.voxelList != null)
                {
                    foreach (Voxel curVoxel in this.voxelList)
                    {
                        List<Vector3> newVertices = new List<Vector3>();
                        List<Vector2> newUvs = new List<Vector2>();
                        List<int> newTriangles = new List<int>();

                        this.container.BuildFacesForVoxel(curVoxel, newVertices, newTriangles, newUvs, texture, textureWidth, textureHeight);
                        // curVoxel.PrepareSelectionMask();

                        //shift triangles
                        for (int i = 0; i < newTriangles.Count; i++)
                        {
                            newTriangles[i] += this.vertices.Count;
                        }

                        this.vertices.AddRange(newVertices);
                        this.uvs.AddRange(newUvs);
                        this.triangles.AddRange(newTriangles);
                    }
                }
            }
            finally
            {
                this.isChanged = false;
            }
        }
    }
}
