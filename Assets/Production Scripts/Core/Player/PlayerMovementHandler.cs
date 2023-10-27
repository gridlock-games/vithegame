using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementHandler : NetworkBehaviour
    {
        [SerializeField] private Camera cameraInstance;

        [Header("Locomotion Settings")]
        [SerializeField] private float runSpeed = 5;
        [SerializeField] private float angularSpeed = 540;
        [Header("Animation Settings")]
        [SerializeField] private float moveAnimSpeed = 5;

        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            if (attributes.ShouldApplyAilmentRotation())
            {
                Vector3 oldPos = transform.position;

                // Set position to current position
                characterController.enabled = false;
                transform.position = movementPrediction.currentPosition;
                characterController.enabled = true;

                characterController.Move(animationHandler.ApplyNetworkRootMotion());
                Vector3 newPos = transform.position;

                // Revert movement change
                characterController.enabled = false;
                transform.position = oldPos;
                characterController.enabled = true;

                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, newPos, attributes.GetAilmentRotation());
            }

            Vector3 targetDirection = inputPayload.rotation * new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y);

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
            targetDirection *= characterController.isGrounded ? runSpeed : 0;
            targetDirection += Physics.gravity;

            Vector3 oldPosition = transform.position;

            // Set position to current position
            characterController.enabled = false;
            transform.position = movementPrediction.currentPosition;
            characterController.enabled = true;

            // Apply movement to charactercontroller
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion();
            if (animationHandler.ShouldApplyRootMotion())
            {
                characterController.Move(rootMotion);
            }
            else
            {
                characterController.Move(1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection);
            }

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
                newRotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

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
        private Animator animator;
        private AnimationHandler animationHandler;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            animator = GetComponentInChildren<Animator>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponentInParent<Attributes>();

            if (!PlayerPrefs.HasKey("MouseXSensitivity")) { PlayerPrefs.SetFloat("MouseXSensitivity", 0.2f); }
            if (!PlayerPrefs.HasKey("MouseYSensitivity")) { PlayerPrefs.SetFloat("MouseYSensitivity", 0.2f); }
            lookSensitivity = new Vector2(PlayerPrefs.GetFloat("MouseXSensitivity"), PlayerPrefs.GetFloat("MouseYSensitivity"));
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            Vector3 targetDirection = movementPrediction.currentPosition - transform.position;
            if (targetDirection.magnitude > 0.1f)
            {
                Vector2 normalizedHorizontalMovement = new Vector2(targetDirection.x, targetDirection.z).normalized;
                targetDirection = new Vector3(normalizedHorizontalMovement.x, targetDirection.y, normalizedHorizontalMovement.y);
            }

            Vector3 animDir = targetDirection;

            targetDirection *= characterController.isGrounded ? runSpeed : 0;

            float localDistance = Vector3.Distance(movementPrediction.currentPosition, transform.position);
            //if (localDistance > movementPrediction.playerObjectTeleportThreshold) { targetDirection *= localDistance * (1/movementPrediction.playerObjectTeleportThreshold); }
            
            targetDirection += Physics.gravity;

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, movementPrediction.currentRotation, Time.deltaTime * angularSpeed);

            Vector3 rootMotion = animationHandler.ApplyLocalRootMotion();

            if (localDistance > movementPrediction.playerObjectTeleportThreshold)
            {
                //Debug.Log("Teleporting player: " + OwnerClientId);
                characterController.enabled = false;
                transform.position = movementPrediction.currentPosition;
                characterController.enabled = true;
            }
            else if (animationHandler.ShouldApplyRootMotion()) // is root moving
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
                //rootMotion = rootMotion.normalized * localDistance;
                if (localDistance > movementPrediction.rootMotionDistanceScaleThreshold) { rootMotion *= localDistance * (1/movementPrediction.rootMotionDistanceScaleThreshold); }

                characterController.Move(rootMotion);
            }
            else
            {
                characterController.Move(targetDirection * Time.deltaTime);
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (animDir.magnitude < 0.1f) { animDir = Vector3.zero; }
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * moveAnimSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * moveAnimSpeed));
        }

        private Vector2 lookSensitivity;
        public Vector2 GetLookInput() { return lookInput * lookSensitivity; }
        public Vector2 GetLookSensitivity() { return lookSensitivity; }
        public void SetLookSensitivity(Vector2 newLookSensitivity) { lookSensitivity = newLookSensitivity; }

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

        void OnDodge()
        {
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y), transform.forward, Vector3.up);
            animationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}

