using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;

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

        private float originalMass;
        public override void OnIsGrabbedChange(bool prev, bool current)
        {
            movementPredictionRigidbody.mass = current ? Mathf.Infinity : originalMass;
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
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    newRotation = attributes.GetAilmentRotation();
                else if (animationHandler.IsGrabAttacking())
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

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            RaycastHit[] allHits = Physics.SphereCastAll(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity,
                gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
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
                    Physics.gravity, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
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
                if (attributes.IsRooted() & attributes.GetAilment() != ActionClip.Ailment.Knockup & attributes.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else if (weaponHandler.CurrentActionClip.limitAttackMotionBasedOnTarget & (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking) | animationHandler.IsLunging())
                {
                    movement = rootMotion;
                    ExtDebug.DrawBoxCastBox(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, movementPrediction.CurrentRotation * Vector3.forward, movementPrediction.CurrentRotation, ActionClip.boxCastDistance, Color.blue, 1f / NetworkManager.NetworkTickSystem.TickRate);
                    allHits = Physics.BoxCastAll(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, movementPrediction.CurrentRotation * Vector3.forward, movementPrediction.CurrentRotation, ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                    List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                    foreach (RaycastHit hit in allHits)
                    {
                        if (hit.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.Attributes) & !networkCollider.Attributes.IsInvincible())
                            {
                                Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - movementPrediction.CurrentPosition, Vector3.up);
                                angleList.Add((networkCollider,
                                    Mathf.Abs(targetRot.eulerAngles.y - movementPrediction.CurrentRotation.eulerAngles.y),
                                    hit));
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
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded ? Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                movement = attributes.IsRooted() | animationHandler.IsReloading() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }
            
            if (animationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

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

                if (Application.isEditor) { Debug.DrawRay(startPos, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate); }
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
                Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - movementPrediction.CurrentPosition, Vector3.up);
                float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                if (angle > 180) { angle -= 360; }

                if (angle > -20 & angle < 20)
                {
                    movement = Vector3.zero;
                }
            }

            movement += forceAccumulated;
            forceAccumulated = Vector3.zero;

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

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraController.gameObject.AddComponent<AudioListener>();
                cameraController.GetComponent<Camera>().enabled = true;

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
            originalMass = movementPredictionRigidbody.mass;
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

            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Map"))
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
        private new void Update()
        {
            base.Update();
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            FindMainCamera();

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
                                RaycastHit[] allHits = Physics.RaycastAll(mainCamera.ScreenPointToRay(touch.screenPosition), 10, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
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
            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetBool("IsGrounded", isGrounded);

            if (attributes.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        Vector3 forceAccumulated;
        public override void AddForce(Vector3 force)
        {
            if (!attributes.IsGrabbed() & !animationHandler.IsGrabAttacking()) { forceAccumulated += force * Time.fixedDeltaTime; }
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
            autoAim = bool.Parse(FasterPlayerPrefs.Singleton.GetString("AutoAim"));
        }

        private bool autoAim;
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
                    animationHandler.Animator.speed = (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (animationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
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
                    if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | animationHandler.IsLunging())
                    {
                        ExtDebug.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, Color.yellow, Time.deltaTime);
                        RaycastHit[] allHits = Physics.BoxCastAll(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                        List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                        foreach (RaycastHit hit in allHits)
                        {
                            if (hit.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                            {
                                if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.Attributes) & !networkCollider.Attributes.IsInvincible())
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
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (attributes.IsFeared() ? -1 : 1);
        }

        public void OnDodge()
        {
            if (animationHandler.IsReloading()) { return; }
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (attributes.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            animationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }

        void OnInteract()
        {
            RaycastHit[] allHits = Physics.RaycastAll(mainCamera.transform.position, mainCamera.transform.forward, 15, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
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

        public Attributes CameraFollowTarget { get; private set; }
        public void OnIncrementFollowPlayer()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                List<Attributes> spectatableAttributesList = PlayerDataManager.Singleton.GetActivePlayerObjects(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
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
                List<Attributes> spectatableAttributesList = PlayerDataManager.Singleton.GetActivePlayerObjects(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
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

        //private void OnDrawGizmos()
        //{
        //    if (!Application.isPlaying) { return; }
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawWireSphere(movementPrediction.CurrentPosition + movementPrediction.CurrentRotation * gravitySphereCastPositionOffset, gravitySphereCastRadius);
        //}
    }
}

