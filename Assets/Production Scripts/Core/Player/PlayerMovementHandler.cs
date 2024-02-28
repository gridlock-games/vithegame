using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.Player
{
    public class PlayerMovementHandler : MovementHandler
    {
        [SerializeField] private Camera cameraInstance;
        [SerializeField] private Camera minimapCameraInstance;

        [Header("Locomotion Settings")]
        [SerializeField] private float angularSpeed = 540;
        [Header("Animation Settings")]
        [SerializeField] private float runAnimationTransitionSpeed = 5;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            movementPrediction.SetOrientation(newPosition, newRotation);
        }

        public void SetPredictionRigidbodyPosition(Vector3 newPosition)
        {
            movementPredictionRigidbody.position = newPosition;
        }

        public void SetCameraRotation(float rotationX, float rotationY)
        {
            cameraInstance.GetComponent<CameraController>().SetRotation(rotationX, rotationY);
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        [Header("Network Prediction")]
        [SerializeField] private Rigidbody movementPredictionRigidbody;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        [SerializeField] private float stairHeight = 0.5f;
        [SerializeField] private float rampCheckHeight = 0.1f;
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool isGrounded = true;
        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                if (IsOwner)
                {
                    moveForwardTarget.Value = 0;
                    moveSidesTarget.Value = 0;
                }
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition, movementPrediction.CurrentRotation);
            }

            Quaternion newRotation = movementPrediction.CurrentRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    newRotation = attributes.GetAilmentRotation();
                if (weaponHandler.IsAiming())
                    newRotation = Quaternion.LookRotation(camDirection);
                else
                    newRotation = Quaternion.RotateTowards(inputPayload.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            // Handle gravity
            RaycastHit[] allHits = Physics.SphereCastAll(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                                            gravitySphereCastRadius, Physics.gravity, gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 gravity = Vector3.zero;
            bool bHit = false;
            foreach (RaycastHit hit in allHits)
            {
                gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(hit.distance) * Physics.gravity;
                bHit = true;
                break;
            }
            if (!bHit) { gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity; }
            isGrounded = bHit;

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(weaponHandler.GetWeapon().GetRunSpeed() - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (animationHandler.ShouldApplyRootMotion())
            {
                movement = attributes.IsRooted() | animationHandler.IsReloading() ? Vector3.zero : rootMotion;
            }
            else
            {
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                movement = attributes.IsRooted() | animationHandler.IsReloading() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            // If we hit an object in the direction we are moving, we need to check if it is a stair/climbable
            if (Physics.Raycast(movementPrediction.CurrentPosition, movement.normalized, out RaycastHit lowerHit, 1, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
            {
                //Debug.DrawRay(movementPrediction.CurrentPosition + transform.up * rampCheckHeight, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate);
                // Check if we are walking up a ramp
                if (Physics.Raycast(movementPrediction.CurrentPosition + transform.up * rampCheckHeight, movement.normalized, out RaycastHit rampHit, 1, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
                {
                    // If the distances of the lowerHit and rampHit are the same, that means we are climbing a stairs
                    if (Mathf.Abs(rampHit.distance - lowerHit.distance) < 0.1f)
                    {
                        Debug.DrawRay(movementPrediction.CurrentPosition + transform.up * stairHeight, movement.normalized * lowerHit.distance, Color.black, 1f / NetworkManager.NetworkTickSystem.TickRate);
                        
                        if (Physics.Raycast(movementPrediction.CurrentPosition + transform.up * stairHeight, movement.normalized, out RaycastHit upperHit, lowerHit.distance + 0.1f, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
                        {
                            //Debug.Log(Time.time + " " + Mathf.Abs(lowerHit.distance - upperHit.distance));
                            if (Mathf.Abs(lowerHit.distance - upperHit.distance) > 0.1f)
                            {
                                movement.y += stairHeight / 2;
                            }
                        }
                        else
                        {
                            //Debug.Log(Time.time + " climbing stairs " + lowerHit.collider.name + " " + rampHit.collider.name);
                            movement.y += stairHeight / 2;
                        }
                    }
                }
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (IsOwner)
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }
            return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition + movement + gravity, newRotation);
        }

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraInstance.GetComponent<AudioListener>().enabled = true;
                cameraInstance.enabled = true;
                minimapCameraInstance.enabled = true;
                GetComponent<PlayerInput>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
            }
            else
            {
                Destroy(cameraInstance.gameObject);
                Destroy(minimapCameraInstance.gameObject);
                GetComponent<PlayerInput>().enabled = false;
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (cameraInstance) { Destroy(cameraInstance.gameObject); }
            if (movementPredictionRigidbody) { Destroy(movementPredictionRigidbody.gameObject); }
        }

        private PlayerNetworkMovementPrediction movementPrediction;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        protected new void Start()
        {
            base.Start();
            movementPredictionRigidbody.transform.SetParent(null, true);
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponent<Attributes>();
            animationHandler = GetComponent<AnimationHandler>();
        }

        private void OnEnable()
        {
            if (IsLocalPlayer)
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            if (IsLocalPlayer)
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }

        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private OnScreenStick[] joysticks = new OnScreenStick[0];
        private readonly float minimapCameraOffset = 15;
        private void Update()
        {
            if (!IsSpawned) { return; }

            // If on a mobile platform
            if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
                lookInput = Vector2.zero;
                PlayerInput playerInput = GetComponent<PlayerInput>();
                if (playerInput.currentActionMap.name == playerInput.defaultActionMap)
                {
                    foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                    {
                        if (touch.isTap)
                        {
                            OnInteract();
                        }
                        else
                        {
                            if (joysticks.Length == 0) { joysticks = GetComponentsInChildren<OnScreenStick>(); }

                            foreach (OnScreenStick joystick in joysticks)
                            {
                                if (!RectTransformUtility.RectangleContainsScreenPoint(joystick.transform.parent.GetComponent<RectTransform>(), touch.startScreenPosition) & touch.screenPosition.x > Screen.width / 2f)
                                {
                                    lookInput += touch.delta;
                                }
                            }
                        }
                    }
                }
            }
            
            UpdateLocomotion();
            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetBool("IsGrounded", isGrounded);

            if (minimapCameraInstance)
            {
                bool bHit = Physics.Raycast(transform.position, transform.up, out RaycastHit hit, minimapCameraOffset, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore);
                minimapCameraInstance.transform.localPosition = bHit ? new Vector3(0, hit.distance, 0) : new Vector3(0, minimapCameraOffset, 0);
            }
        }

        private float positionStrength = 1;
        //private float rotationStrength = 1;
        void FixedUpdate()
        {
            if (Vector3.Distance(movementPredictionRigidbody.position, movementPrediction.CurrentPosition) > 4)
            {
                movementPredictionRigidbody.position = movementPrediction.CurrentPosition;
            }
            else
            {
                Vector3 deltaPos = movementPrediction.CurrentPosition - movementPredictionRigidbody.position;
                movementPredictionRigidbody.velocity = 1f / Time.fixedDeltaTime * deltaPos * Mathf.Pow(positionStrength, 90f * Time.fixedDeltaTime);

                //(movementPrediction.CurrentRotation * Quaternion.Inverse(transform.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
                //if (angle > 180.0f) angle -= 360.0f;
                //movementPredictionRigidbody.angularVelocity = 1f / Time.fixedDeltaTime * 0.01745329251994f * angle * Mathf.Pow(rotationStrength, 90f * Time.fixedDeltaTime) * axis;
            }
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

            if (weaponHandler.CurrentActionClip != null)
                animationHandler.Animator.speed = (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * weaponHandler.CurrentActionClip.animationSpeed;

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

        void OnInteract()
        {
            RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, 15, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root.TryGetComponent(out NetworkInteractable networkInteractable))
                {
                    networkInteractable.Interact(gameObject);
                    break;
                }
            }
        }

        //private void OnDrawGizmos()
        //{
        //    if (!Application.isPlaying) { return; }
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawWireSphere(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset, gravitySphereCastRadius);
        //}
    }
}

