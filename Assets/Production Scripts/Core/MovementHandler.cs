using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Vi.Core
{
    public class MovementHandler : NetworkBehaviour
    {
        public virtual void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (TryGetComponent(out CharacterController characterController))
            {
                characterController.enabled = false;
                transform.position = newPosition;
                transform.rotation = newRotation;
                characterController.enabled = true;
            }
            else
            {
                transform.position = newPosition;
                transform.rotation = newRotation;
            }
        }

        protected void Start()
        {
            if (!PlayerPrefs.HasKey("MouseXSensitivity")) { PlayerPrefs.SetFloat("MouseXSensitivity", 0.2f); }
            if (!PlayerPrefs.HasKey("MouseYSensitivity")) { PlayerPrefs.SetFloat("MouseYSensitivity", 0.2f); }
            lookSensitivity = new Vector2(PlayerPrefs.GetFloat("MouseXSensitivity"), PlayerPrefs.GetFloat("MouseYSensitivity"));
        }

        protected Vector2 lookInput;
        public Vector2 GetLookInput() { return lookInput * lookSensitivity; }
        public Vector2 GetRawLookInput() { return lookInput; }
        public Vector2 GetLookSensitivity() { return lookSensitivity; }
        public void SetLookSensitivity(Vector2 newLookSensitivity) { lookSensitivity = newLookSensitivity; }

        protected Vector2 lookSensitivity;

        public Vector2 GetMoveInput() { return moveInput; }

        protected Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}