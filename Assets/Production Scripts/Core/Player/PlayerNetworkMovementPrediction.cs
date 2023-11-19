using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Player
{
    [RequireComponent(typeof(PlayerMovementHandler))]
    public class PlayerNetworkMovementPrediction : NetworkBehaviour
    {
        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;
            public Quaternion rotation;

            public InputPayload(int tick, Vector2 inputVector, Quaternion rotation)
            {
                this.tick = tick;
                this.inputVector = inputVector;
                this.rotation = rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref rotation);
            }
        }

        public struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector3 position;
            public Quaternion rotation;

            public StatePayload(int tick, Vector3 position, Quaternion rotation)
            {
                this.tick = tick;
                this.position = position;
                this.rotation = rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
            }
        }

        private bool applyOverridePosition;
        private Vector3 overridePosition;
        public void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsServer) { Debug.LogError("PlayerNetworkMovementPrediction.SetOrientation() should only be called on the server!"); return; }
            //SetRotationClientRpc(newRotation, new ClientRpcParams() { Send = { TargetClientIds = { l } } });
            overridePosition = newPosition;
            applyOverridePosition = true;
            SetRotationClientRpc(newRotation, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { OwnerClientId } } });
        }

        private bool applyOverrideRotation;
        private Quaternion overrideRotation;
        [ClientRpc]
        private void SetRotationClientRpc(Quaternion newRotation, ClientRpcParams clientRpcParams = default)
        {
            movementHandler.SetCameraRotation(newRotation.eulerAngles.x, newRotation.eulerAngles.y);
            overrideRotation = newRotation;
            applyOverrideRotation = true;
        }

        public float playerObjectTeleportThreshold = 2;

        private const int BUFFER_SIZE = 1024;

        private int currentTick;
        private StatePayload[] stateBuffer;
        private InputPayload[] inputBuffer;
        private StatePayload latestServerState;
        private StatePayload lastProcessedState;
        private Queue<InputPayload> inputQueue;

        public Vector3 CurrentPosition { get; private set; }
        public Quaternion CurrentRotation { get; private set; }

        private PlayerMovementHandler movementHandler;

        private void Awake()
        {
            CurrentPosition = transform.position;
            CurrentRotation = transform.rotation;
        }

        private void Start()
        {
            movementHandler = GetComponent<PlayerMovementHandler>();
            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new InputPayload[BUFFER_SIZE];
            inputQueue = new Queue<InputPayload>();
        }

        public override void OnNetworkSpawn()
        {
            latestServerState = new StatePayload(0, CurrentPosition, CurrentRotation);

            if (IsServer)
                NetworkManager.NetworkTickSystem.Tick += HandleServerTick;
            if (IsClient)
                NetworkManager.NetworkTickSystem.Tick += HandleClientTick;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                NetworkManager.NetworkTickSystem.Tick -= HandleServerTick;
            if (IsClient)
                NetworkManager.NetworkTickSystem.Tick -= HandleClientTick;
        }

        private void HandleServerTick()
        {
            int bufferIndex = ProcessInputQueue();

            if (bufferIndex != -1)
            {
                SendStateToClientRpc(stateBuffer[bufferIndex]);
            }

            currentTick++;
        }

        [ClientRpc] private void SendStateToClientRpc(StatePayload statePayload) { latestServerState = statePayload; }

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

                InputPayload inputPayload = new InputPayload(currentTick, movementHandler.GetMoveInput(), transform.rotation);

                SendInputServerRpc(inputPayload);

                if (!IsHost)
                {
                    inputQueue.Enqueue(inputPayload);
                    ProcessInputQueue();
                }
            }
            else // If we are not the owner of this object
            {
                CurrentPosition = latestServerState.position;
                CurrentRotation = latestServerState.rotation;
            }

            // If we are the host, this is also called in the HandleServerTick() method
            if (!IsHost) { currentTick++; }
        }

        private int ProcessInputQueue()
        {
            int bufferIndex = -1;

            while (inputQueue.Count > 0)
            {
                InputPayload inputPayload = inputQueue.Dequeue();
                bufferIndex = inputPayload.tick % BUFFER_SIZE;

                inputBuffer[bufferIndex] = inputPayload;

                StatePayload statePayload = ProcessInput(inputPayload);
                CurrentPosition = statePayload.position;
                CurrentRotation = statePayload.rotation;

                stateBuffer[bufferIndex] = statePayload;
            }

            return bufferIndex;
        }

        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState;

            int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);

            if (positionError > 0.001f)
            {
                Debug.Log(OwnerClientId + " Position Error: " + positionError);

                CurrentPosition = latestServerState.position;
                CurrentRotation = latestServerState.rotation;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                int tickToProcess = latestServerState.tick + 1;

                Vector3 currentPositionCached = latestServerState.position;
                Quaternion currentRotationCached = latestServerState.rotation;
                while (tickToProcess < currentTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = ProcessInput(inputBuffer[bufferIndex]);
                    currentPositionCached = statePayload.position;
                    currentRotationCached = statePayload.rotation;

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }

                CurrentPosition = currentPositionCached;
                CurrentRotation = currentRotationCached;
            }
        }

        [ServerRpc] private void SendInputServerRpc(InputPayload inputPayload) { inputQueue.Enqueue(inputPayload); }

        private StatePayload ProcessInput(InputPayload input)
        {
            // Should always be in sync with same function on Client
            StatePayload statePayload = movementHandler.ProcessMovement(input);
            if (applyOverridePosition) { statePayload.position = overridePosition; applyOverridePosition = false; }
            if (applyOverrideRotation) { statePayload.rotation = overrideRotation; applyOverrideRotation = false; }
            movementHandler.SetTargetMovePosition(statePayload.position);
            return statePayload;
        }

        private void OnDrawGizmos()
        {
            //if (OwnerClientId == 0)
            //    Gizmos.color = Color.red;
            //else if (OwnerClientId == 1)
            //    Gizmos.color = Color.blue;
            //else if (OwnerClientId == 2)
            //    Gizmos.color = Color.green;
            //else
            //    Gizmos.color = Color.black;

            //Gizmos.DrawSphere(CurrentPosition, 0.25f);
        }
    }
}