using UnityEngine;
using System.Collections;

namespace VoxelMax
{
    public class Rocket : MonoBehaviour
    {
        public float speed = 20f;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            this.gameObject.transform.position += this.gameObject.transform.TransformDirection(Vector3.up) * speed * Time.deltaTime;
        }

        public void OnCollisionEnter(Collision collision)
        {
            Debug.Log("On collision enter");
            VoxelBomb bomb = this.gameObject.GetComponent<VoxelBomb>();
            if (bomb != null)
            {
                bomb.triggered = true;
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            //VoxelBomb bomb = this.gameObject.GetComponent<VoxelBomb>();
            //if (bomb != null)
            //{
            //    bomb.triggered = true;
            //}
        }

    }
}