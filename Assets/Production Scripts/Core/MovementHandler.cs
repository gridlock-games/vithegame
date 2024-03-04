using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class MovementHandler : NetworkBehaviour
    {
        public virtual void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
        }

        public virtual void ReceiveOnCollisionEnterMessage(Collision collision) { }
        public virtual void ReceiveOnCollisionStayMessage(Collision collision) { }
        public virtual void ReceiveOnCollisionExitMessage(Collision collision) { }

        protected WeaponHandler weaponHandler;
        protected void Awake()
        {
            weaponHandler = GetComponent<WeaponHandler>();
        }

        protected Vector2 lookInput;
        public Vector2 GetLookInput()
        {
            Vector2 lookSensitivity = new Vector2(PlayerPrefs.GetFloat("MouseXSensitivity"), PlayerPrefs.GetFloat("MouseYSensitivity")) * (bool.Parse(PlayerPrefs.GetString("InvertMouse")) ? -1 : 1);
            if (weaponHandler)
            {
                if (weaponHandler.IsAiming()) { lookSensitivity *= PlayerPrefs.GetFloat("ZoomSensitivityMultiplier"); }
            }
            return lookInput * lookSensitivity;
        }

        public Vector2 GetMoveInput() { return moveInput; }

        protected Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private NetworkVariable<bool> canMove = new NetworkVariable<bool>(true);

        public bool CanMove() { return canMove.Value; }

        public void SetCanMove(bool canMove)
        {
            if (!IsServer) { Debug.LogError("MovementHandler.SetCanMove() should only be called on the server!"); return; }
            this.canMove.Value = canMove;
        }
    }
}