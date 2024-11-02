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
using System.Runtime.CompilerServices;

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
        private Vector3 HandleServerReconciliation()
        {
            lastProcessedState = latestServerState.Value;

            if (isServerAuthoritative)
            {
                return Vector3.zero;
            }

            if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return Vector3.zero;
            }

            if (!CanMove())
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                return Vector3.zero;
            }

            int serverStateBufferIndex = latestServerState.Value.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex].position);

            if (stateBuffer[serverStateBufferIndex].usedRootMotion | latestServerState.Value.usedRootMotion)
            {
                return Vector3.zero;
            }

            if (positionError > serverReconciliationThreshold)
            {
                Debug.Log(latestServerState.Value.tick + " Position Error: " + positionError);
                lastServerReconciliationTime = Time.time;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState.Value;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                Physics.simulationMode = SimulationMode.Script;
                Rigidbody.position = latestServerState.Value.position;
                if (!Rigidbody.isKinematic) { Rigidbody.linearVelocity = latestServerState.Value.velocity; }
                NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody, false);

                int tickToProcess = latestServerState.Value.tick + 1;
                while (tickToProcess < movementTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = Move(inputBuffer[bufferIndex], Vector3.zero, stateBuffer[bufferIndex].usedRootMotion);
                    NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody, false);

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
                Physics.simulationMode = SimulationMode.FixedUpdate;
            }
            else if (Vector3.Distance(stateBuffer[serverStateBufferIndex].velocity, latestServerState.Value.velocity) > serverReconciliationThreshold)
            {
                return latestServerState.Value.velocity - stateBuffer[serverStateBufferIndex].velocity;
            }
            return Vector3.zero;
        }

        public override void OnActionClipPlayed()
        {
            if (IsServer)
            {
                // Empty the input queue and simulate the player up. This prevents the player from jumping backwards in time because the server simulation runs behind the owner simulation
                while (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                {
                    if (serverInputQueue.Count > 0)
                    {
                        if (inputPayload.moveInput == Vector2.zero & lastMoveInputProcessedOnServer == Vector2.zero)
                        {
                            if (!combatAgent.AnimationHandler.ShouldApplyRootMotion()) { continue; }
                        }
                    }

                    StatePayload statePayload = Move(inputPayload, combatAgent.AnimationHandler.ApplyRootMotion(), combatAgent.AnimationHandler.ShouldApplyRootMotion());
                    stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                    latestServerState.Value = statePayload;
                    lastMoveInputProcessedOnServer = inputPayload.moveInput;
                    NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody);
                }
            }
            rootMotionTick = 0;
        }

        private Vector2 lastMoveInputProcessedOnServer;
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner & !IsServer)
            {
                if (latestServerState.Value.tick > 0)
                {
                    // Sync position here with latest server state
                    Rigidbody.MovePosition(transform.position);
                }
            }

            if (!IsClient)
            {
                if (rootMotionTick != -1)
                {
                    Quaternion newRotation = latestServerState.Value.rotation;
                    while (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                    {
                        newRotation = inputPayload.rotation;
                    }

                    StatePayload statePayload = Move(new InputPayload(latestServerState.Value.tick, Vector2.zero, newRotation),
                        combatAgent.AnimationHandler.ApplyRootMotion(),
                        true);
                    stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                    latestServerState.Value = statePayload;
                    lastMoveInputProcessedOnServer = Vector2.zero;
                }
                else
                {
                    while (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                    {
                        if (serverInputQueue.Count > 3)
                        {
                            if (inputPayload.moveInput == Vector2.zero & lastMoveInputProcessedOnServer == Vector2.zero) { continue; }
                        }

                        StatePayload statePayload = Move(inputPayload,
                            combatAgent.AnimationHandler.ApplyRootMotion(),
                            false);
                        stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                        latestServerState.Value = statePayload;
                        lastMoveInputProcessedOnServer = inputPayload.moveInput;
                        break;
                    }
                }
            }

            if (IsOwner)
            {
                Vector3 serverReconciliationVelocityError = Vector3.zero;
                if (latestServerState.Value.tick > 0 & latestServerState.Value.tick < movementTick)
                {
                    if (!latestServerState.Equals(default(StatePayload)) &&
                        (lastProcessedState.Equals(default(StatePayload)) ||
                        !latestServerState.Equals(lastProcessedState)))
                    {
                        HandleServerReconciliation();
                    }
                }
                
                Vector2 moveInput;
                bool shouldApplyRootMotion = IsHost ? combatAgent.AnimationHandler.ShouldApplyRootMotion() : rootMotionTick != -1;
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
                else if (shouldApplyRootMotion)
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
                if (IsServer) // we're the host here
                {
                    movementTick++;
                }
                else
                {
                    if (!shouldApplyRootMotion) { movementTick++; }
                }
                
                StatePayload statePayload = Move(inputPayload,
                    combatAgent.AnimationHandler.ApplyRootMotion(),
                    shouldApplyRootMotion);
                
                if (inputPayload.tick % BUFFER_SIZE < inputBuffer.Count)
                    inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;
                else
                    inputBuffer.Add(inputPayload);

                stateBuffer[inputPayload.tick % BUFFER_SIZE] = statePayload;
                Rigidbody.AddForce(serverReconciliationVelocityError, ForceMode.VelocityChange);

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
            TargetToLockOn = default;
            CameraFollowTarget = default;
            joysticks = new UIDeadZoneElement[0];
        }

        private int movementTick;
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        private int rootMotionTick = -1;
        private bool isServerAuthoritative;
#if UNITY_EDITOR
        private List<Vector3> bakedRootMotion = new List<Vector3>();
#endif
        private StatePayload Move(InputPayload inputPayload, Vector3 rootMotion, bool shouldApplyRootMotion)
        {
            if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                if (IsServer)
                {
                    Rigidbody.Sleep();
                }
                else
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(latestServerState.Value.position);
                }
                return new StatePayload(inputPayload, Rigidbody, inputPayload.rotation, false);
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed & !combatAgent.IsGrabbing)
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
#if UNITY_EDITOR
                if (IsHost) { bakedRootMotion.Add(rootMotion); }
                else
                {
                    Vector3[] data = weaponHandler.GetWeapon().GetRootMotionData(combatAgent.AnimationHandler.GetAnimationClip(combatAgent.WeaponHandler.CurrentActionClip));
                    if (data.Length > 0 & rootMotionTick < data.Length) { rootMotion = data[rootMotionTick]; }
                    if (rootMotionTick >= data.Length)
                    {
                        rootMotionTick = -1;
                    }
                    else
                    {
                        rootMotionTick++;
                    }
                }
#else
                Vector3[] data = weaponHandler.GetWeapon().GetRootMotionData(combatAgent.AnimationHandler.GetAnimationClip(combatAgent.WeaponHandler.CurrentActionClip));
                if (data.Length > 0 & rootMotionTick < data.Length) { rootMotion = data[rootMotionTick]; }
                if (rootMotionTick >= data.Length)
                {
                    rootMotionTick = -1;
                }
                else
                {
                    rootMotionTick++;
                }
#endif
                movement = (IsServer ? newRotation : latestServerState.Value.rotation) * rootMotion * GetRootMotionSpeed();
            }
            else
            {
#if UNITY_EDITOR
                // For baking root motion in the training room
                if (IsHost & latestServerState.Value.usedRootMotion)
                {
                    // Bake root motion into the weapon for determinism at runtime
                    Weapon weapon = null;
                    if (combatAgent.LoadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary)
                    {
                        weapon = combatAgent.LoadoutManager.PrimaryWeaponOption.weapon;
                    }
                    else if (combatAgent.LoadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Secondary)
                    {
                        weapon = combatAgent.LoadoutManager.SecondaryWeaponOption.weapon;
                    }

                    if (weapon)
                    {
                        if (weapon.SetRootMotionData(combatAgent.AnimationHandler.GetAnimationClip(combatAgent.WeaponHandler.CurrentActionClip), bakedRootMotion))
                        {
                            Debug.Log("Setting root motion data " + weapon + " " + combatAgent.WeaponHandler.CurrentActionClip);
                        }
                    }
                    bakedRootMotion.Clear();
                }
#endif
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = combatAgent.StatusAgent.IsRooted() | combatAgent.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            if (shouldApplyRootMotion)
            {
                isServerAuthoritative = true;
            }
            else if (isServerAuthoritative)
            {
                isServerAuthoritative = false;
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
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
            return new StatePayload(inputPayload, Rigidbody, newRotation, shouldApplyRootMotion);
        }

        private const float bodyRadius = 0.5f;

        private Quaternion EvaluateRotation()
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
        private const float serverReconciliationTeleportThreshold = 0.5f;
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
            
            if (IsOwner)
            {
                if (latestServerState.Value.usedRootMotion & !combatAgent.ShouldApplyAilmentRotation() & !combatAgent.IsGrabbed & !combatAgent.IsGrabbing & !combatAgent.ShouldPlayHitStop())
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, latestServerState.Value.rotation, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * CameraController.orbitSpeed);
                }
                else
                {
                    transform.rotation = EvaluateRotation();
                }
            }
            else
            {
                transform.rotation = EvaluateRotation();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkTransform.SetPositionMaximumInterpolationTime(ActionClip.defaultMaximumAnimationInterpolationTime);
            networkTransform.SyncPositionX = !IsOwner;
            networkTransform.SyncPositionY = !IsOwner;
            networkTransform.SyncPositionZ = !IsOwner;
            if (IsLocalPlayer)
            {
                inputBuffer.Clear();

                cameraController.gameObject.tag = "MainCamera";
                cameraController.gameObject.SetActive(true);
                cameraController.gameObject.AddComponent<AudioListener>();

                cameraController.SetActive(true);

                cameraController.SetOrbitalCameraState(false);

                playerInput.enabled = true;
                string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);

                actionMapHandler.enabled = true;
            }
            else
            {
                cameraController.gameObject.SetActive(false);

                cameraController.SetActive(false);

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
            networkTransform.SyncPositionX = true;
            networkTransform.SyncPositionY = true;
            networkTransform.SyncPositionZ = true;
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
            else if (networkListEvent.Type != NetworkListEvent<InputPayload>.EventType.Clear)
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
        private NetworkTransform networkTransform;
        protected override void Awake()
        {
            base.Awake();
            Rigidbody.isKinematic = true;

            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new NetworkList<InputPayload>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
            serverInputQueue = new Queue<InputPayload>();

            actionMapHandler = GetComponent<ActionMapHandler>();
            networkTransform = GetComponent<NetworkTransform>();
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
            if (IsLocalPlayer)
            {
                cameraController.UpdateCamera();
                networkTransform.SetPositionMaximumInterpolationTime(combatAgent.WeaponHandler.CurrentActionClip.GetMaximumAnimationInterpolationTime(combatAgent.AnimationHandler.GetActionClipNormalizedTime(combatAgent.WeaponHandler.CurrentActionClip)));
            }
            AutoAim();
            SetAnimationMoveInput(IsOwner ? GetPlayerMoveInput() : latestServerState.Value.moveInput);

            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

#if UNITY_EDITOR
        private bool drawCasts;
#endif

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

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(latestServerState.Value.position, 0.5f);

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(Rigidbody.position, 0.3f);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawWireSphere(Rigidbody.position, isGroundedSphereCheckRadius);
        }
    }
}

