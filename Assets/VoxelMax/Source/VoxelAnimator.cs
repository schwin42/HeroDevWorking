using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelMax
{
    [ExecuteInEditMode]
    public class VoxelAnimator : MonoBehaviour
    {
        public List<GameObject> voxelContainerPrefabs=new List<GameObject>();
        public string animationName = "";
        public float framesPerSecond=3f;
        public bool playLooped = true;
        public bool playOnAwake = true;        
        private bool playing = false;

        private int curFrameIndex = 0;
        private float lastFrameShift = 0;
        [SerializeField]
        private List<GameObject> frames = new List<GameObject>();

        public bool IsPlaying
        {
            get
            {
                return playing;
            }      
        }
   
        void Start()
        {
            InstantiatePrefabs();
            if (this.playOnAwake)
                this.playing = true;
        }

        // Update is called once per frame
        void Update()
        {            
            if (this.playing)
            {                
                if (lastFrameShift == 0) lastFrameShift = Time.time;
                if ((Time.time - lastFrameShift) > (1f / this.framesPerSecond))
                {
                    curFrameIndex++;
                    if (curFrameIndex >= this.frames.Count)
                    {
                        curFrameIndex = 0;
                        if (!playLooped) playing = false;
                    }
                    lastFrameShift = Time.time;
                }
                for(int i=0; i<this.frames.Count; i++)
                {
                    if (this.frames[i] != null)
                    {
                        if (curFrameIndex != i)
                            this.frames[i].SetActive(false);
                        else
                            this.frames[i].SetActive(true);
                    }
                }
            }
        }

        /*
        Instantiate and fix the frame list if neccesary
        */
        private void InstantiatePrefabs()
        {
            bool matchingAlreadyInstaniated = true;
            if (this.voxelContainerPrefabs.Count != this.frames.Count)
                matchingAlreadyInstaniated = false;
            else
            {
                for (int i = 0; i < this.voxelContainerPrefabs.Count; i++)
                {
                    GameObject curPrefab = this.voxelContainerPrefabs[i];
                    GameObject curFrame = this.frames[i];
                    if (curFrame == null)
                    {
                        matchingAlreadyInstaniated = false;
                        break;
                    }
                    VoxelContainer prefabContainer = curPrefab.GetComponent<VoxelContainer>();
                    VoxelContainer frameVoxelContainer = curFrame.GetComponent<VoxelContainer>();
                    if (prefabContainer == null)
                    {
                        Debug.LogError("One of the prefab in the animation '" + this.animationName + "' is does not have VoxelContainer!");
                        return;
                    }

                    if (prefabContainer.modelFilename != frameVoxelContainer.modelFilename)
                    {
                        matchingAlreadyInstaniated = false;
                        break;
                    }
                }
            }
            if (!matchingAlreadyInstaniated)
            {
                foreach (GameObject curFrame in this.frames)
                {
                    DestroyImmediate(curFrame);
                }
                this.frames.Clear();

                foreach (GameObject curPrefab in this.voxelContainerPrefabs)
                {
                    GameObject newObject = Instantiate(curPrefab);
                    newObject.transform.parent = this.transform;
                    newObject.SetActive(false);
                    this.frames.Add(newObject);
                }
            }
        }

        #region UserInterface
        public void StartAnimation()
        {
            this.playing = true;                            
        }
        public void StopAnimation()
        {
            this.playing = false;
        }
        public void RewindAnimation()
        {
            this.curFrameIndex = 0;
            this.lastFrameShift = Time.time;
        }
        #endregion
    }
}