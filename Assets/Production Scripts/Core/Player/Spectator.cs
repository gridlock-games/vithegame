using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
    public class Spectator : MovementHandler
    {
        [SerializeField] private float moveSpeed = 7;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                GetComponent<PlayerInput>().enabled = true;
                GetComponent<Camera>().enabled = true;
                GetComponent<AudioListener>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
            }
            else
            {
                GetComponent<PlayerInput>().enabled = false;
                GetComponent<Camera>().enabled = false;
                GetComponent<AudioListener>().enabled = false;
                GetComponent<ActionMapHandler>().enabled = false;
            }
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        private bool isSprinting;
        void OnSprint(InputValue value)
        {
            isSprinting = value.isPressed;
        }

        private bool isAscending;
        void OnAscend(InputValue value)
        {
            isAscending = value.isPressed;
        }

        private bool isDescending;
        void OnDescend(InputValue value)
        {
            isDescending = value.isPressed;
        }

        private void Update()
        {
            if (!IsLocalPlayer) { return; }

            float verticalSpeed = 0;
            if (isAscending) { verticalSpeed = 1; }
            if (isDescending) { verticalSpeed = -1; }

            transform.Translate((isSprinting ? moveSpeed * 2 : moveSpeed) * Time.deltaTime * new Vector3(moveInput.x, verticalSpeed, moveInput.y));

            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - GetLookInput().y, transform.localEulerAngles.y + GetLookInput().x, transform.localEulerAngles.z);
        }
    }
}