using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Vi.Core.MovementHandlers;
using Vi.ProceduralAnimations;
using Vi.ScriptableObjects;
using Vi.Utility;
using System.Collections;
using Unity.Netcode.Components;

namespace Vi.Player
{
    public class PlayerMovementHandler : PhysicsMovementHandler
    {
        [Header("Player Movement Handler")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Rigidbody interpolationRigidbody;

        public CameraController CameraController { get { return cameraController; } }

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetOrientation(newPosition, newRotation);
            if (IsSpawned)
            {
                SetRotationClientRpc(newRotation);
            }
        }

        [Rpc(SendTo.Owner)]
        private void SetRotationClientRpc(Quaternion newRotation)
        {
            if (cameraController)
            {
                cameraController.SetRotation(newRotation.eulerAngles.x, newRotation.eulerAngles.y);
            }
            else
            {
                Debug.LogError("Recieved a set rotation client RPC but there's no camera controller for " + combatAgent.GetName());
            }
        }

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
            public bool shouldUseRootMotion;
            public Vector3 rootMotion;
            public float runSpeed;
            public LoadoutManager.WeaponSlotType equippedWeaponSlot;
            public bool isGrounded;
            public bool shouldPlayHitStop;
            public float stairMovement;

            public InputPayload(int tick, Vector2 moveInput, Quaternion rotation, bool shouldUseRootMotion, Vector3 rootMotion, float runSpeed,
                LoadoutManager.WeaponSlotType weaponSlotType, bool isGrounded, bool shouldPlayHitStop)
            {
                this.tick = tick;
                this.moveInput = moveInput;
                this.rotation = rotation;
                this.shouldUseRootMotion = shouldUseRootMotion;
                this.rootMotion = rootMotion;
                this.runSpeed = runSpeed;
                this.equippedWeaponSlot = weaponSlotType;
                this.isGrounded = isGrounded;
                this.shouldPlayHitStop = shouldPlayHitStop;
                // This is assigned after the input is processed
                this.stairMovement = 0;
            }

            public bool Equals(InputPayload other)
            {
                return tick == other.tick & moveInput == other.moveInput & rotation == other.rotation & shouldUseRootMotion == other.shouldUseRootMotion & rootMotion == other.rootMotion;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref stairMovement);
            }
        }

        public struct StatePayload : INetworkSerializable, System.IEquatable<StatePayload>
        {
            public int tick;
            public Vector2 moveInput;
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
            public bool usedRootMotion;
            public int rootMotionId;
            public float rootMotionTime;

            public StatePayload(InputPayload inputPayload, Rigidbody Rigidbody, bool usedRootMotion, int rootMotionId, float rootMotionTime)
            {
                tick = inputPayload.tick;
                moveInput = inputPayload.moveInput;
                position = Rigidbody.position;
                velocity = Rigidbody.linearVelocity;
                this.rotation = inputPayload.rotation;
                this.usedRootMotion = usedRootMotion;
                this.rootMotionId = rootMotionId;
                this.rootMotionTime = rootMotionTime;
            }

            public bool Equals(StatePayload other)
            {
                return tick == other.tick;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
                serializer.SerializeValue(ref usedRootMotion);
                if (usedRootMotion)
                {
                    serializer.SerializeValue(ref rootMotionId);
                    serializer.SerializeValue(ref rootMotionTime);
                }
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
        private Vector3 HandleServerReconciliation()
        {
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                lastProcessedState = latestServerState.Value;
                return Vector3.zero;
            }

            if (!CanMove())
            {
                if (Rigidbody.isKinematic) { Rigidbody.MovePosition(latestServerState.Value.position); }
                lastProcessedState = latestServerState.Value;
                return Vector3.zero;
            }

            int serverStateBufferIndex = latestServerState.Value.tick % BUFFER_SIZE;
            if (latestServerState.Value.usedRootMotion)
            {
                int rootMotionReconciliationIndex = System.Array.FindIndex(stateBuffer, item => item.rootMotionId == latestServerState.Value.rootMotionId
                    & Mathf.Approximately(item.rootMotionTime, latestServerState.Value.rootMotionTime));

                if (rootMotionReconciliationIndex == -1)
                {
                    if (lastProcessedState.rootMotionTime > latestServerState.Value.rootMotionTime & lastProcessedState.rootMotionId == latestServerState.Value.rootMotionId)
                    {
                        Debug.LogWarning("Root Motion State Not Found At Time: " + latestServerState.Value.rootMotionTime + " " + combatAgent.WeaponHandler.CurrentActionClip + " " + combatAgent.WeaponHandler.GetWeapon());
                    }
                }
                else // Root motion state found
                {
                    lastProcessedState = latestServerState.Value;

                    StatePayload rootMotionReconciliationState = stateBuffer[rootMotionReconciliationIndex];
                    float rootMotionPositionError = Vector3.Distance(latestServerState.Value.position, rootMotionReconciliationState.position);
                    if (rootMotionPositionError > serverReconciliationThreshold)
                    {
                        //Debug.Log(latestServerState.Value.tick + " " + rootMotionReconciliationState.tick + " Root Motion Position Error: " + rootMotionPositionError);

                        StatePayload modifiedStatePayload = latestServerState.Value;
                        modifiedStatePayload.tick = combatAgent.AnimationHandler.WasLastActionClipMotionPredicted ? latestServerState.Value.tick : rootMotionReconciliationState.tick;
                        stateBuffer[rootMotionReconciliationIndex] = modifiedStatePayload;

                        ReprocessInputs(modifiedStatePayload);
                    }
                    else
                    {
                        return latestServerState.Value.position - rootMotionReconciliationState.position;
                    }
                }
                return Vector3.zero;
            }

            lastProcessedState = latestServerState.Value;

            if (stateBuffer[serverStateBufferIndex].usedRootMotion)
            {
                return Vector3.zero;
            }

            float positionError = Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex].position);
            if (positionError > serverReconciliationThreshold)
            {
                //Debug.Log(latestServerState.Value.tick + " Position Error: " + positionError);

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState.Value;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                ReprocessInputs(latestServerState.Value);
            }
            else
            {
                return latestServerState.Value.position - stateBuffer[serverStateBufferIndex].position;
            }
            return Vector3.zero;
        }

        private int stepsToBuffer;
        private void ReprocessInputs(StatePayload latestServerState)
        {
            ResetNonOwnerCollidersToServerPosition();

            Vector3 oldPosition = Rigidbody.position;
            Rigidbody.position = latestServerState.position;
            if (!Rigidbody.isKinematic) { Rigidbody.linearVelocity = latestServerState.velocity; }

            Physics.simulationMode = SimulationMode.Script;
            NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody, false);

            int tickToProcess = latestServerState.tick + 1;
            while (tickToProcess < movementTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with reconciled state
                InputPayload inputPayload = inputBuffer[bufferIndex];
                if (inputPayload.equippedWeaponSlot != combatAgent.LoadoutManager.GetEquippedSlotType())
                {
                    inputPayload.runSpeed = GetRunSpeed();
                }
                StatePayload statePayload = Move(ref inputPayload, true);
                NetworkPhysicsSimulation.SimulateOneRigidbody(Rigidbody, false);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
            Physics.simulationMode = SimulationMode.FixedUpdate;

            // Only interpolate error if the distance is large enough
            if (Vector3.Distance(oldPosition, Rigidbody.position) > 0.05f)
            {
                stepsToBuffer = 1;
            }
        }

        public override Vector2[] GetMoveInputQueue()
        {
            InputPayload[] arr = new InputPayload[serverInputQueue.Count];
            serverInputQueue.CopyTo(arr, 0);

            List<Vector2> result = new List<Vector2>();
            foreach (InputPayload inputPayload in arr)
            {
                result.Add(inputPayload.moveInput);
            }
            return result.ToArray();
        }

        private void SetInputPayloadVariablesOnServer(ref InputPayload inputPayload)
        {
            if (combatAgent.ShouldApplyAilmentRotation()) { inputPayload.rotation = combatAgent.GetAilmentRotation(); }
            inputPayload.shouldUseRootMotion = combatAgent.AnimationHandler.ShouldApplyRootMotion();
            inputPayload.rootMotion = combatAgent.AnimationHandler.ApplyRootMotion();

            if (combatAgent.StatusAgent.IsRooted())
            {
                inputPayload.rootMotion.x = 0;
                inputPayload.rootMotion.z = 0;
            }

            inputPayload.runSpeed = GetRunSpeed();
            inputPayload.isGrounded = IsGrounded();
            inputPayload.shouldPlayHitStop = combatAgent.ShouldPlayHitStop();
        }

        private float timeWithoutInputs;
        private InputPayload lastInputPayloadProcessedOnServer;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!IsSpawned)
            {
                //interpolationRigidbody.position = Rigidbody.position;
                return;
            }

            if (!IsClient)
            {
                bool shouldApplyRootMotion = combatAgent.AnimationHandler.ShouldApplyRootMotion();
                // This if statement should only be reached
                if (shouldApplyRootMotion & !combatAgent.AnimationHandler.WasLastActionClipMotionPredicted)
                {
                    timeWithoutInputs = 0;
                    InputPayload serverInputPayload = new InputPayload();
                    if (serverInputQueue.Count > 0)
                    {
                        serverInputPayload = serverInputQueue.Dequeue();
                    }
                    else
                    {
                        serverInputPayload.rotation = latestServerState.Value.rotation;
                    }

                    serverInputPayload.tick = latestServerState.Value.tick + 1;
                    serverInputPayload.moveInput = Vector2.zero;
                    SetInputPayloadVariablesOnServer(ref serverInputPayload);

                    StatePayload statePayload = Move(ref serverInputPayload, false);

                    stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                    latestServerState.Value = statePayload;
                    lastInputPayloadProcessedOnServer = serverInputPayload;
                }
                else if (serverInputQueue.Count > 0)
                {
                    timeWithoutInputs = 0;
                    while (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                    {
                        // Have to double check these to prevent cheating
                        if (combatAgent.StatusAgent.IsRooted())
                        {
                            inputPayload.moveInput = Vector2.zero;
                        }
                        else if (combatAgent.AnimationHandler.IsReloading())
                        {
                            inputPayload.moveInput = Vector2.zero;
                        }
                        else if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
                        {
                            inputPayload.moveInput = Vector2.zero;
                        }
                        else if (!CanMove())
                        {
                            inputPayload.moveInput = Vector2.zero;
                        }
                        else if (!combatAgent.AnimationHandler.IsAtRest() & !combatAgent.AnimationHandler.WasLastActionClipMotionPredicted)
                        {
                            inputPayload.moveInput = Vector2.zero;
                        }

                        if (serverInputQueue.Count > 3 & !shouldApplyRootMotion)
                        {
                            if (inputPayload.moveInput == Vector2.zero & lastInputPayloadProcessedOnServer.moveInput == Vector2.zero) { continue; }
                        }

                        SetInputPayloadVariablesOnServer(ref inputPayload);

                        StatePayload statePayload = Move(ref inputPayload, false);
                        stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                        latestServerState.Value = statePayload;
                        lastInputPayloadProcessedOnServer = inputPayload;
                        break;
                    }
                }
                else // Server input queue is 0 meaning we're waiting on the client to send inputs
                {
                    timeWithoutInputs += Time.deltaTime;
                    // Make the player descend to the ground if it has no inputs (they're lagging out)
                    if (timeWithoutInputs > 1.5f)
                    {
                        Rigidbody.linearVelocity = Physics.gravity;
                    }
                }
            }

            if (IsOwner)
            {
                if (!IsServer)
                {
                    if (latestServerState.Value.tick > 0 & latestServerState.Value.tick < movementTick)
                    {
                        if (!latestServerState.Value.Equals(lastProcessedState))
                        {
                            Vector3 serverReconciliationOffset = HandleServerReconciliation();
                            serverReconciliationOffset *= Time.fixedDeltaTime;
                            Rigidbody.position += serverReconciliationOffset;

                            int tickToProcess = latestServerState.Value.tick + 1;
                            while (tickToProcess < movementTick)
                            {
                                int bufferIndex = tickToProcess % BUFFER_SIZE;

                                // Update buffer with recalculated state
                                stateBuffer[bufferIndex].position += serverReconciliationOffset;

                                tickToProcess++;
                            }
                        }
                    }
                }

                Vector2 moveInput;
                bool shouldApplyRootMotion = combatAgent.AnimationHandler.ShouldApplyRootMotion();
                if (combatAgent.WeaponHandler.LightAttackIsPressed)
                {
                    moveInput = Vector2.zero;
                }
                else if (combatAgent.AnimationHandler.WaitingForActionClipToPlay)
                {
                    moveInput = Vector2.zero;
                }
                else if (combatAgent.AnimationHandler.IsReloading())
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
                else if (combatAgent.StatusAgent.IsRooted())
                {
                    moveInput = Vector2.zero;
                }
                else if (shouldApplyRootMotion)
                {
                    moveInput = Vector2.zero;
                }
                else if (!combatAgent.AnimationHandler.IsAtRest())
                {
                    moveInput = Vector2.zero;
                }
                else
                {
                    moveInput = GetPlayerMoveInput();
                }

                InputPayload inputPayload = new InputPayload(movementTick, moveInput,
                    EvaluateRotation(), shouldApplyRootMotion, combatAgent.AnimationHandler.ApplyRootMotion(),
                    GetRunSpeed(), combatAgent.LoadoutManager.GetEquippedSlotType(), IsGrounded(), combatAgent.ShouldPlayHitStop());

                if (combatAgent.StatusAgent.IsRooted())
                {
                    inputPayload.rootMotion.x = 0;
                    inputPayload.rootMotion.z = 0;
                }

                StatePayload statePayload = Move(ref inputPayload, false);

                bool isGameOver = false;
                if (GameModeManager.Singleton)
                {
                    if (GameModeManager.Singleton.IsGameOver()) { isGameOver = true; }
                }

                if (!NetSceneManager.IsBusyLoadingScenes() & !isGameOver)
                {
                    movementTick++;

                    if (!isGameOver)
                    {
                        if (inputPayload.tick % BUFFER_SIZE < inputBuffer.Count)
                            inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;
                        else
                            inputBuffer.Add(inputPayload);
                    }
                }

                stateBuffer[inputPayload.tick % BUFFER_SIZE] = statePayload;

                if (IsServer) { latestServerState.Value = statePayload; }
            }

            if (latestServerState.Value.tick == 0 & !IsServer)
            {
                Rigidbody.Sleep();
            }

            float t = 1;
            if (stepsToBuffer == 0)
            {
                t = 1;
            }
            else if (stepsToBuffer == 1)
            {
                t = 0.75f;
            }
            else if (stepsToBuffer == 2)
            {
                t = 0.5f;
            }
            else
            {
                Debug.LogWarning("Unsure how to handle steps to buffer number " + stepsToBuffer);
            }

            interpolationRigidbody.MovePosition(Vector3.Lerp(interpolationRigidbody.position, Rigidbody.position, t));

            if (stepsToBuffer > 0)
            {
                stepsToBuffer--;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            movementTick = default;
            TargetToLockOn = default;
            CameraFollowTarget = default;
            joysticks = new UIDeadZoneElement[0];

            lastInputPayloadProcessedOnServer = default;
            lastProcessedState = default;
            stateBuffer = new StatePayload[BUFFER_SIZE];
            lastProcessedState = default;
            serverInputQueue.Clear();
        }

        private void OnLastMovementWasZeroSyncedChanged(bool prev, bool current)
        {
            LastMovementWasZero = current;
        }

        private NetworkVariable<bool> lastMovementWasZeroSynced = new NetworkVariable<bool>();
        private void SetLastMovement(Vector3 lastMovement)
        {
            bool value = lastMovement == Vector3.zero;

            LastMovementWasZero = value;

            if (IsServer)
            {
                lastMovementWasZeroSynced.Value = value;
            }
        }

        private int movementTick;
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        private StatePayload Move(ref InputPayload inputPayload, bool isReprocessing)
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
                SetLastMovement(Vector3.zero);
                return new StatePayload(inputPayload, Rigidbody, false, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
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
                SetLastMovement(Vector3.zero);
                return new StatePayload(inputPayload, Rigidbody, false, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
            }

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = true;
                //if (!IsServer) { Rigidbody.MovePosition(latestServerState.Value.position); }
                SetLastMovement(Vector3.zero);
                return new StatePayload(inputPayload, Rigidbody, false, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
            }
            else if (combatAgent.IsGrabbed & combatAgent.GetAilment() == ActionClip.Ailment.None)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(grabAssailant.MovementHandler.GetPosition() + (grabAssailant.MovementHandler.GetRotation() * Vector3.forward));
                    SetLastMovement(Vector3.zero);
                    return new StatePayload(inputPayload, Rigidbody, false, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
                }
            }
            else if (inputPayload.shouldPlayHitStop)
            {
                movement = Vector3.zero;
            }
            else if (combatAgent.IsPulled)
            {
                movement = combatAgent.GetPullAssailantPosition() - GetPosition();
            }
            else if (inputPayload.shouldUseRootMotion)
            {
                Quaternion rootMotionRotation;
                // Dodges are predicted, meaning they don't wait for server confirmation before playing
                if (combatAgent.AnimationHandler.WasLastActionClipMotionPredicted)
                {
                    rootMotionRotation = inputPayload.rotation;
                }
                else
                {
                    rootMotionRotation = IsServer ? inputPayload.rotation : latestServerState.Value.rotation;
                }
                movement = rootMotionRotation * inputPayload.rootMotion;
            }
            else
            {
                Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.moveInput.x, 0, inputPayload.moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= inputPayload.runSpeed;
                movement = targetDirection;
            }

            Rigidbody.isKinematic = false;

            if (combatAgent.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            // Stair Movement
            float stairMovement = 0;
            if (IsOwner & !isReprocessing)
            {
                Vector3 startPos = Rigidbody.position + inputPayload.rotation * stairRaycastingStartOffset;
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
            }
            else
            {
                stairMovement = inputPayload.stairMovement;
                if (stairMovement > maxStairStepHeight) { stairMovement = 0; }
            }

            bool evaluateForce = true;
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity & inputPayload.shouldUseRootMotion)
            {
                if (movement.y >= 0)
                {
                    Rigidbody.AddForce(movement - Rigidbody.linearVelocity, ForceMode.VelocityChange);
                }
                else
                {
                    Rigidbody.AddForce(movement - new Vector3(Rigidbody.linearVelocity.x, 0, Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
                }
                evaluateForce = false;
            }

            if (evaluateForce)
            {
                if (inputPayload.isGrounded)
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
            if (GetStairCollidersCount() == 0 | !Mathf.Approximately(movement.sqrMagnitude, 0)) { Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration); }
            inputPayload.stairMovement = stairMovement;
            SetLastMovement(movement);
            return new StatePayload(inputPayload, Rigidbody, inputPayload.shouldUseRootMotion, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
        }

        private const float bodyRadius = 0.5f;

        private Quaternion EvaluateRotation()
        {
            if (combatAgent.ShouldApplyAilmentRotation()) { return combatAgent.GetAilmentRotation(); }

            if (combatAgent.IsGrabbed)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                    return IsolateYRotation(Quaternion.LookRotation(rel, Vector3.up));
                }
            }

            if (IsOwner)
            {
                Vector3 camDirection = cameraController.GetCamDirection();

                if (combatAgent.IsGrabbing)
                    return transform.rotation;
                else if (!combatAgent.ShouldPlayHitStop())
                    return IsolateYRotation(Quaternion.LookRotation(camDirection));
            }
            else
            {
                return Quaternion.Slerp(transform.rotation, latestServerState.Value.rotation, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * CameraController.orbitSpeed);
            }
            return transform.rotation;
        }

        private void UpdateTransform()
        {
            if (!IsSpawned) { return; }
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }

            transform.position = interpolationRigidbody.transform.position;

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

                AdaptivePerformanceManager.Singleton.RefreshThermalSettings();

                StartCoroutine(AutomatedClientLogic());
                StartCoroutine(AutomatedClientMovementLogic());
            }
            else
            {
                cameraController.gameObject.SetActive(false);

                cameraController.SetActive(false);

                playerInput.enabled = false;

                actionMapHandler.enabled = false;
            }

            if (!IsClient & IsServer)
            {
                inputBuffer.OnListChanged += OnInputBufferChanged;
            }

            if (IsServer)
            {
                latestServerState.Value = new StatePayload(new InputPayload(0, Vector2.zero, transform.rotation, false, Vector3.zero, 0, combatAgent.LoadoutManager.GetEquippedSlotType(), true, false),
                    Rigidbody, false, combatAgent.AnimationHandler.RootMotionId, combatAgent.AnimationHandler.TotalRootMotionTime);
            }

            if (!IsOwner & !IsServer)
            {
                lastMovementWasZeroSynced.OnValueChanged += OnLastMovementWasZeroSyncedChanged;
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

            if (!IsClient & IsServer)
            {
                inputBuffer.OnListChanged -= OnInputBufferChanged;
            }

            if (!IsOwner & !IsServer)
            {
                lastMovementWasZeroSynced.OnValueChanged -= OnLastMovementWasZeroSyncedChanged;
            }
        }

        private void OnInputBufferChanged(NetworkListEvent<InputPayload> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Value | networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Add)
            {
                //if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None) { Debug.Log(combatAgent.GetName() + " recieved input " + networkListEvent.Value.moveInput); }
                serverInputQueue.Enqueue(networkListEvent.Value);
            }
            else if (networkListEvent.Type != NetworkListEvent<InputPayload>.EventType.Clear)
            {
                Debug.LogWarning(combatAgent.GetName() + " Player input buffer shouldn't be receiving an event for a network list event type of: " + networkListEvent.Type);
            }
        }

        protected override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            interpolationRigidbody.transform.SetParent(null, true);
            NetworkPhysicsSimulation.AddRigidbody(interpolationRigidbody);
        }

        #region Automated Client Logic
        private IEnumerator AutomatedClientMovementLogic()
        {
            if (!FasterPlayerPrefs.IsAutomatedClient) { yield break; }
            if (!IsLocalPlayer) { yield break; }

            float startTime = Time.time;

            while (true)
            {
                SetLookInput(Random.insideUnitCircle * Random.Range(-2f, 2));
                SetMoveInput(Random.insideUnitCircle * Random.Range(-1f, 1));
                yield return new WaitForSeconds(0.25f);

                if (Time.time - startTime > 15)
                {
                    PersistentLocalObjects.Singleton.StartCoroutine(AutomatedConnectToRandomLobby());
                }
            }
        }

        private IEnumerator AutomatedConnectToRandomLobby()
        {
            Debug.Log("Autoconnecting to random lobby");

            if (WebRequestManager.Singleton.LobbyServers.Length == 0) { yield break; }
            if (!IsLocalPlayer) { yield break; }

            WebRequestManager.Server server = WebRequestManager.Singleton.LobbyServers[Random.Range(0, WebRequestManager.Singleton.LobbyServers.Length)];
            Unity.Netcode.Transports.UTP.UnityTransport networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.SetConnectionData(server.ip, ushort.Parse(server.port), FasterPlayerPrefs.serverListenAddress);

            NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
            NetworkManager.Singleton.StartClient();
        }

        private IEnumerator AutomatedClientLogic()
        {
            if (!FasterPlayerPrefs.IsAutomatedClient) { yield break; }
            if (!IsLocalPlayer) { yield break; }

            yield return new WaitUntil(() => IsSpawned);

            float chargeAttackDuration = 1;
            float chargeWaitDuration = 2;
            float lastChargeAttackTime = Time.time;

            float dodgeWaitDuration = 5;
            float lastDodgeTime = Time.time;

            float weaponSwapDuration = 20;
            float lastWeaponSwapTime = Time.time;

            float abilityWaitDuration = 3;
            float lastAbilityTime = Time.time;

            float bufferTime = 0.5f;

            while (true)
            {
                if (Time.time - lastChargeAttackTime > chargeWaitDuration)
                {
                    weaponHandler.HeavyAttack(true);

                    yield return new WaitForSeconds(chargeAttackDuration);

                    lastChargeAttackTime = Time.time;
                    weaponHandler.HeavyAttack(false);
                }

                yield return new WaitForSeconds(bufferTime);

                if (Time.time - lastWeaponSwapTime > weaponSwapDuration | combatAgent.LoadoutManager.WeaponNameThatCanFlashAttack != null)
                {
                    combatAgent.LoadoutManager.SwitchWeapon();
                    lastWeaponSwapTime = Time.time;
                }

                yield return new WaitForSeconds(bufferTime);

                if (weaponHandler.CanADS)
                {
                    weaponHandler.AimDownSights(true);
                    weaponHandler.LightAttack(true);
                }
                else
                {
                    weaponHandler.LightAttack(true);
                }

                yield return new WaitForSeconds(bufferTime);

                if (Time.time - lastDodgeTime > dodgeWaitDuration)
                {
                    OnDodge();
                    lastDodgeTime = Time.time;
                }

                yield return new WaitForSeconds(bufferTime);

                if (Time.time - lastAbilityTime > abilityWaitDuration)
                {
                    if (combatAgent.GetRage() / combatAgent.GetMaxRage() >= 1)
                    {
                        combatAgent.OnActivateRage();
                        lastAbilityTime = Time.time;
                    }
                    else
                    {
                        List<int> abilitiesOffCooldown = new List<int>();
                        for (int i = 1; i < 5; i++)
                        {
                            switch (i)
                            {
                                case 1:
                                    if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility1()), 1)) { abilitiesOffCooldown.Add(i); }
                                    break;
                                case 2:
                                    if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility2()), 1)) { abilitiesOffCooldown.Add(i); }
                                    break;
                                case 3:
                                    if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility3()), 1)) { abilitiesOffCooldown.Add(i); }
                                    break;
                                case 4:
                                    if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility4()), 1)) { abilitiesOffCooldown.Add(i); }
                                    break;
                                default:
                                    Debug.LogError("Unsure how to handle ability num " + i);
                                    break;
                            }
                        }

                        if (abilitiesOffCooldown.Count == 0)
                        {
                            lastAbilityTime = Time.time;
                        }
                        else
                        {
                            int abilityNum = abilitiesOffCooldown[Random.Range(0, abilitiesOffCooldown.Count)];
                            if (abilityNum == 1)
                            {
                                weaponHandler.Ability1(true);
                            }
                            else if (abilityNum == 2)
                            {
                                weaponHandler.Ability2(true);
                            }
                            else if (abilityNum == 3)
                            {
                                weaponHandler.Ability3(true);
                            }
                            else if (abilityNum == 4)
                            {
                                weaponHandler.Ability4(true);
                            }
                            else
                            {
                                Debug.LogError("Unsure how to handle ability num of - " + abilityNum);
                            }
                            lastAbilityTime = Time.time;
                        }
                    }
                }

                yield return new WaitForSeconds(bufferTime);
            }
        }
        #endregion

        protected override void OnReturnToPool()
        {
            base.OnReturnToPool();
            interpolationRigidbody.transform.SetParent(transform, true);
            NetworkPhysicsSimulation.RemoveRigidbody(interpolationRigidbody);
            cameraController.transform.SetParent(transform);
            cameraController.transform.localPosition = new Vector3(0.34f, 1.73f, -2.49f);
            cameraController.transform.localRotation = Quaternion.identity;
        }

        private const int BUFFER_SIZE = 256;

        private StatePayload[] stateBuffer;
        private NetworkList<InputPayload> inputBuffer;
        private NetworkVariable<StatePayload> latestServerState = new NetworkVariable<StatePayload>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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

        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];
        RaycastHit[] interactableHits = new RaycastHit[10];
        protected override void Update()
        {
            base.Update();

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
                                // Gyroscopic rotation
                                if (UnityEngine.InputSystem.Gyroscope.current != null)
                                {
                                    if (UnityEngine.InputSystem.Gyroscope.current.enabled)
                                    {
                                        Vector3 gyroVelocity = -UnityEngine.InputSystem.Gyroscope.current.angularVelocity.value;
                                        gyroVelocity *= gyroscopicRotationSensitivity;
                                        lookInputToAdd += new Vector2(gyroVelocity.y, gyroVelocity.x);
                                    }
                                }

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
            SetAnimationMoveInput(IsOwner ? GetPlayerMoveInput() : latestServerState.Value.moveInput);

            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        private float gyroscopicRotationSensitivity;

        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
            gyroscopicRotationSensitivity = FasterPlayerPrefs.Singleton.GetFloat("GyroscopicRotationSensitivity");

            if (UnityEngine.InputSystem.Gyroscope.current != null)
            {
                if (Mathf.Approximately(gyroscopicRotationSensitivity, 0))
                {
                    if (UnityEngine.InputSystem.Gyroscope.current.enabled)
                    {
                        UnityEngine.InputSystem.InputSystem.DisableDevice(UnityEngine.InputSystem.Gyroscope.current);
                    }
                }
                else // Sensitivity is not equal to 0
                {
                    if (!UnityEngine.InputSystem.Gyroscope.current.enabled)
                    {
                        UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
                    }
                }
            }
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
            if (combatAgent.AnimationHandler.WaitingForActionClipToPlay) { return; }
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

        public void OnInteract()
        {
            if (TryGetNetworkInteractableInRange(out NetworkInteractable networkInteractable))
            {
                networkInteractable.Interact(gameObject);
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

            Gizmos.color = latestServerState.Value.usedRootMotion ? Color.red : Color.blue;
            Gizmos.DrawSphere(latestServerState.Value.position, 0.5f);

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(Rigidbody.position, 0.3f);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawWireSphere(Rigidbody.position, isGroundedSphereCheckRadius);
        }
    }
}