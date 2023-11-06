using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Vi.Core
{
    public class MovementHandler : NetworkBehaviour
    {
        public Vector2 GetMoveInput() { return moveInput; }

        protected Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}