using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.Player
{
    [RequireComponent(typeof(PlayerMovementHandler))]
    public class PlayerNetworkMovementPrediction : NetworkBehaviour
    {
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
            public bool isGrounded;
            public Vector3 position;
            public Quaternion rotation;

            public StatePayload(int tick, InputPayload inputPayload, bool isGrounded, Vector3 position, Quaternion rotation)
            {
                this.tick = tick;
                moveInput = inputPayload.moveInput;
                this.isGrounded = isGrounded;
                this.position = position;
                this.rotation = rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref isGrounded);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
            }
        }

        public bool IsGrounded()
        {
            if (IsOwner)
            {
                return stateBuffer[Mathf.Max(0, NetworkManager.NetworkTickSystem.LocalTime.Tick-1) % BUFFER_SIZE].isGrounded;
            }
            else
            {
                return latestServerState.Value.isGrounded;
            }
        }

        public Vector2 GetWalkCycleAnimationParameters()
        {
            if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
            {
                return Vector2.zero;
            }
            else if (!movementHandler.CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                return Vector2.zero;
            }
            else
            {
                Vector2 moveInput = movementHandler.GetMoveInput();
                Vector2 animDir = (new Vector2(moveInput.x, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                animDir = Vector2.ClampMagnitude(animDir, 1);

                if (combatAgent.WeaponHandler.IsBlocking)
                {
                    switch (combatAgent.WeaponHandler.GetWeapon().GetBlockingLocomotion())
                    {
                        case Weapon.BlockingLocomotion.NoMovement:
                            animDir = Vector2.zero;
                            break;
                        case Weapon.BlockingLocomotion.CanWalk:
                            animDir /= 2;
                            break;
                        case Weapon.BlockingLocomotion.CanRun:
                            break;
                        default:
                            Debug.LogError("Unsure how to handle blocking locomotion type: " + combatAgent.WeaponHandler.GetWeapon().GetBlockingLocomotion());
                            break;
                    }
                }
                return animDir;
            }
        }

        private bool applyOverridePosition;
        private Vector3 overridePosition;
        public void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsServer) { Debug.LogError("PlayerNetworkMovementPrediction.SetOrientation() should only be called on the server!"); return; }
            CurrentPosition = newPosition;
            overridePosition = newPosition;
            applyOverridePosition = true;

            overrideRotation.Value = newRotation;
            applyOverrideRotation.Value = true;
            SetRotationClientRpc(newRotation);
        }

        private NetworkVariable<bool> applyOverrideRotation = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private NetworkVariable<Quaternion> overrideRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        [Rpc(SendTo.Owner)]
        private void SetRotationClientRpc(Quaternion newRotation)
        {
            movementHandler.SetCameraRotation(newRotation.eulerAngles.x, newRotation.eulerAngles.y);
        }

        public float playerObjectTeleportThreshold = 2;

        private const int BUFFER_SIZE = 1024;

        private StatePayload[] stateBuffer;
        private NetworkList<InputPayload> inputBuffer;
        private NetworkVariable<StatePayload> latestServerState = new NetworkVariable<StatePayload>();
        private StatePayload lastProcessedState;
        private Queue<InputPayload> inputQueue;

        public Vector3 CurrentPosition { get; private set; }
        public Quaternion CurrentRotation { get; private set; }

        private PlayerMovementHandler movementHandler;
        private CombatAgent combatAgent;

        private void Awake()
        {
            movementHandler = GetComponent<PlayerMovementHandler>();
            combatAgent = GetComponent<CombatAgent>();
            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new NetworkList<InputPayload>(new InputPayload[BUFFER_SIZE], NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
            inputQueue = new Queue<InputPayload>();

            CurrentPosition = transform.position;
            CurrentRotation = transform.rotation;
            if (NetworkManager.Singleton.IsServer) { overrideRotation.Value = transform.rotation; }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                overrideRotation.Value = transform.rotation;
                latestServerState.Value = new StatePayload(0, new InputPayload(), true, CurrentPosition, CurrentRotation);
                stateBuffer[latestServerState.Value.tick % BUFFER_SIZE] = latestServerState.Value;

                inputBuffer.OnListChanged += OnInputBufferChanged;
                NetworkManager.NetworkTickSystem.Tick += HandleServerTick;
            }
            if (IsClient)
            {
                stateBuffer[latestServerState.Value.tick % BUFFER_SIZE] = latestServerState.Value;

                NetworkManager.NetworkTickSystem.Tick += HandleClientTick;
            }
            CurrentPosition = transform.position;
            CurrentRotation = transform.rotation;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                inputBuffer.OnListChanged -= OnInputBufferChanged;
                NetworkManager.NetworkTickSystem.Tick -= HandleServerTick;
            }
            if (IsClient)
                NetworkManager.NetworkTickSystem.Tick -= HandleClientTick;
        }

        public void OnInputBufferChanged(NetworkListEvent<InputPayload> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Value)
            {
                inputQueue.Enqueue(networkListEvent.Value);
            }
            else
            {
                Debug.Log("We shouldn't be receiving an event for a network list event type of: " + networkListEvent.Type);
            }
        }

        private void HandleServerTick()
        {
            int bufferIndex = ProcessInputQueue();

            if (bufferIndex != -1)
            {
                latestServerState.Value = stateBuffer[bufferIndex];
            }
        }

        private void HandleClientTick()
        {
            if (IsOwner)
            {
                if (!latestServerState.Equals(default(StatePayload)) &&
                (lastProcessedState.Equals(default(StatePayload)) ||
                !latestServerState.Equals(lastProcessedState)))
                {
                    HandleServerReconciliation();
                }

                InputPayload inputPayload = new InputPayload(NetworkManager.NetworkTickSystem.LocalTime.Tick, movementHandler.GetMoveInput(), transform.rotation);
                inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;

                if (!IsHost)
                {
                    inputQueue.Enqueue(inputPayload);
                    ProcessInputQueue();
                }
            }
            else // If we are not the owner of this object
            {
                CurrentPosition = latestServerState.Value.position;
                CurrentRotation = latestServerState.Value.rotation;
            }
        }

        private int ProcessInputQueue()
        {
            int bufferIndex = -1;

            while (inputQueue.Count > 0)
            {
                InputPayload inputPayload = inputQueue.Dequeue();
                bufferIndex = inputPayload.tick % BUFFER_SIZE;

                StatePayload statePayload = ProcessInput(inputPayload);
                CurrentPosition = statePayload.position;
                CurrentRotation = statePayload.rotation;

                stateBuffer[bufferIndex] = statePayload;
            }

            return bufferIndex;
        }

        private const float serverReconciliationThreshold = 0.0001f;
        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState.Value;

            int serverStateBufferIndex = latestServerState.Value.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex].position);

            if (positionError > serverReconciliationThreshold)
            {
                //Debug.Log(OwnerClientId + " Position Error: " + positionError);

                CurrentPosition = latestServerState.Value.position;
                CurrentRotation = latestServerState.Value.rotation;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState.Value;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                int tickToProcess = latestServerState.Value.tick + 1;
                while (tickToProcess < NetworkManager.NetworkTickSystem.LocalTime.Tick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = ProcessInput(inputBuffer[bufferIndex]);
                    CurrentPosition = statePayload.position;
                    CurrentRotation = statePayload.rotation;

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
            }
        }

        private bool removeRotationServerRpcSent;
        [Rpc(SendTo.Server)] private void RemoveRotationOverrideRpc() { applyOverrideRotation.Value = false; }

        private StatePayload ProcessInput(InputPayload input)
        {
            // Should always be in sync with same function on Client
            StatePayload statePayload = movementHandler.ProcessMovement(input);
            if (applyOverridePosition) { statePayload.position = overridePosition; applyOverridePosition = false; }
            
            if (IsServer)
            {
                if (applyOverrideRotation.Value) { statePayload.rotation = overrideRotation.Value; }
            }

            if (IsOwner)
            {
                if (applyOverrideRotation.Value)
                {
                    statePayload.rotation = overrideRotation.Value;
                    if (!removeRotationServerRpcSent) { RemoveRotationOverrideRpc(); }
                    removeRotationServerRpcSent = true;
                }
                else
                {
                    removeRotationServerRpcSent = false;
                }
            }
            return statePayload;
        }

        public void ProcessCollisionEvent(Collision collision, Vector3 newPosition)
        {
            CurrentPosition = newPosition;
        }

        private void OnDrawGizmos()
        {
            if (OwnerClientId == 0)
                Gizmos.color = Color.red;
            else if (OwnerClientId == 1)
                Gizmos.color = Color.blue;
            else if (OwnerClientId == 2)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.black;

            Gizmos.DrawWireSphere(CurrentPosition, 0.25f);
        }
    }
}