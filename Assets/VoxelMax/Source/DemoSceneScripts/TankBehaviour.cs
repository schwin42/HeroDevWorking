using UnityEngine;
using System.Collections;
namespace VoxelMax
{
    public class TankBehaviour : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float bodyRotationSpeed = 30f;
        public float turretRotationSpeed = 30f;
        public float timebetweenShoots = 3f;
        public GameObject turret = null;
        public GameObject rocket = null;
        public GameObject rocketInstantiatePoint = null;
        private float lastTimeShoot = 0f;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                this.gameObject.transform.position += this.gameObject.transform.TransformVector(Vector3.right) * moveSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                this.gameObject.transform.position -= this.gameObject.transform.TransformVector(Vector3.right) * moveSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                this.gameObject.transform.Rotate(Vector3.up, -this.bodyRotationSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.D))
            {
                this.gameObject.transform.Rotate(Vector3.up, this.bodyRotationSpeed * Time.deltaTime);
            }
            if (turret != null)
            {
                if (Input.GetKey(KeyCode.Q))
                {
                    this.turret.transform.Rotate(Vector3.up, -this.turretRotationSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    this.turret.transform.Rotate(Vector3.up, this.turretRotationSpeed * Time.deltaTime);
                }
            }
            if ((rocket != null) && (rocketInstantiatePoint != null))
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    this.Shoot();
                }
            }

            if (Input.GetMouseButtonDown(0)) {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    if (this.turret != null)
                    {
                        float angle = Vector2.Angle(Vector2.right, (new Vector2(hit.point.x, hit.point.z) - new Vector2(this.turret.transform.position.x, this.turret.transform.position.z)).normalized);
                        this.turret.transform.localEulerAngles = new Vector3(0, angle, 0) * Mathf.Sign(hit.point.z - this.turret.transform.position.z) * -1f;
                        this.Shoot();
                    }
                }
            }                                                  
        }

        public void Shoot()
        {
            if ((Time.fixedTime - lastTimeShoot) > this.timebetweenShoots)
            {
                lastTimeShoot = Time.fixedTime;
                GameObject newGameObject = Instantiate(rocket);
                newGameObject.transform.position = rocketInstantiatePoint.transform.position;
                newGameObject.transform.Rotate(Vector3.left, turret.transform.rotation.eulerAngles.y);
            }
        }
    }
}