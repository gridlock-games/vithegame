using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace GameCreator.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterNetworkTransform : NetworkBehaviour
    {
        private struct InputPayload : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;
            public Quaternion rotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref rotation);
            }
        }

        private struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector3 position;
            public Quaternion rotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
            }
        }

        //public Vector3 CurrentPosition { get; private set; }
        //public Quaternion CurrentRotation { get; private set; }
        private Vector3 CurrentPosition;
        private Quaternion CurrentRotation;

        private const string AXIS_H = "Horizontal";
        private const string AXIS_V = "Vertical";
        private const int BUFFER_SIZE = 1024;

        public int currentTick { get; private set; }
        private StatePayload[] stateBuffer;
        private InputPayload[] inputBuffer;
        private StatePayload latestServerState;
        private StatePayload lastProcessedState;
        private Queue<InputPayload> inputQueue;

        private PlayerCharacter playerCharacter;

        public override void OnNetworkSpawn()
        {
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
            if (!latestServerState.Equals(default(StatePayload)) &&
                (lastProcessedState.Equals(default(StatePayload)) ||
                !latestServerState.Equals(lastProcessedState)))
            {
                HandleServerReconciliation();
            }

            if (IsOwner)
            {
                InputPayload inputPayload = new InputPayload()
                {
                    tick = currentTick,
                    inputVector = new Vector2(Input.GetAxisRaw(AXIS_H), Input.GetAxisRaw(AXIS_V)),
                    rotation = transform.rotation
                };
                SendMoveInputServerRpc(inputPayload);
                inputQueue.Enqueue(inputPayload);
            }

            ProcessInputQueue();

            currentTick++;
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
                Debug.Log(positionError);

                playerCharacter.characterLocomotion.characterController.enabled = false;
                transform.position = latestServerState.position;
                CurrentPosition = latestServerState.position;
                playerCharacter.characterLocomotion.characterController.enabled = true;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                int tickToProcess = latestServerState.tick + 1;

                while (tickToProcess < currentTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = ProcessInput(inputBuffer[bufferIndex]);

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
            }

            //float angleError = Quaternion.Angle(latestServerState.rotation, stateBuffer[serverStateBufferIndex].rotation);

            //if (angleError > 0.001f)
            //{
            //    Debug.Log("Angle Error: " + angleError);

            //    CurrentRotation = latestServerState.rotation;
            //    transform.position = latestServerState.position;
            //    transform.rotation = latestServerState.rotation;

            //    // Update buffer at index of latest server state
            //    stateBuffer[serverStateBufferIndex] = latestServerState;

            //    // Now re-simulate the rest of the ticks up to the current tick on the client
            //    int tickToProcess = latestServerState.tick + 1;

            //    while (tickToProcess < currentTick)
            //    {
            //        int bufferIndex = tickToProcess % BUFFER_SIZE;

            //        // Process new movement with reconciled state
            //        StatePayload statePayload = ProcessInput(inputBuffer[bufferIndex]);

            //        // Update buffer with recalculated state
            //        stateBuffer[bufferIndex] = statePayload;

            //        tickToProcess++;
            //    }
            //}
        }

        private void Start()
        {
            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new InputPayload[BUFFER_SIZE];
            inputQueue = new Queue<InputPayload>();
            playerCharacter = GetComponent<PlayerCharacter>();
        }

        [ServerRpc]
        private void SendMoveInputServerRpc(InputPayload inputPayload)
        {
            inputQueue.Enqueue(inputPayload);

            // Send input to all clients that aren't the owner of this object
            List<ulong> clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            clientIds.Remove(OwnerClientId);
            SendMoveInputClientRpc(inputPayload, new ClientRpcParams() { Send = { TargetClientIds = clientIds } });
        }

        [ClientRpc] private void SendMoveInputClientRpc(InputPayload inputPayload, ClientRpcParams clientRpcParams) { inputQueue.Enqueue(inputPayload); }

        private StatePayload ProcessInput(InputPayload input)
        {
            // Should always be in sync with same function on Client
            KeyValuePair<Vector3, Quaternion> transformData = playerCharacter.ProcessMovement(new Vector3(input.inputVector.x, 0, input.inputVector.y), input.rotation);
            CurrentPosition = transformData.Key;
            CurrentRotation = transformData.Value;

            return new StatePayload()
            {
                tick = input.tick,
                position = CurrentPosition,
                rotation = CurrentRotation
            };
        }

        //private void Update()
        //{
        //    Debug.Log(currentTick + " " + transform.position + " " + transform.eulerAngles);
        //}

        private void LateUpdate()
        {
            if (!IsOwner) return;
        }
    }
}