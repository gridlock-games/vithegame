using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using Vi.Core;
using Vi.ScriptableObjects;
using System.Linq;

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

        [Header("Collision Settings")]
        [SerializeField] private float collisionPushDampeningFactor = 1;
        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            if (collision.collider.GetComponent<NetworkCollider>())
            {
                if (collision.relativeVelocity.magnitude > 1)
                {
                    if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { movementPredictionRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
                }
            }
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        private Vector3 lastMovement;
        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            if (collision.collider.GetComponent<NetworkCollider>())
            {
                if (collision.relativeVelocity.magnitude > 1)
                {
                    if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { movementPredictionRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
                }
            }
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        [Header("Network Prediction")]
        [SerializeField] private Rigidbody movementPredictionRigidbody;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
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
                lastMovement = Vector3.zero;
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition, movementPrediction.CurrentRotation);
            }

            Quaternion newRotation = movementPrediction.CurrentRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraInstance.transform.TransformDirection(Vector3.forward);
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    newRotation = attributes.GetAilmentRotation();
                if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.LookRotation(camDirection);
                else if (!attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.RotateTowards(inputPayload.rotation, Quaternion.LookRotation(camDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            RaycastHit[] allHits = Physics.SphereCastAll(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity,
                gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            bool bHit = false;
            foreach (RaycastHit gravityHit in allHits)
            {
                gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(gravityHit.distance) * Physics.gravity;
                bHit = true;
                break;
            }

            if (bHit)
            {
                isGrounded = true;
            }
            else // If no sphere cast hit
            {
                if (Physics.Raycast(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                    Physics.gravity, 1, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
                {
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                    gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity;
                }
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (animationHandler.ShouldApplyRootMotion())
            {
                if (attributes.IsRooted())
                {
                    movement = Vector3.zero;
                }
                else if (weaponHandler.CurrentActionClip.limitAttackMotionBasedOnTarget & (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking))
                {
                    movement = rootMotion;
                    ExtDebug.DrawBoxCastBox(movementPrediction.CurrentPosition + weaponHandler.CurrentActionClip.boxCastOriginPositionOffset, weaponHandler.CurrentActionClip.boxCastHalfExtents, movementPrediction.CurrentRotation * Vector3.forward, movementPrediction.CurrentRotation, weaponHandler.CurrentActionClip.boxCastDistance, Color.blue);
                    allHits = Physics.BoxCastAll(movementPrediction.CurrentPosition + weaponHandler.CurrentActionClip.boxCastOriginPositionOffset, weaponHandler.CurrentActionClip.boxCastHalfExtents, movementPrediction.CurrentRotation * Vector3.forward, movementPrediction.CurrentRotation, weaponHandler.CurrentActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                    System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                    foreach (RaycastHit hit in allHits)
                    {
                        if (hit.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.Attributes))
                            {
                                movement = Vector3.ClampMagnitude(movement, hit.distance);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    movement = rootMotion;
                }
            }
            else
            {
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                movement = attributes.IsRooted() | animationHandler.IsReloading() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = movementPrediction.CurrentPosition;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }

                Debug.DrawRay(startPos, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate);
                startPos.y += yOffset;
                stairMovement = startPos.y - movementPrediction.CurrentPosition.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement;

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (IsOwner)
            {
                if (weaponHandler.GetWeapon().IsWalking(weaponHandler.IsBlocking))
                {
                    moveForwardTarget.Value = animDir.z / 2;
                    moveSidesTarget.Value = animDir.x / 2;
                }
                else
                {
                    moveForwardTarget.Value = animDir.z;
                    moveSidesTarget.Value = animDir.x;
                }
            }
            lastMovement = movement;
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
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
            else
            {
                Destroy(cameraInstance.gameObject);
                Destroy(minimapCameraInstance.gameObject);
                GetComponent<PlayerInput>().enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (cameraInstance) { Destroy(cameraInstance.gameObject); }
            if (movementPredictionRigidbody) { Destroy(movementPredictionRigidbody.gameObject); }
        }

        private PlayerNetworkMovementPrediction movementPrediction;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        private void Start()
        {
            movementPredictionRigidbody.transform.SetParent(null, true);
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            attributes = GetComponent<Attributes>();
            animationHandler = GetComponent<AnimationHandler>();
        }

        private OnScreenStick[] joysticks = new OnScreenStick[0];
        private readonly float minimapCameraOffset = 15;
        private Vector2 lookInputToSubtract;
        private void Update()
        {
            if (!IsSpawned) { return; }

            #if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            if (IsLocalPlayer & UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                lookInput -= lookInputToSubtract;
                Vector2 lookInputToAdd = Vector2.zero;
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
                                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)joystick.transform.parent, touch.startScreenPosition) & touch.startScreenPosition.x > Screen.width / 2f)
                                {
                                    lookInputToAdd += touch.delta / 1.5f;
                                }
                            }
                        }
                    }
                }
            lookInput += lookInputToAdd;
            lookInputToSubtract = lookInputToAdd;
            }
            #endif

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

        private static readonly Vector3 targetSystemOffset = new Vector3(0, 1, 0);

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

                if (attributes.ShouldShake())
                {
                    movement += Random.insideUnitSphere * (Time.deltaTime * Attributes.ShakeAmount);
                }

                transform.position += movement;
            }

            if (weaponHandler.CurrentActionClip != null)
            {
                if (attributes.ShouldPlayHitStop())
                {
                    animationHandler.Animator.speed = 0;
                }
                else
                {
                    animationHandler.Animator.speed = (Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) * weaponHandler.CurrentActionClip.animationSpeed;
                }
            }

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);

            if (weaponHandler.CurrentActionClip.useRotationalTargetingSystem & cameraInstance)
            {
                if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking)
                {
                    CameraController cameraController = cameraInstance.GetComponent<CameraController>();
                    ExtDebug.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + weaponHandler.CurrentActionClip.boxCastOriginPositionOffset, weaponHandler.CurrentActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, weaponHandler.CurrentActionClip.boxCastDistance, Color.yellow);
                    RaycastHit[] allHits = Physics.BoxCastAll(cameraController.CameraPositionClone.transform.position + weaponHandler.CurrentActionClip.boxCastOriginPositionOffset, weaponHandler.CurrentActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, weaponHandler.CurrentActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                    List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                    foreach (RaycastHit hit in allHits)
                    {
                        if (hit.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.Attributes) & !networkCollider.Attributes.IsInvincible)
                            {
                                Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position + targetSystemOffset - cameraController.CameraPositionClone.transform.position, Vector3.up);
                                angleList.Add((networkCollider,
                                    Mathf.Abs(targetRot.eulerAngles.y - cameraController.CameraPositionClone.transform.eulerAngles.y) + Mathf.Abs((targetRot.eulerAngles.x < 180 ? targetRot.eulerAngles.x : targetRot.eulerAngles.x - 360) - (cameraController.CameraPositionClone.transform.eulerAngles.x < 180 ? cameraController.CameraPositionClone.transform.eulerAngles.x : cameraController.CameraPositionClone.transform.eulerAngles.x - 360)),
                                    hit));
                            }
                        }
                    }

                    angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                    foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position + targetSystemOffset - cameraController.CameraPositionClone.transform.position, Vector3.up);
                        if (angle < weaponHandler.CurrentActionClip.maximumTargetingRotationAngle)
                        {
                            cameraController.AddRotation(Mathf.Clamp(((targetRot.eulerAngles.x < 180 ? targetRot.eulerAngles.x : targetRot.eulerAngles.x - 360) - (cameraController.CameraPositionClone.transform.eulerAngles.x < 180 ? cameraController.CameraPositionClone.transform.eulerAngles.x : cameraController.CameraPositionClone.transform.eulerAngles.x - 360)) * Time.deltaTime * LimbReferences.rotationConstraintOffsetSpeed, -LimbReferences.rotationConstraintOffsetSpeed, LimbReferences.rotationConstraintOffsetSpeed),
                                Mathf.Clamp((targetRot.eulerAngles.y - cameraController.CameraPositionClone.transform.eulerAngles.y) * Time.deltaTime * LimbReferences.rotationConstraintOffsetSpeed, -LimbReferences.rotationConstraintOffsetSpeed, LimbReferences.rotationConstraintOffsetSpeed));
                            break;
                        }
                    }
                }
            }
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (attributes.IsFeared() ? -1 : 1);
        }

        void OnDodge()
        {
            if (animationHandler.IsReloading()) { return; }
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

