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

        private void Update()
        {
            Vector3 targetEulers = Quaternion.LookRotation(cameraInstance.transform.forward, Vector3.up).eulerAngles;
            targetEulers.x = 0;
            targetEulers.z = 0;

            transform.eulerAngles = targetEulers;
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

