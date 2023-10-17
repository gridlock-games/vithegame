using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Vi.Player
{
    public class MovementHandler : NetworkBehaviour
    {
        [SerializeField] private Camera cameraInstance;

        public void ProcessMovement()
        {

        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        [SerializeField] private float angularSpeed = 540;
        private void Update()
        {
            Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
            camDirection.Scale(HORIZONTAL_PLANE);

            Quaternion srcRotation = transform.rotation;
            Quaternion dstRotation = Quaternion.LookRotation(camDirection);

            transform.rotation = Quaternion.RotateTowards(srcRotation, dstRotation, Time.deltaTime * angularSpeed);
        }

        public Vector2 GetLookInput() { return lookInput * lookSensitivity; }

        [SerializeField] private Vector2 lookSensitivity = new Vector2(0.2f, 0.2f);

        private Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

    }
}

