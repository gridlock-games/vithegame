using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementHandler : MovementHandler
    {
        [SerializeField] private Camera cameraInstance;

        [Header("Locomotion Settings")]
        [SerializeField] private float runSpeed = 5;
        [SerializeField] private float angularSpeed = 540;
        [Header("Animation Settings")]
        [SerializeField] private float runAnimationTransitionSpeed = 5;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            movementPrediction.SetOrientation(newPosition, newRotation);
            base.SetOrientation(newPosition, newRotation);
        }

        public void SetCameraRotation(float rotationX, float rotationY)
        {
            cameraInstance.GetComponent<CameraController>().SetRotation(rotationX, rotationY);
        }

        private float moveForwardTarget;
        private float moveSidesTarget;
        private bool isGrounded;
        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            if (!CanMove())
            {
                moveForwardTarget = 0;
                moveSidesTarget = 0;
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition, movementPrediction.CurrentRotation);
            }

            if (attributes.ShouldApplyAilmentRotation())
            {
                Vector3 oldPos = transform.position;

                // Set position to current position
                characterController.enabled = false;
                transform.position = movementPrediction.CurrentPosition;
                characterController.enabled = true;

                characterController.Move(animationHandler.ApplyNetworkRootMotion());
                Vector3 newPos = transform.position;

                // Revert movement change
                characterController.enabled = false;
                transform.position = oldPos;
                characterController.enabled = true;

                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, newPos, attributes.GetAilmentRotation());
            }

            Vector3 oldPosition = transform.position;

            // Set position to current position
            characterController.enabled = false;
            transform.position = movementPrediction.CurrentPosition;
            characterController.enabled = true;

            Vector3 animDir = Vector3.zero;
            // Apply movement to charactercontroller
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(runSpeed - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            if (animationHandler.ShouldApplyRootMotion())
            {
                rootMotion += Physics.gravity * (1f / NetworkManager.NetworkTickSystem.TickRate);
                characterController.Move(attributes.IsRooted() ? Vector3.zero : rootMotion);
            }
            else
            {
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                targetDirection += Physics.gravity;
                characterController.Move(attributes.IsRooted() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection);
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }
            isGrounded = characterController.isGrounded;

            Vector3 newPosition = transform.position;

            // Revert movement change
            characterController.enabled = false;
            transform.position = oldPosition;
            characterController.enabled = true;

            Quaternion newRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
                camDirection.Scale(HORIZONTAL_PLANE);
                if (weaponHandler.IsAiming())
                {
                    newRotation = Quaternion.LookRotation(camDirection);
                }
                else
                {
                    newRotation = Quaternion.RotateTowards(inputPayload.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
                }
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            //if (animDir.magnitude < 0.1f) { animDir = Vector3.zero; }
            moveForwardTarget = animDir.z;
            moveSidesTarget = animDir.x;
            return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, newPosition, newRotation);
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
        private PlayerNetworkMovementPrediction movementPrediction;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        protected new void Start()
        {
            base.Start();
            characterController = GetComponent<CharacterController>();
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponentInParent<Attributes>();
            animationHandler = GetComponent<AnimationHandler>();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            UpdateLocomotion();
            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private void UpdateLocomotion()
        {
            //if (localDistance > movementPrediction.playerObjectTeleportThreshold)
            //{
            //    //Debug.Log("Teleporting player: " + OwnerClientId);
            //    characterController.enabled = false;
            //    transform.position = movementPrediction.CurrentPosition;
            //    characterController.enabled = true;
            //}

            animationHandler.Animator.speed = (Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / runSpeed;

            Vector3 movement = Time.deltaTime * (NetworkManager.NetworkTickSystem.TickRate / 2) * (movementPrediction.CurrentPosition - transform.position);
            characterController.enabled = false;
            transform.position += movement;
            characterController.enabled = true;

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = movementPrediction.CurrentRotation;
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * angularSpeed);
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (attributes.IsFeared() ? -1 : 1);
        }

        void OnDodge()
        {
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y), transform.forward, Vector3.up);
            animationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}

