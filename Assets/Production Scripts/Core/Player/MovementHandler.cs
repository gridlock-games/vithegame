using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Vi.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementHandler : NetworkBehaviour
    {
        public Vector3 targetPosition;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(targetPosition, 0.25f);
        }

        [SerializeField] private Camera cameraInstance;

        [Header("Locomotion Settings")]
        [SerializeField] private float runSpeed = 5;

        [SerializeField] private float angularSpeed = 540;
        [SerializeField] private Vector2 lookSensitivity = new Vector2(0.2f, 0.2f);

        public NetworkMovementPrediction.StatePayload ProcessMovement()
        {

        }

        private CharacterController characterController;
        private void Start()
        {
            characterController = GetComponent<CharacterController>();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            targetPosition += transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * Time.deltaTime * runSpeed;

            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            Vector3 targetDirection = targetPosition - transform.position;

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

            //Quaternion targetRotation = this.UpdateRotation(targetDirection);

            Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
            camDirection.Scale(HORIZONTAL_PLANE);

            Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camDirection), Time.deltaTime * angularSpeed);

            if (false) // is root moving
            {
                //UpdateRootMovement(Physics.gravity);
                transform.rotation = targetRotation;
            }
            else
            {
                characterController.Move(targetDirection * Time.deltaTime);
                transform.rotation = targetRotation;
            }
        }

        public Vector2 GetLookInput() { return lookInput * lookSensitivity; }

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

