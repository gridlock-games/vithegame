using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
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

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool isGrounded;
        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            if (!CanMove())
            {
                if (IsOwner)
                {
                    moveForwardTarget.Value = 0;
                    moveSidesTarget.Value = 0;
                }
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition, movementPrediction.CurrentRotation);
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement to charactercontroller
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(runSpeed - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (animationHandler.ShouldApplyRootMotion())
            {
                //rootMotion += Physics.gravity * (1f / NetworkManager.NetworkTickSystem.TickRate);
                movement = attributes.IsRooted() ? Vector3.zero : rootMotion;
            }
            else
            {
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                //targetDirection += Physics.gravity;
                movement = attributes.IsRooted() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            RaycastHit[] allHits = Physics.CapsuleCastAll(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * animationHandler.LimbReferences.bottomPointOfCapsuleOffset,
                                                          movementPrediction.CurrentPosition + transform.up * animationHandler.LimbReferences.characterHeight,
                                                          animationHandler.LimbReferences.characterRadius, movement, movement.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            bool bHit = false;
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root == transform) { continue; }

                bHit = true;
                break;
            }

            Vector3 newPosition = movementPrediction.CurrentPosition;
            if (!bHit) { newPosition += movement; }
            isGrounded = true;

            Quaternion newRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    transform.rotation = attributes.GetAilmentRotation();
                if (weaponHandler.IsAiming())
                    newRotation = Quaternion.LookRotation(camDirection);
                else
                    newRotation = Quaternion.RotateTowards(inputPayload.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            //if (animDir.magnitude < 0.1f) { animDir = Vector3.zero; }
            if (IsOwner)
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
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

        private Rigidbody rb;
        private PlayerNetworkMovementPrediction movementPrediction;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        protected new void Start()
        {
            base.Start();
            rb = GetComponent<Rigidbody>();
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponentInParent<Attributes>();
            animationHandler = GetComponent<AnimationHandler>();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void Update()
        {
            UpdateLocomotion();
            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private void UpdateLocomotion()
        {
            if (Vector3.Distance(transform.position, movementPrediction.CurrentPosition) > movementPrediction.playerObjectTeleportThreshold)
            {
                //Debug.Log("Teleporting player: " + OwnerClientId);
                transform.position = movementPrediction.CurrentPosition;
            }
            else
            {
                Vector3 movement = Time.deltaTime * (NetworkManager.NetworkTickSystem.TickRate / 2) * (movementPrediction.CurrentPosition - transform.position);
                transform.position += movement;
            }

            animationHandler.Animator.speed = (Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / runSpeed;

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            DrawWireCapsule(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * animationHandler.LimbReferences.bottomPointOfCapsuleOffset, movementPrediction.CurrentPosition + transform.up * animationHandler.LimbReferences.characterHeight, animationHandler.LimbReferences.characterRadius);
        }

        public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
            #if UNITY_EDITOR
            // Special case when both points are in the same position
            if (p1 == p2)
            {
                // DrawWireSphere works only in gizmo methods
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }
            using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                Quaternion p1Rotation = Quaternion.LookRotation(p1 - p2);
                Quaternion p2Rotation = Quaternion.LookRotation(p2 - p1);
                // Check if capsule direction is collinear to Vector.up
                float c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
                if (c == 1f || c == -1f)
                {
                    // Fix rotation
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // First side
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // Second side
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // Lines
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);
            }
            #endif
        }
    }
}

