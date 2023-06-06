using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace GameCreator.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class CustomNetworkTransform : NetworkBehaviour
    {
        private struct InputPayload : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
            }
        }

        private struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector3 position;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref position);
            }
        }

        private const string AXIS_H = "Horizontal";
        private const string AXIS_V = "Vertical";
        private const int BUFFER_SIZE = 1024;

        private int currentTick;
        private StatePayload[] stateBuffer;
        private InputPayload[] inputBuffer;
        private StatePayload latestServerState;
        private StatePayload lastProcessedState;
        private Queue<InputPayload> inputQueue;

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

            InputPayload inputPayload = new InputPayload() { tick = currentTick, inputVector = new Vector2(Input.GetAxisRaw(AXIS_H), Input.GetAxisRaw(AXIS_V)) };
            SendMoveInputServerRpc(inputPayload);
            inputQueue.Enqueue(inputPayload);

            int bufferIndex = ProcessInputQueue();

            currentTick++;
        }

        private int ProcessInputQueue()
        {
            int bufferIndex = -1;

            if (inputQueue.Count == 0)
            {
                //InputPayload inputPayload = new InputPayload()
                //{
                //    tick = currentTick,
                //    inputVector = currentMoveInput
                //};

                //bufferIndex = inputPayload.tick % BUFFER_SIZE;

                //inputBuffer[bufferIndex] = inputPayload;
                //StatePayload statePayload = ProcessMovement(inputPayload);
                //stateBuffer[bufferIndex] = statePayload;
            }

            while (inputQueue.Count > 0)
            {
                InputPayload inputPayload = inputQueue.Dequeue();

                bufferIndex = inputPayload.tick % BUFFER_SIZE;

                inputBuffer[bufferIndex] = inputPayload;
                StatePayload statePayload = ProcessMovement(inputPayload);
                stateBuffer[bufferIndex] = statePayload;
            }

            return bufferIndex;
        }

        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState;

            int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
            //Debug.Log(latestServerState.tick + " " + stateBuffer[serverStateBufferIndex].tick + " " + serverStateBufferIndex + " " + latestServerState.position + " " + stateBuffer[serverStateBufferIndex].position);
            float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);

            if (positionError > 0.001f)
            {
                Debug.Log("Positions out of sync " + positionError);

                transform.position = latestServerState.position;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                int tickToProcess = latestServerState.tick + 1;

                while (tickToProcess < currentTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex]);

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
            }
        }

        private void Start()
        {
            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new InputPayload[BUFFER_SIZE];
            inputQueue = new Queue<InputPayload>();
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

        private StatePayload ProcessMovement(InputPayload input)
        {
            // Should always be in sync with same function on Client
            transform.position += 1f / NetworkManager.NetworkTickSystem.TickRate * 3f * new Vector3(input.inputVector.x, 0, input.inputVector.y);

            return new StatePayload()
            {
                tick = input.tick,
                position = transform.position,
            };
        }
    }
}