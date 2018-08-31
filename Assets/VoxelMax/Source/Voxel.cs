using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace VoxelMax
{
    [System.Serializable]
    public class Voxel
    {
        [System.NonSerialized]
        public VoxelContainer voxelContainer = null;
        public Vector3 position;

        [SerializeField]
        private List<Vector3> neighborDirections = new List<Vector3>();

        [SerializeField]
        private Color fcolor = Color.white;        
        private bool fselected = false;
                               
        public void SetContainer(VoxelContainer aVoxelContainer)
        {
            this.voxelContainer = aVoxelContainer;
            this.selectionList = null;
        }

        public Color color
        {
            get
            {
                return this.fcolor;
            }
            set
            {
                this.fcolor = value;
                try
                {
                    if (this.voxelContainer != null)
                    {
                        VoxelArea voxelArea = this.voxelContainer.GetVoxelAreaForVoxel(this);
                        if (voxelArea != null)
                            voxelArea.isChanged = true;
                    }
                }
                catch
                {
                    Debug.Log("Error while setting area changed");
                }
            }
        }

        public bool selected
        {
            get
            {
                return this.fselected;
            }
            set
            {
                this.fselected = value;
            }
        }


        public bool IsPossibleExtrudeDirection(Vector3 aDirection)
        {            
            for (int i = 0; i < this.neighborDirections.Count; i++)
            {
                if (neighborDirections[i] == aDirection) return false;
            }
            return true;
        }

        public int GetMustDrawFacesCount()
        {
            int result = 0; 
            for (int i=0; i<6; i++)
            {
                if (this.IsPossibleExtrudeDirection(StaticValues.sixDirectionArray[i])) result++;
            }
            return result;
        }

        public bool EqualVoxels(Voxel aVoxel)
        {
            if (this.color != aVoxel.color) return false;
            if (this.position != aVoxel.position) return false;
            return true;
        }

        #region SelectionHandles
        [System.NonSerialized]
        private List<Vector3> selectionList = null;
        [System.NonSerialized]
        private Vector3 preparedInPosition = new Vector3();
        [System.NonSerialized]
        private Vector3 preparedInRotation = new Vector3();
        [System.NonSerialized]
        private Vector3 preparedInScale = new Vector3();

        public void PrepareSelectionMask()
        {
            this.selectionList = null;
            this.selectionList = new List<Vector3>();

            this.preparedInPosition = this.voxelContainer.transform.position;
            this.preparedInRotation = this.voxelContainer.transform.rotation.eulerAngles;
            this.preparedInScale = this.voxelContainer.transform.localScale;

            Transform curTransform = this.voxelContainer.transform;

            Vector3 centerPosition = curTransform.TransformPoint(position);
            if (this.IsPossibleExtrudeDirection(Vector3.forward))
            {
                //Front
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(-0.5f, -0.5f, 0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.one) - curTransform.TransformVector(Vector3.forward) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.right) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);

            }
            if (this.IsPossibleExtrudeDirection(Vector3.back))
            {
                //Back
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(-0.5f, -0.5f, -0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.one) - curTransform.TransformVector(Vector3.forward) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.right) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
            }
            if (this.IsPossibleExtrudeDirection(Vector3.up))
            {
                //Top
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(-0.5f, -0.5f, 0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(1f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(1f, 1f, 0f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
            }
            if (this.IsPossibleExtrudeDirection(Vector3.down))
            {
                //Bottom
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(-0.5f, -1.5f, 0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(1f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(1f, 1f, 0f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
            }
            if (this.IsPossibleExtrudeDirection(Vector3.left))
            {
                //Left
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(-0.5f, -0.5f, 0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 0f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
            }
            if (this.IsPossibleExtrudeDirection(Vector3.right))
            {
                //Right
                Vector3 facePosition = centerPosition + curTransform.TransformVector(new Vector3(0.5f, -0.5f, 0.5f));
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.up) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 1f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(new Vector3(0f, 0f, -1f)) + facePosition);
                this.selectionList.Add(curTransform.TransformVector(Vector3.zero) + facePosition);
            }
        }

        private bool IsTransformChanged()
        {
            if ((this.voxelContainer == null)) return false;

            if ((this.preparedInPosition != this.voxelContainer.transform.position) ||
                (this.preparedInRotation != this.voxelContainer.transform.rotation.eulerAngles) ||
                (this.preparedInScale != this.voxelContainer.transform.localScale))
                return true;
            return false;
        }

        public void DrawSelection()
        {
            if (selected)
            {
                if ((selectionList == null) || (this.IsTransformChanged()))
                    this.PrepareSelectionMask();
                #if UNITY_EDITOR
                    Handles.color = StaticValues.selectionColor; 
                    Handles.DrawPolyLine(selectionList.ToArray());
                #endif
            }
        }

        public bool IsInnerVoxel()
        {
            if (neighborDirections!=null)
                if (this.neighborDirections.Count == 6) return true;
            return false;
        }

        public void UpdateNeighborList()
        {
            this.neighborDirections.Clear();

            if (this.voxelContainer == null)
                return;

            for (int i = 0; i < StaticValues.sixDirectionArray.Length; i++)
            {
                Voxel neighborVoxel=this.voxelContainer.GetVoxelByCoordinate(this.position + StaticValues.sixDirectionArray[i]);
                if (neighborVoxel != null)
                {
                    this.neighborDirections.Add(StaticValues.sixDirectionArray[i]);                    
                }
            }           
        }
        #endregion
    }
}