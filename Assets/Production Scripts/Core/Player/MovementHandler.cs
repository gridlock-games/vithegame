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
        [SerializeField] private Camera cameraInstance;

        [Header("Locomotion Settings")]
        [SerializeField] private float runSpeed = 5;

        [SerializeField] private float angularSpeed = 540;
        [SerializeField] private Vector2 lookSensitivity = new Vector2(0.2f, 0.2f);

        public NetworkMovementPrediction.StatePayload ProcessMovement(NetworkMovementPrediction.InputPayload inputPayload)
        {
            Vector3 targetDirection = inputPayload.rotation * new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y);

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

            Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
            camDirection.Scale(HORIZONTAL_PLANE);

            Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camDirection), Time.deltaTime * angularSpeed);

            Vector3 oldPosition = transform.position;

            // Set position to current position
            characterController.enabled = false;
            transform.position = movementPrediction.currentPosition;
            characterController.enabled = true;

            // Apply movement to charactercontroller
            characterController.Move(targetDirection * (1f / NetworkManager.NetworkTickSystem.TickRate));

            Vector3 newPosition = transform.position;

            // Revert movement change
            characterController.enabled = false;
            transform.position = oldPosition;
            characterController.enabled = true;

            if (IsOwner)
                transform.rotation = targetRotation;
            else
                transform.rotation = inputPayload.rotation;

            return new NetworkMovementPrediction.StatePayload(inputPayload.tick, newPosition, transform.rotation);
        }

        private CharacterController characterController;
        private NetworkMovementPrediction movementPrediction;
        private Animator animator;
        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            movementPrediction = GetComponent<NetworkMovementPrediction>();
            animator = GetComponentInChildren<Animator>();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            //targetPosition += transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * Time.deltaTime * runSpeed;

            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            Vector3 targetDirection = movementPrediction.currentPosition - transform.position;

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            Vector3 animDir = targetDirection;
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

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

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            animator.SetFloat("MoveForward", animDir.z);
            animator.SetFloat("MoveSides", animDir.x);
        }

        public Vector2 GetLookInput() { return lookInput * lookSensitivity; }

        private Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        public Vector2 GetMoveInput() { return moveInput; }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}

