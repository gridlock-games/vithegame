using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;

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
        [Header("Animation Settings")]
        [SerializeField] private float moveAnimSpeed = 5;

        public NetworkMovementPrediction.StatePayload ProcessMovement(NetworkMovementPrediction.InputPayload inputPayload)
        {
            Vector3 targetDirection = inputPayload.rotation * new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y);

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

            Vector3 oldPosition = transform.position;

            // Set position to current position
            characterController.enabled = false;
            transform.position = movementPrediction.currentPosition;
            characterController.enabled = true;

            // Apply movement to charactercontroller
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion();
            if (rootMotion != Vector3.zero)
            {
                rootMotion += Physics.gravity;
                characterController.Move(rootMotion);
            }
            else
            {
                characterController.Move(targetDirection * (1f / NetworkManager.NetworkTickSystem.TickRate));
            }

            Vector3 newPosition = transform.position;

            // Revert movement change
            characterController.enabled = false;
            transform.position = oldPosition;
            characterController.enabled = true;

            if (IsOwner)
            {
                Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
                camDirection.Scale(HORIZONTAL_PLANE);
                Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = inputPayload.rotation;
            }

            return new NetworkMovementPrediction.StatePayload(inputPayload.tick, newPosition, transform.rotation);
        }

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraInstance.GetComponent<AudioListener>().enabled = true;
                cameraInstance.enabled = true;
                GetComponent<PlayerInput>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
            }
            else
            {
                Destroy(cameraInstance.gameObject);
                GetComponent<PlayerInput>().enabled = false;
            }
        }

        private CharacterController characterController;
        private NetworkMovementPrediction movementPrediction;
        private Animator animator;
        private AnimationHandler animationHandler;
        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            movementPrediction = GetComponent<NetworkMovementPrediction>();
            animator = GetComponentInChildren<Animator>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            Vector3 targetDirection = movementPrediction.currentPosition - transform.position;

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            Vector3 animDir = targetDirection;
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

            Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, movementPrediction.currentRotation, Time.deltaTime * angularSpeed);

            Vector3 rootMotion = animationHandler.ApplyLocalRootMotion();
            if (rootMotion != Vector3.zero) // is root moving
            {
                rootMotion = transform.rotation * rootMotion;

                // Calculate rotation to look at the current network position
                Quaternion relativeRotation = Quaternion.identity;
                if ((movementPrediction.currentPosition - transform.position).normalized != Vector3.zero) { relativeRotation = Quaternion.LookRotation((movementPrediction.currentPosition - transform.position).normalized, transform.up); }

                // Calculate rotation to look in the direction of our movement
                Quaternion movementRotation = Quaternion.identity;
                if (rootMotion.normalized != Vector3.zero) { movementRotation = Quaternion.LookRotation(rootMotion.normalized, transform.up); }

                // Apply the direction of movement to the direction to move towards the current network position
                Quaternion finalRotation = relativeRotation * movementRotation;

                // Invert movement along the local x axis, idk why I need to do this
                rootMotion.x *= -1;
                // Apply rotation to movement vector
                rootMotion = finalRotation * rootMotion;

                // Scale movement vector according to distance between network position and local position
                float localDistance = Vector3.Distance(movementPrediction.currentPosition, transform.position);
                rootMotion = rootMotion.normalized * localDistance;

                // If our movement vector isn't going to reduce the distance between the local position and network position, simply move straight to the network position
                // This prevents some jitters
                float afterMoveDistance = Vector3.Distance(movementPrediction.currentPosition, transform.position + (rootMotion.normalized * localDistance));
                if (localDistance < afterMoveDistance)
                {
                    rootMotion = movementPrediction.currentPosition - transform.position;
                }

                rootMotion += Physics.gravity;

                characterController.Move(rootMotion);
                transform.rotation = targetRotation;
            }
            else
            {
                characterController.Move(targetDirection * Time.deltaTime);
                transform.rotation = targetRotation;
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * moveAnimSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * moveAnimSpeed));
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

        [Header("Dodge Assignments")]
        [SerializeField] private ActionClip dodgeF;
        [SerializeField] private ActionClip dodgeFL;
        [SerializeField] private ActionClip dodgeFR;
        [SerializeField] private ActionClip dodgeB;
        [SerializeField] private ActionClip dodgeBL;
        [SerializeField] private ActionClip dodgeBR;
        [SerializeField] private ActionClip dodgeL;
        [SerializeField] private ActionClip dodgeR;

        void OnDodge()
        {
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y), transform.forward, Vector3.up);

            ActionClip dodgeClip;
            if (angle <= 15f && angle >= -15f)
            {
                dodgeClip = dodgeF;
            }
            else if (angle < 80f && angle > 15f)
            {
                dodgeClip = dodgeFL;
            }
            else if (angle > -80f && angle < -15f)
            {
                dodgeClip = dodgeFR;
            }
            else if (angle > 80f && angle < 100f)
            {
                dodgeClip = dodgeL;
            }
            else if (angle < -80f && angle > -100f)
            {
                dodgeClip = dodgeR;
            }
            else if (angle < -100f && angle > -170f)
            {
                dodgeClip = dodgeBR;
            }
            else if (angle > 100f && angle < 170f)
            {
                dodgeClip = dodgeBL;
            }
            else
            {
                dodgeClip = dodgeB;
            }

            animationHandler.PlayAction(dodgeClip);
        }
    }
}

