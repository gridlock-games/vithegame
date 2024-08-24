using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.ProceduralAnimations;

namespace Vi.Player
{
    public class PlayerMovementHandler : MovementHandler
    {
        [SerializeField] private CameraController cameraController;

        [Header("Locomotion Settings")]
        [SerializeField] private float angularSpeed = 540;
        [Header("Animation Settings")]
        [SerializeField] private float runAnimationTransitionSpeed = 5;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            movementPrediction.SetOrientation(newPosition, newRotation);
        }

        public override Vector3 GetPosition() { return movementPrediction.CurrentPosition; }

        public override void SetImmovable(bool isKinematic)
        {
            movementPredictionRigidbody.constraints = isKinematic ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.FreezeRotation;
        }

        public bool IsCameraAnimating() { return cameraController.IsAnimating; }

        public Transform TargetToLockOn { get; private set; }
        public void LockOnTarget(Transform target)
        {
            TargetToLockOn = target;
        }

        public void SetPredictionRigidbodyPosition(Vector3 newPosition)
        {
            movementPredictionRigidbody.position = newPosition;
        }

        public void SetCameraRotation(float rotationX, float rotationY)
        {
            cameraController.SetRotation(rotationX, rotationY);
        }

        public override void Flinch(Vector2 flinchAmount)
        {
            cameraController.AddRotation(flinchAmount.x, flinchAmount.y);
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            //if (collision.collider.GetComponent<NetworkCollider>())
            //{
            //    if (collision.relativeVelocity.magnitude > 1)
            //    {
            //        if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { movementPredictionRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
            //    }
            //}
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        private Vector3 lastMovement;
        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            //if (collision.collider.GetComponent<NetworkCollider>())
            //{
            //    if (collision.relativeVelocity.magnitude > 1)
            //    {
            //        if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { movementPredictionRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
            //    }
            //}
            movementPrediction.ProcessCollisionEvent(collision, movementPredictionRigidbody.position);
        }

        private float GetTickRateDeltaTime()
        {
            return 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale;
        }

        private float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount() + attributes.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        [Header("Network Prediction")]
        [SerializeField] private Rigidbody movementPredictionRigidbody;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool isGrounded = true;
        RaycastHit[] allGravityHits = new RaycastHit[10];
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            Quaternion newRotation = movementPrediction.CurrentRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    newRotation = attributes.GetAilmentRotation();
                else if (attributes.AnimationHandler.IsGrabAttacking())
                    newRotation = inputPayload.rotation;
                else if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.LookRotation(camDirection);
                else if (!attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.LookRotation(camDirection);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                if (IsOwner)
                {
                    moveForwardTarget.Value = 0;
                    moveSidesTarget.Value = 0;
                }
                isGrounded = true;
                lastMovement = Vector3.zero;
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, movementPrediction.CurrentPosition, newRotation);
            }

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            int allGravityHitsCount = Physics.SphereCastNonAlloc(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity.normalized, allGravityHits, gravitySphereCastPositionOffset.magnitude,
                LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);

            bool bHit = false;
            float minDistance = 0;
            bool minDistanceInitialized = false;
            Vector3 amountToAddToGravity = Vector3.zero;
            for (int i = 0; i < allGravityHitsCount; i++)
            {
                if (allGravityHits[i].distance > minDistance & minDistanceInitialized) { continue; }
                bHit = true;
                amountToAddToGravity = GetTickRateDeltaTime() * Mathf.Clamp01(allGravityHits[i].distance) * Physics.gravity;
                minDistance = allGravityHits[i].distance;
                minDistanceInitialized = true;
            }
            gravity += amountToAddToGravity;

            if (bHit)
            {
                isGrounded = true;
            }
            else // If no sphere cast hit
            {
                if (Physics.Raycast(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                    Physics.gravity, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
                {
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                    gravity += GetTickRateDeltaTime() * Physics.gravity;
                }
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = attributes.AnimationHandler.ApplyNetworkRootMotion() * GetRootMotionSpeed();
            Vector3 movement;
            if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (attributes.AnimationHandler.ShouldApplyRootMotion())
            {
                if (attributes.StatusAgent.IsRooted() & attributes.GetAilment() != ActionClip.Ailment.Knockup & attributes.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else if (weaponHandler.CurrentActionClip.limitAttackMotionBasedOnTarget & (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking) | attributes.AnimationHandler.IsLunging())
                {
                    movement = rootMotion;

                    # if UNITY_EDITOR
                    ExtDebug.DrawBoxCastBox(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, movementPrediction.CurrentRotation * Vector3.forward, movementPrediction.CurrentRotation, ActionClip.boxCastDistance, Color.blue, GetTickRateDeltaTime());
                    # endif

                    int rootMotionHitCount = Physics.BoxCastNonAlloc(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset,
                        ActionClip.boxCastHalfExtents, (movementPrediction.CurrentRotation * Vector3.forward).normalized, rootMotionHits,
                        movementPrediction.CurrentRotation, ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                    
                    List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();

                    for (int i = 0; i < rootMotionHitCount; i++)
                    {
                        if (rootMotionHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
                            {
                                Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - movementPrediction.CurrentPosition, Vector3.up);
                                angleList.Add((networkCollider,
                                    Mathf.Abs(targetRot.eulerAngles.y - movementPrediction.CurrentRotation.eulerAngles.y),
                                    rootMotionHits[i]));
                            }
                        }
                    }

                    angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                    foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - movementPrediction.CurrentPosition, Vector3.up);
                        if (angle < ActionClip.maximumRootMotionLimitRotationAngle)
                        {
                            movement = Vector3.ClampMagnitude(movement, hit.distance);
                            break;
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
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.moveInput.x, 0, inputPayload.moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? GetRunSpeed() : 0;
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : GetTickRateDeltaTime() * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }
            
            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = movementPrediction.CurrentPosition;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }
#if UNITY_EDITOR
                Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
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

            bool wasPlayerHit = Physics.CapsuleCast(movementPrediction.CurrentPosition, movementPrediction.CurrentPosition + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            //bool wasPlayerHit = Physics.Raycast(movementPrediction.CurrentPosition + bodyHeightOffset / 2, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            if (wasPlayerHit)
            {
                bool collidersIgnoreEachOther = false;
                foreach (Collider c in attributes.NetworkCollider.Colliders)
                {
                    if (Physics.GetIgnoreCollision(playerHit.collider, c))
                    {
                        collidersIgnoreEachOther = true;
                        break;
                    }
                }

                if (!collidersIgnoreEachOther)
                {
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - movementPrediction.CurrentPosition, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            movement += forceAccumulated * GetTickRateDeltaTime();
            forceAccumulated = Vector3.MoveTowards(forceAccumulated, Vector3.zero, drag * GetTickRateDeltaTime());

            lastMovement = movement;

            Vector3 newPosition;
            if (Mathf.Approximately(movement.y, 0))
            {
                newPosition = movementPrediction.CurrentPosition + movement + gravity;
            }
            else
            {
                newPosition = movementPrediction.CurrentPosition + movement;
            }

            return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, newPosition, newRotation);
        }

        private const float drag = 1;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraController.gameObject.AddComponent<AudioListener>();
                cameraController.Camera.enabled = true;

                playerInput.enabled = true;
                string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);

                GetComponent<ActionMapHandler>().enabled = true;
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
            else
            {
                Destroy(cameraController.gameObject);
                Destroy(playerInput);
            }
            movementPredictionRigidbody.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (cameraController) { Destroy(cameraController.gameObject); }
            if (movementPredictionRigidbody) { Destroy(movementPredictionRigidbody.gameObject); }
        }

        private new void Awake()
        {
            base.Awake();
            RefreshStatus();
        }

        private PlayerNetworkMovementPrediction movementPrediction;
        private Attributes attributes;
        private void Start()
        {
            movementPredictionRigidbody.transform.SetParent(null, true);
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            attributes = GetComponent<Attributes>();

            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room"))
            {
                cameraController.PlayAnimation("TutorialIntro");
            }
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera) { return; }
            mainCamera = Camera.main;
        }

        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];
        RaycastHit[] interactableHits = new RaycastHit[10];
        private new void Update()
        {
            base.Update();
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            FindMainCamera();

            if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                SetImmovable(attributes.AnimationHandler.IsGrabAttacking());
            }
            else
            {
                SetImmovable(attributes.IsGrabbed());
            }

            if (!IsSpawned) { return; }

#if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            if (IsLocalPlayer & UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                Vector2 lookInputToAdd = Vector2.zero;
                if (playerInput.currentActionMap.name == playerInput.defaultActionMap)
                {
                    foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                    {
                        if (joysticks.Length == 0) { joysticks = GetComponentsInChildren<UIDeadZoneElement>(); }

                        bool isTouchingJoystick = false;
                        foreach (UIDeadZoneElement joystick in joysticks)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)joystick.transform.parent, touch.startScreenPosition))
                            {
                                isTouchingJoystick = true;
                                break;
                            }
                        }

                        if (!isTouchingJoystick)
                        {
                            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                            {
                                int interactableHitCount = Physics.RaycastNonAlloc(mainCamera.ScreenPointToRay(touch.screenPosition),
                                    interactableHits, 10, LayerMask.GetMask(interactableRaycastLayers), QueryTriggerInteraction.Ignore);

                                float minDistance = 0;
                                bool minDistanceInitialized = false;
                                NetworkInteractable networkInteractable = null;
                                for (int i = 0; i < interactableHitCount; i++)
                                {
                                    if (interactableHits[i].distance > minDistance & minDistanceInitialized) { continue; }
                                    networkInteractable = interactableHits[i].transform.root.GetComponent<NetworkInteractable>();
                                    minDistance = interactableHits[i].distance;
                                    minDistanceInitialized = true;
                                }
                                if (networkInteractable) { networkInteractable.Interact(gameObject); }
                            }
                        }
                        
                        if (!isTouchingJoystick & touch.startScreenPosition.x > Screen.width / 2f)
                        {
                            lookInputToAdd += touch.delta;
                        }
                    }
                }
            lookInput += lookInputToAdd;
            }
#endif

            UpdateLocomotion();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", isGrounded);

            if (attributes.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        public override Vector3 GetVelocity() { return forceAccumulated; }

        Vector3 forceAccumulated;
        public override void AddForce(Vector3 force)
        {
            if (!attributes.IsGrabbed() & !attributes.AnimationHandler.IsGrabAttacking()) { forceAccumulated += force; }
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

        public static readonly Vector3 targetSystemOffset = new Vector3(0, 1, 0);

        private void RefreshStatus()
        {
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

        private float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        private float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (attributes.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }

        private bool autoAim;
        RaycastHit[] cameraHits = new RaycastHit[10];
        private void UpdateLocomotion()
        {
            if (Vector3.Distance(transform.position, movementPrediction.CurrentPosition) > movementPrediction.playerObjectTeleportThreshold)
            {
                //Debug.Log("Teleporting player: " + OwnerClientId);
                transform.position = movementPrediction.CurrentPosition;
            }
            else
            {
                Vector3 newPosition;
                Vector2 horizontalPosition;
                if (attributes.AnimationHandler.ShouldApplyRootMotion())
                {
                    horizontalPosition = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z),
                        new Vector2(movementPrediction.CurrentPosition.x, movementPrediction.CurrentPosition.z),
                        attributes.AnimationHandler.ApplyLocalRootMotion().magnitude * GetRootMotionSpeed());
                }
                else
                {
                    horizontalPosition = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z),
                        new Vector2(movementPrediction.CurrentPosition.x, movementPrediction.CurrentPosition.z),
                        Time.deltaTime * GetRunSpeed());
                }
                newPosition.x = horizontalPosition.x;
                newPosition.z = horizontalPosition.y;
                newPosition.y = Mathf.MoveTowards(transform.position.y, movementPrediction.CurrentPosition.y, Time.deltaTime * -Physics.gravity.y);

                if (attributes.ShouldShake())
                {
                    newPosition += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount);
                }

                transform.position = newPosition;
            }

            if (weaponHandler.CurrentActionClip != null)
            {
                if (attributes.ShouldPlayHitStop())
                {
                    attributes.AnimationHandler.Animator.speed = 0;
                }
                else
                {
                    if (attributes.IsGrabbed())
                    {
                        CombatAgent grabAssailant = attributes.GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (grabAssailant.AnimationHandler)
                            {
                                attributes.AnimationHandler.Animator.speed = grabAssailant.AnimationHandler.Animator.speed;
                            }
                        }
                    }
                    else
                    {
                        attributes.AnimationHandler.Animator.speed = GetAnimatorSpeed();
                    }
                }
            }

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);

            if (autoAim)
            {
                if (weaponHandler.CurrentActionClip.useRotationalTargetingSystem & cameraController & !weaponHandler.CurrentActionClip.mustBeAiming)
                {
                    if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | attributes.AnimationHandler.IsLunging())
                    {
                        ExtDebug.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, Color.yellow, Time.deltaTime);
                        int cameraHitsCount = Physics.BoxCastNonAlloc(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset,
                            ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward.normalized, cameraHits,
                            cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance,
                            LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                        
                        List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                        for (int i = 0; i < cameraHitsCount; i++)
                        {
                            if (cameraHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                            {
                                if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position + targetSystemOffset - cameraController.CameraPositionClone.transform.position, Vector3.up);
                                    angleList.Add((networkCollider,
                                        Mathf.Abs(targetRot.eulerAngles.y - cameraController.CameraPositionClone.transform.eulerAngles.y) + Mathf.Abs((targetRot.eulerAngles.x < 180 ? targetRot.eulerAngles.x : targetRot.eulerAngles.x - 360) - (cameraController.CameraPositionClone.transform.eulerAngles.x < 180 ? cameraController.CameraPositionClone.transform.eulerAngles.x : cameraController.CameraPositionClone.transform.eulerAngles.x - 360)),
                                        cameraHits[i]));
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
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (attributes.StatusAgent.IsFeared() ? -1 : 1);
        }

        public void OnDodge()
        {
            if (attributes.AnimationHandler.IsReloading()) { return; }
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            attributes.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }

        private string[] interactableRaycastLayers = new string[]
        {
            "Default",
            "NetworkPrediction",
            "Projectile",
            "ProjectileCollider"
        };

        void OnInteract()
        {
            int interactableHitsCount = Physics.RaycastNonAlloc(mainCamera.transform.position, mainCamera.transform.forward.normalized,
                interactableHits, 15, LayerMask.GetMask(interactableRaycastLayers), QueryTriggerInteraction.Ignore);

            for (int i = 0; i < interactableHitsCount; i++)
            {
                if (interactableHits[i].transform.root.TryGetComponent(out NetworkInteractable networkInteractable))
                {
                    networkInteractable.Interact(gameObject);
                    break;
                }
            }
        }

        public CombatAgent CameraFollowTarget { get; private set; }
        public void OnIncrementFollowPlayer()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
                if (CameraFollowTarget == null)
                {
                    if (spectatableAttributesList.Count > 0) { CameraFollowTarget = spectatableAttributesList[0]; }
                }
                else
                {
                    int index = spectatableAttributesList.IndexOf(CameraFollowTarget);
                    index += 1;
                    if (index >= 0 & index < spectatableAttributesList.Count)
                    {
                        CameraFollowTarget = spectatableAttributesList[index];
                    }
                    else if (spectatableAttributesList.Count > 0)
                    {
                        CameraFollowTarget = spectatableAttributesList[0];
                    }
                }
            }
        }

        public void OnDecrementFollowPlayer()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
                if (CameraFollowTarget == null)
                {
                    if (spectatableAttributesList.Count > 0) { CameraFollowTarget = spectatableAttributesList[^1]; }
                }
                else
                {
                    int index = spectatableAttributesList.IndexOf(CameraFollowTarget);
                    index -= 1;
                    if (index >= 0 & index < spectatableAttributesList.Count)
                    {
                        CameraFollowTarget = spectatableAttributesList[index];
                    }
                    else if (spectatableAttributesList.Count > 0)
                    {
                        CameraFollowTarget = spectatableAttributesList[^1];
                    }
                }
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) { return; }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(movementPrediction.CurrentPosition, 0.3f);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Vector3.MoveTowards(transform.position, movementPrediction.CurrentPosition, Time.deltaTime), 0.3f);
        }
    }
}

