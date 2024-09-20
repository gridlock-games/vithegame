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

namespace Vi.Player
{
    public class PlayerMovementHandler : PhysicsMovementHandler
    {
        [Header("Player Movement Handler")]
        [SerializeField] private CameraController cameraController;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsServer) { Debug.LogError("PlayerMovementHandler.SetOrientation() should only be called on the server!"); return; }

            Rigidbody.position = newPosition;
            Rigidbody.velocity = Vector3.zero;
            transform.position = newPosition;

            SetRotationClientRpc(newRotation);
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
                velocity = Rigidbody.velocity;
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

        protected override bool IsGrounded()
        {
            if (latestServerState.Value.tick == 0)
            {
                return true;
            }
            else
            {
                return base.IsGrounded();
            }
        }

        private const float serverReconciliationThreshold = 0.01f;
        private float lastServerReconciliationTime = Mathf.NegativeInfinity;
        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState.Value;

            if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return;
            }
            if (!CanMove())
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return;
            }
            if (latestServerState.Value.usedRootMotion)
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return;
            }

            int serverStateBufferIndex = latestServerState.Value.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex].position);

            if (positionError > serverReconciliationThreshold)
            {
                //Debug.Log(OwnerClientId + " Position Error: " + positionError);
                //Debug.Log(positionError + " " + (Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex + 1].position) < serverReconciliationThreshold));
                lastServerReconciliationTime = Time.time;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState.Value;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                Physics.simulationMode = SimulationMode.Script;
                Rigidbody.position = latestServerState.Value.position;
                Rigidbody.velocity = latestServerState.Value.velocity;
                NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody);

                int tickToProcess = latestServerState.Value.tick + 1;
                while (tickToProcess < movementTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = Move(inputBuffer[bufferIndex]);
                    NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody);

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
                Physics.simulationMode = SimulationMode.FixedUpdate;
            }
        }

        public override void OnServerActionClipPlayed()
        {
            // Empty the input queue and simulate the player up. This prevents the player from jumping backwards in time because the server simulation runs behind the owner simulation
            while (serverInputQueue.TryDequeue(out InputPayload inputPayload))
            {
                StatePayload statePayload = Move(inputPayload);
                stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                latestServerState.Value = statePayload;

                if (serverInputQueue.Count > 0) { NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody); }
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner & !IsServer)
            {
                if (latestServerState.Value.tick > 0)
                {
                    // Sync position here with latest server state
                    Rigidbody.MovePosition(latestServerState.Value.position);
                }
            }

            if (!IsClient)
            {
                if (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                {
                    StatePayload statePayload = Move(inputPayload);
                    stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                    latestServerState.Value = statePayload;
                }
            }

            if (IsOwner)
            {
                if (latestServerState.Value.tick > 0)
                {
                    if (!latestServerState.Equals(default(StatePayload)) &&
                        (lastProcessedState.Equals(default(StatePayload)) ||
                        !latestServerState.Equals(lastProcessedState)))
                    {
                        HandleServerReconciliation();
                    }
                }

                Vector2 moveInput;
                if (combatAgent.AnimationHandler.WaitingForActionClipToPlay)
                {
                    moveInput = Vector2.zero;
                }
                else if (latestServerState.Value.usedRootMotion)
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

                InputPayload inputPayload = new InputPayload(movementTick, moveInput, EvaluateRotation());
                if (inputPayload.tick % BUFFER_SIZE < inputBuffer.Count)
                    inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;
                else
                    inputBuffer.Add(inputPayload);
                movementTick++;

                StatePayload statePayload = Move(inputPayload);
                stateBuffer[inputPayload.tick % BUFFER_SIZE] = statePayload;

                if (IsServer) { latestServerState.Value = statePayload; }
            }

            if (latestServerState.Value.tick == 0 & !IsServer)
            {
                Rigidbody.Sleep();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            movementTick = default;
            lastEvaluatedServerRootMotionTick = default;
            TargetToLockOn = default;
            CameraFollowTarget = default;
        }

        private int movementTick;
        private int lastEvaluatedServerRootMotionTick;
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        private StatePayload Move(InputPayload inputPayload)
        {
            Vector3 rootMotion = combatAgent.AnimationHandler.ApplyRootMotion();
            if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                if (IsServer)
                {
                    Rigidbody.velocity = Vector3.zero;
                }
                else
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(latestServerState.Value.position);
                }
                return new StatePayload(inputPayload, Rigidbody, inputPayload.rotation, false);
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed() & !combatAgent.IsGrabbing())
            {
                if (IsServer)
                {
                    Rigidbody.isKinematic = false;
                }
                else
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(latestServerState.Value.position);
                }
                return new StatePayload(inputPayload, Rigidbody, inputPayload.rotation, false);
            }

            Vector2 moveInput = inputPayload.moveInput;
            Quaternion newRotation = inputPayload.rotation;

            // Apply movement
            bool shouldApplyRootMotion = combatAgent.AnimationHandler.ShouldApplyRootMotion();
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing())
            {
                Rigidbody.isKinematic = true;
                //if (!IsServer) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return new StatePayload(inputPayload, Rigidbody, newRotation, false);
            }
            else if (combatAgent.IsGrabbed() & combatAgent.GetAilment() == ActionClip.Ailment.None)
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
            else if (combatAgent.IsPulled())
            {
                CombatAgent pullAssailant = combatAgent.GetPullAssailant();
                if (pullAssailant)
                {
                    movement = pullAssailant.MovementHandler.GetPosition() - GetPosition();
                }
            }
            else if (shouldApplyRootMotion)
            {
                if (IsServer)
                {
                    if (combatAgent.StatusAgent.IsRooted() & combatAgent.GetAilment() != ActionClip.Ailment.Knockup & combatAgent.GetAilment() != ActionClip.Ailment.Knockdown)
                    {
                        movement = Vector3.zero;
                    }
                    else if (weaponHandler.CurrentActionClip.limitAttackMotionBasedOnTarget & (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking) | combatAgent.AnimationHandler.IsLunging())
                    {
                        movement = newRotation * rootMotion * GetRootMotionSpeed();
#if UNITY_EDITOR
                        DebugExtensions.DrawBoxCastBox(GetPosition() + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, newRotation * Vector3.forward, newRotation, ActionClip.boxCastDistance, Color.blue, GetTickRateDeltaTime());
#endif
                        int rootMotionHitCount = Physics.BoxCastNonAlloc(GetPosition() + ActionClip.boxCastOriginPositionOffset,
                            ActionClip.boxCastHalfExtents, (newRotation * Vector3.forward).normalized, rootMotionHits,
                            newRotation, ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

                        List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();

                        for (int i = 0; i < rootMotionHitCount; i++)
                        {
                            if (rootMotionHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                            {
                                if (PlayerDataManager.Singleton.CanHit(combatAgent, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - GetPosition(), Vector3.up);
                                    angleList.Add((networkCollider,
                                        Mathf.Abs(targetRot.eulerAngles.y - newRotation.eulerAngles.y),
                                        rootMotionHits[i]));
                                }
                            }
                        }

                        angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                        foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - GetPosition(), Vector3.up);
                            if (angle < ActionClip.maximumRootMotionLimitRotationAngle)
                            {
                                movement = Vector3.ClampMagnitude(movement, hit.distance / Time.fixedDeltaTime);
                                break;
                            }
                        }
                    }
                    else
                    {
                        movement = newRotation * rootMotion * GetRootMotionSpeed();
                    }
                }
                else if (latestServerState.Value.usedRootMotion)
                {
                    if (latestServerState.Value.tick == lastEvaluatedServerRootMotionTick)
                    {
                        movement = newRotation * rootMotion * GetRootMotionSpeed();
                    }
                    else
                    {
                        movement = (latestServerState.Value.position - GetPosition()) / Time.fixedDeltaTime;
                    }
                    lastEvaluatedServerRootMotionTick = latestServerState.Value.tick;
                }
            }
            else if (combatAgent.AnimationHandler.IsAtRest())
            {
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = combatAgent.StatusAgent.IsRooted() | combatAgent.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            Rigidbody.isKinematic = false;

            if (combatAgent.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = Rigidbody.position;
            startPos.y += stairStepHeight;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }
#if UNITY_EDITOR
                Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
                startPos.y += stairStepHeight;
                stairMovement += stairStepHeight;

                if (stairMovement > maxStairStepHeight)
                {
                    stairMovement = 0;
                    break;
                }
            }

            if (Physics.CapsuleCast(Rigidbody.position, Rigidbody.position + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
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
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity)
            {
                if (combatAgent.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
                {
                    Rigidbody.AddForce(movement - Rigidbody.velocity, ForceMode.VelocityChange);
                    evaluateForce = false;
                }
            }

            if (evaluateForce)
            {
                if (IsGrounded())
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.velocity.x, 0, Rigidbody.velocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.velocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.velocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while aiRigidbodyorne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.velocity.x, 0, -Rigidbody.velocity.z), airborneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement, 0), ForceMode.VelocityChange);
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
            return new StatePayload(inputPayload, Rigidbody, newRotation, shouldApplyRootMotion);
        }

        private Quaternion EvaluateRotation()
        {
            Quaternion rot = transform.rotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (combatAgent.ShouldApplyAilmentRotation())
                    rot = combatAgent.GetAilmentRotation();
                else if (combatAgent.IsGrabbing())
                    return rot;
                else if (combatAgent.IsGrabbed())
                {
                    CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                    if (grabAssailant)
                    {
                        Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                        rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                        Quaternion.LookRotation(rel, Vector3.up);
                    }
                }
                else if (!combatAgent.ShouldPlayHitStop())
                    rot = Quaternion.LookRotation(camDirection);
            }
            else
            {
                rot = Quaternion.Slerp(transform.rotation, latestServerState.Value.rotation, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * CameraController.orbitSpeed);
            }
            return rot;
        }

        private const float serverReconciliationLerpDuration = 1;
        private const float serverReconciliationTeleportThreshold = 2;
        private const float serverReconciliationLerpSpeed = 8;

        private void UpdateTransform()
        {
            if (!IsSpawned) { return; }
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }

            if (Time.time - lastServerReconciliationTime < serverReconciliationLerpDuration & !weaponHandler.IsAiming())
            {
                float dist = Vector3.Distance(transform.position, Rigidbody.transform.position);
                if (dist > serverReconciliationTeleportThreshold)
                {
                    transform.position = Rigidbody.transform.position;
                    lastServerReconciliationTime = Mathf.NegativeInfinity;
                }
                else if (dist < 0.01f)
                {
                    transform.position = Rigidbody.transform.position;
                    lastServerReconciliationTime = Mathf.NegativeInfinity;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, Rigidbody.transform.position, Time.deltaTime * serverReconciliationLerpSpeed);
                }
            }
            else
            {
                transform.position = Rigidbody.transform.position;
            }

            if (combatAgent.ShouldShake()) { transform.position += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount); }

            transform.rotation = EvaluateRotation();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsLocalPlayer)
            {
                cameraController.gameObject.tag = "MainCamera";
                cameraController.gameObject.SetActive(true);
                cameraController.gameObject.AddComponent<AudioListener>();
                cameraController.Camera.enabled = true;

                playerInput.enabled = true;
                string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);

                actionMapHandler.enabled = true;
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
            else
            {
                cameraController.gameObject.SetActive(false);
                cameraController.Camera.enabled = false;
                playerInput.enabled = false;

                actionMapHandler.enabled = false;
            }

            if (!IsClient)
            {
                inputBuffer.OnListChanged += OnInputBufferChanged;
            }

            if (IsServer)
            {
                latestServerState.Value = new StatePayload(new InputPayload(0, Vector2.zero, transform.rotation), Rigidbody, transform.rotation, false);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
                Cursor.lockState = CursorLockMode.None;
            }

            cameraController.gameObject.SetActive(false);
            if (cameraController.gameObject.TryGetComponent(out AudioListener audioListener))
            {
                Destroy(audioListener);
            }
            cameraController.Camera.enabled = false;

            playerInput.enabled = false;
            actionMapHandler.enabled = false;
            cameraController.gameObject.tag = "Untagged";

            if (!IsClient)
            {
                inputBuffer.OnListChanged -= OnInputBufferChanged;
            }
        }

        private void OnInputBufferChanged(NetworkListEvent<InputPayload> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Value | networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Add)
            {
                serverInputQueue.Enqueue(networkListEvent.Value);
            }
            else
            {
                Debug.LogError("We shouldn't be receiving an event for a network list event type of: " + networkListEvent.Type);
            }
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
        private NetworkList<InputPayload> inputBuffer;
        private NetworkVariable<StatePayload> latestServerState = new NetworkVariable<StatePayload>();
        private StatePayload lastProcessedState;
        private Queue<InputPayload> serverInputQueue;

        private ActionMapHandler actionMapHandler;
        protected override void Awake()
        {
            base.Awake();
            Rigidbody.isKinematic = true;

            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new NetworkList<InputPayload>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
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
            if (IsLocalPlayer & UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled & playerInput.currentActionMap != null)
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
            UpdateTransform();
            if (IsLocalPlayer) { cameraController.UpdateCamera(); }
            AutoAim();
            SetAnimationMoveInput(IsOwner ? GetPlayerMoveInput() : latestServerState.Value.moveInput);

            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

        private bool autoAim;
        RaycastHit[] cameraHits = new RaycastHit[10];
        private void AutoAim()
        {
            if (!autoAim) { return; }
            if (!IsOwner) { return; }
            if (weaponHandler.CurrentActionClip.useRotationalTargetingSystem & !weaponHandler.CurrentActionClip.mustBeAiming)
            {
                if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | combatAgent.AnimationHandler.IsLunging())
                {
                    DebugExtensions.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, Color.yellow, Time.deltaTime);
                    int cameraHitsCount = Physics.BoxCastNonAlloc(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset,
                        ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward.normalized, cameraHits,
                        cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance,
                        LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

                    List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                    for (int i = 0; i < cameraHitsCount; i++)
                    {
                        if (cameraHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(combatAgent, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
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

        protected void OnDrawGizmos()
        {
            if (!Application.isPlaying) { return; }

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(latestServerState.Value.position, 0.5f);

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(Rigidbody.position, 0.3f);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawWireSphere(Rigidbody.position, isGroundedSphereCheckRadius);
        }
    }
}

