using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.MovementHandlers;
using Vi.ProceduralAnimations;
using Unity.Netcode.Components;
using static Vi.Player.PlayerMovementHandler;

namespace Vi.Player
{
    public class PlayerMovementHandler : PhysicsMovementHandler
    {
        [Header("Player Movement Handler")]
        [SerializeField] private CameraController cameraController;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetOrientation(newPosition, newRotation);
            if (IsSpawned)
            {
                SetRotationClientRpc(newRotation);
            }
        }

        [Rpc(SendTo.Owner)] private void SetRotationClientRpc(Quaternion newRotation) { cameraController.SetRotation(newRotation.eulerAngles.x, newRotation.eulerAngles.y); }

        public bool IsCameraAnimating() { return cameraController.IsAnimating; }

        public Transform TargetToLockOn { get; private set; }
        public void LockOnTarget(Transform target) { TargetToLockOn = target; }

        public override void Flinch(Vector2 flinchAmount)
        {
            if (!IsServer) { Debug.LogError("PlayerMovementHandler.Flinch() should only be called on the server!"); return; }
            FlinchRpc(flinchAmount);
        }

        [Rpc(SendTo.Owner)] private void FlinchRpc(Vector2 flinchAmount) { cameraController.AddRotation(flinchAmount.x, flinchAmount.y); }

        public struct InputPayload : INetworkSerializable, System.IEquatable<InputPayload>
        {
            public int tick;
            public Vector2 moveInput;
            public Quaternion rotation;

            public InputPayload(int tick, Vector2 moveInput, Quaternion rotation)
            {
                this.tick = tick;
                this.moveInput = moveInput;
                this.rotation = rotation;
            }

            public bool Equals(InputPayload other)
            {
                return tick == other.tick & moveInput == other.moveInput & rotation == other.rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref rotation);
            }
        }

        public struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector2 moveInput;
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
            public bool usedRootMotion;
            
            public StatePayload(InputPayload inputPayload, Rigidbody Rigidbody, Quaternion rotation, bool usedRootMotion)
            {
                tick = inputPayload.tick;
                moveInput = inputPayload.moveInput;
                position = Rigidbody.position;
                velocity = Rigidbody.linearVelocity;
                this.rotation = rotation;
                this.usedRootMotion = usedRootMotion;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
                serializer.SerializeValue(ref usedRootMotion);
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                Rigidbody.Sleep();
                return;
            }

            if (IsOwner)
            {
                Vector2 moveInput;
                if (combatAgent.AnimationHandler.WaitingForActionClipToPlay)
                {
                    moveInput = Vector2.zero;
                }
                else if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
                {
                    moveInput = Vector2.zero;
                }
                else if (!CanMove())
                {
                    moveInput = Vector2.zero;
                }
                else if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
                {
                    moveInput = Vector2.zero;
                }
                else if (combatAgent.StatusAgent.IsRooted())
                {
                    moveInput = Vector2.zero;
                }
                else
                {
                    moveInput = GetPlayerMoveInput();
                }

                syncedMoveInput.Value = moveInput;
                InputPayload inputPayload = new InputPayload(movementTick, moveInput, EvaluateRotation());
                inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;
                movementTick++;

                StatePayload statePayload = Move(inputPayload,
                    combatAgent.AnimationHandler.ApplyRootMotion(),
                    combatAgent.AnimationHandler.ShouldApplyRootMotion());

                stateBuffer[inputPayload.tick % BUFFER_SIZE] = statePayload;

                ownerPosition.Value = statePayload.position;
            }
            else if (IsServer)
            {
                if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
                {
                    Rigidbody.isKinematic = false;
                    Move(new InputPayload(0, Vector2.zero, transform.rotation),
                        Quaternion.Inverse((combatAgent.ShouldApplyAilmentRotation() ? combatAgent.GetAilmentRotation() : transform.rotation)) * (ownerPosition.Value - Rigidbody.position) / Time.fixedDeltaTime,
                        true);
                }
                else
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(ownerPosition.Value);
                }
            }
            else
            {
                Rigidbody.isKinematic = true;
                Rigidbody.MovePosition(transform.position);
            }
        }

        private NetworkVariable<Vector3> serverRotation = new NetworkVariable<Vector3>();

        protected override void OnDisable()
        {
            base.OnDisable();
            movementTick = default;
            TargetToLockOn = default;
            CameraFollowTarget = default;
            joysticks = new UIDeadZoneElement[0];
        }

        private int movementTick;
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        private StatePayload Move(InputPayload inputPayload, Vector3 rootMotion, bool shouldApplyRootMotion)
        {
            if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                Rigidbody.Sleep();
                return new StatePayload(inputPayload, Rigidbody, inputPayload.rotation, false);
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed & !combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = false;
                return new StatePayload(inputPayload, Rigidbody, inputPayload.rotation, false);
            }

            Vector2 moveInput = inputPayload.moveInput;
            Quaternion newRotation = combatAgent.ShouldApplyAilmentRotation() ? combatAgent.GetAilmentRotation() : inputPayload.rotation;

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = true;
                //if (!IsServer) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return new StatePayload(inputPayload, Rigidbody, newRotation, false);
            }
            else if (combatAgent.IsGrabbed & combatAgent.GetAilment() == ActionClip.Ailment.None)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(grabAssailant.MovementHandler.GetPosition() + (grabAssailant.MovementHandler.GetRotation() * Vector3.forward));
                    return new StatePayload(inputPayload, Rigidbody, newRotation, false);
                }
            }
            else if (combatAgent.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (combatAgent.IsPulled)
            {
                CombatAgent pullAssailant = combatAgent.GetPullAssailant();
                if (pullAssailant)
                {
                    movement = pullAssailant.MovementHandler.GetPosition() - GetPosition();
                }
            }
            else if (shouldApplyRootMotion)
            {
                movement = newRotation * rootMotion * GetRootMotionSpeed();
            }
            else
            {
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = combatAgent.StatusAgent.IsRooted() | combatAgent.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            Rigidbody.isKinematic = false;

            if (combatAgent.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = Rigidbody.position + newRotation * stairRaycastingStartOffset;
            startPos.y += stairStepHeight;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }
#if UNITY_EDITOR
                if (drawCasts) Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
                startPos.y += stairStepHeight;
                stairMovement += stairStepHeight;

                if (stairMovement > maxStairStepHeight)
                {
                    stairMovement = 0;
                    break;
                }
            }

            if (Physics.CapsuleCast(Rigidbody.position, Rigidbody.position + BodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
            {
                bool collidersIgnoreEachOther = false;
                foreach (Collider c in combatAgent.NetworkCollider.Colliders)
                {
                    if (Physics.GetIgnoreCollision(playerHit.collider, c))
                    {
                        collidersIgnoreEachOther = true;
                        break;
                    }
                }

                if (!collidersIgnoreEachOther)
                {
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - Rigidbody.position, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            bool evaluateForce = true;
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity & shouldApplyRootMotion)
            {
                Rigidbody.AddForce(movement - Rigidbody.linearVelocity, ForceMode.VelocityChange);
                evaluateForce = false;
            }

            if (evaluateForce)
            {
                if (IsGrounded())
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.linearVelocity.x, 0, Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.linearVelocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.linearVelocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while airborne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.linearVelocity.x, 0, -Rigidbody.linearVelocity.z), airborneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement * stairStepForceMultiplier, 0), ForceMode.VelocityChange);
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
            return new StatePayload(inputPayload, Rigidbody, newRotation, shouldApplyRootMotion);
        }

        private const float bodyRadius = 0.5f;

        private Quaternion EvaluateRotation(bool sendOwnerRotation = false)
        {
            Quaternion rot = transform.rotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (combatAgent.ShouldApplyAilmentRotation())
                    rot = combatAgent.GetAilmentRotation();
                else if (combatAgent.IsGrabbing)
                    return rot;
                else if (combatAgent.IsGrabbed)
                {
                    CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                    if (grabAssailant)
                    {
                        Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                        rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                        Quaternion.LookRotation(rel, Vector3.up);
                    }
                }
                else if (combatAgent.AnimationHandler.ShouldApplyRootMotion() & sendOwnerRotation)
                {
                    rot = Quaternion.Slerp(transform.rotation, Quaternion.Euler(serverRotation.Value), Time.deltaTime * CameraController.orbitSpeed);
                    if (!combatAgent.ShouldPlayHitStop())
                    {
                        ownerRotationEulerAngles.Value = Quaternion.LookRotation(camDirection).eulerAngles;
                    }
                }
                else if (!combatAgent.ShouldPlayHitStop())
                    rot = Quaternion.LookRotation(camDirection);
            }
            else if (IsServer)
            {
                return Quaternion.Euler(ownerRotationEulerAngles.Value);
            }
            return rot;
        }

        private void UpdateTransform()
        {
            if (!IsSpawned) { return; }
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }
            
            transform.position = Rigidbody.transform.position;
            transform.rotation = EvaluateRotation();
            if (IsServer) { serverRotation.Value = transform.eulerAngles; }
            if (IsOwner) { ownerRotationEulerAngles.Value = EvaluateRotation(true).eulerAngles; }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkTransform.SyncPositionX = !IsOwner;
            networkTransform.SyncPositionY = !IsOwner;
            networkTransform.SyncPositionZ = !IsOwner;
            networkTransform.SyncRotAngleX = !IsOwner;
            networkTransform.SyncRotAngleY = !IsOwner;
            networkTransform.SyncRotAngleZ = !IsOwner;

            networkTransform.Interpolate = !IsServer & !IsOwner;

            if (IsLocalPlayer)
            {
                cameraController.gameObject.tag = "MainCamera";
                cameraController.gameObject.SetActive(true);
                cameraController.gameObject.AddComponent<AudioListener>();

                cameraController.SetActive(true);

                cameraController.SetOrbitalCameraState(false);

                playerInput.enabled = true;
                string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);

                actionMapHandler.enabled = true;

                ownerPosition.Value = transform.position;
                ownerRotationEulerAngles.Value = transform.eulerAngles;
                Rigidbody.position = ownerPosition.Value;
            }
            else
            {
                cameraController.gameObject.SetActive(false);

                cameraController.SetActive(false);

                playerInput.enabled = false;

                actionMapHandler.enabled = false;
            }
            Rigidbody.isKinematic = !IsOwner;
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            cameraController.gameObject.SetActive(false);
            if (cameraController.gameObject.TryGetComponent(out AudioListener audioListener))
            {
                Destroy(audioListener);
            }

            cameraController.SetActive(false);

            playerInput.enabled = false;
            actionMapHandler.enabled = false;
            cameraController.gameObject.tag = "Untagged";
        }

        protected override void OnReturnToPool()
        {
            base.OnReturnToPool();
            cameraController.transform.SetParent(transform);
            cameraController.transform.localPosition = new Vector3(0.34f, 1.73f, -2.49f);
            cameraController.transform.localRotation = Quaternion.identity;
        }

        private const int BUFFER_SIZE = 1024;

        private StatePayload[] stateBuffer;
        private InputPayload[] inputBuffer;
        private StatePayload lastProcessedState;
        private Queue<InputPayload> serverInputQueue;

        private NetworkVariable<Vector2> syncedMoveInput = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> ownerPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> ownerRotationEulerAngles = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private ActionMapHandler actionMapHandler;
        protected override void Awake()
        {
            base.Awake();
            Rigidbody.isKinematic = true;

            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new InputPayload[BUFFER_SIZE];
            serverInputQueue = new Queue<InputPayload>();

            actionMapHandler = GetComponent<ActionMapHandler>();
        }

        private void Start()
        {
            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room"))
            {
                cameraController.PlayAnimation("TutorialIntro");
            }
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];
        RaycastHit[] interactableHits = new RaycastHit[10];
        protected override void Update()
        {
            base.Update();

            FindMainCamera();

            if (!IsSpawned) { return; }

#if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            if (playerInput)
            {
                if (IsLocalPlayer)
                {
                    if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
                    {
                        if (playerInput.currentActionMap != null)
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
                                        if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)joystick.transform.parent, touch.startScreenPosition, joystick.RootCanvas.worldCamera))
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
                            lookInput += lookInputToAdd * (combatAgent.StatusAgent.IsFeared() ? -1 : 1);
                        }
                    }
                }
            }
#endif
            UpdateTransform();
            if (IsLocalPlayer) { cameraController.UpdateCamera(); }
            AutoAim();
            SetAnimationMoveInput(IsOwner ? GetPlayerMoveInput() : syncedMoveInput.Value);

            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

# if UNITY_EDITOR
        private bool drawCasts;
# endif

        private bool autoAim;
        RaycastHit[] cameraHits = new RaycastHit[10];
        private void AutoAim()
        {
            if (!autoAim) { return; }
            if (!IsOwner) { return; }
            if (weaponHandler.CurrentActionClip)
            {
                if (weaponHandler.CurrentActionClip.useRotationalTargetingSystem & !weaponHandler.CurrentActionClip.mustBeAiming)
                {
                    if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | combatAgent.AnimationHandler.IsLunging())
                    {
#if UNITY_EDITOR
                        if (drawCasts) DebugExtensions.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, Color.yellow, Time.deltaTime);
#endif
                        int cameraHitsCount = Physics.BoxCastNonAlloc(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset,
                            ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward.normalized, cameraHits,
                            cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance,
                            LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

                        List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                        for (int i = 0; i < cameraHitsCount; i++)
                        {
                            if (cameraHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                            {
                                if (PlayerDataManager.Singleton.CanHit(combatAgent, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible)
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

        public static readonly Vector3 targetSystemOffset = new Vector3(0, 1, 0);

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (combatAgent.StatusAgent.IsFeared() ? -1 : 1);
        }

        public void OnDodge()
        {
            if (combatAgent.AnimationHandler.IsReloading()) { return; }
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            combatAgent.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
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
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent).FindAll(item => (!PlayerDataManager.Singleton.CanHit(combatAgent, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
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
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent).FindAll(item => (!PlayerDataManager.Singleton.CanHit(combatAgent, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
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

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            if (!Application.isPlaying) { return; }

            //Gizmos.color = Color.blue;
            //Gizmos.DrawSphere(latestServerState.Value.position, 0.5f);

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(Rigidbody.position, 0.3f);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawWireSphere(Rigidbody.position, isGroundedSphereCheckRadius);
        }
    }
}

