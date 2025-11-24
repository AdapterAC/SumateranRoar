using UnityEngine;

namespace FOV.Demo
{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;

        private Transform cam = null;
        private FieldOfView view = null;

        void Start()
        {
            cam = Camera.main.transform;
            view = GetComponent<FieldOfView>();
        }

        void Update()
        {
            // Get WASD input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Get camera forward and right (flattened on the XZ plane)
            Vector3 camForward = cam.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cam.right;
            camRight.y = 0;
            camRight.Normalize();

            // Combine input with camera direction
            Vector3 moveDir = camForward * vertical + camRight * horizontal;

            // Move the player
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Rotate the player to face movement direction
            if (moveDir.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            //Field for transform.
            var list = view.Field<Transform>();

            //Looping on the list and printing the names.
            list.ForEach(t => Debug.Log(t.name));
        }
    }
}