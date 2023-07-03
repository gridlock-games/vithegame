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
        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public bool initialized;
            public bool isControllable;
            public Vector2 inputVector;
            public Quaternion rotation;
            public PlayerCharacter.RootMotionResult rootMotionResult;
            
            public InputPayload(int tick, bool isControllable, Vector2 inputVector, Quaternion rotation, PlayerCharacter.RootMotionResult rootMotionResult)
            {
                this.tick = tick;
                initialized = true;
                this.isControllable = isControllable;
                this.inputVector = inputVector;
                this.rotation = rotation;
                this.rootMotionResult = rootMotionResult;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref isControllable);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref initialized);

                if (isControllable)
                {
                    serializer.SerializeValue(ref inputVector);
                }

                serializer.SerializeNetworkSerializable(ref rootMotionResult);
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

        public float playerObjectTeleportThreshold = 2;

        public Vector3 currentPosition { get; private set; }
        public Quaternion currentRotation { get; private set; }

        private const string AXIS_H = "Horizontal";
        private const string AXIS_V = "Vertical";
        private const int BUFFER_SIZE = 1024;

        private int currentTick;
        private StatePayload[] stateBuffer;
        private InputPayload[] inputBuffer;
        private StatePayload latestServerState;
        private StatePayload lastProcessedState;
        private Queue<InputPayload> inputQueue;

        private PlayerCharacter playerCharacter;
        private Vector3 queuedPositionOverwrite;
        private bool overwritePosition;
        public void SetPosition(Vector3 newPosition)
        {
            if (!IsServer) { Debug.LogError("SetPosition() needs to be called from the server"); return; }

            queuedPositionOverwrite = newPosition;
            overwritePosition = true;
        }

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
            if (IsOwner)
            {
                if (!latestServerState.Equals(default(StatePayload)) &&
                (lastProcessedState.Equals(default(StatePayload)) ||
                !latestServerState.Equals(lastProcessedState)))
                {
                    HandleServerReconciliation();
                }

                InputPayload inputPayload = new InputPayload(currentTick, playerCharacter.IsControllable(), new Vector2(Input.GetAxisRaw(AXIS_H), Input.GetAxisRaw(AXIS_V)), transform.rotation, playerCharacter.RootMotionTickUpdate(currentTick));
                
                // If we are in the middle of root motion, do not take an input vector
                if (playerCharacter.characterLocomotion.currentLocomotionSystem.isRootMoving)
                {
                    inputPayload.inputVector = Vector2.zero;
                }

                SendInputServerRpc(inputPayload);

                if (!IsHost)
                {
                    inputQueue.Enqueue(inputPayload);
                    ProcessInputQueue();
                }
            }
            else // If we are not the owner of this object
            {
                currentPosition = latestServerState.position;
                currentRotation = latestServerState.rotation;
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
                //Debug.Log(OwnerClientId + " Position Error: " + positionError);

                currentPosition = latestServerState.position;
                currentRotation = latestServerState.rotation;

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
        }

        private void Start()
        {
            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new InputPayload[BUFFER_SIZE];
            inputQueue = new Queue<InputPayload>();
            playerCharacter = GetComponent<PlayerCharacter>();
        }

        [ServerRpc] private void SendInputServerRpc(InputPayload inputPayload) { inputQueue.Enqueue(inputPayload); }

        private StatePayload ProcessInput(InputPayload input)
        {
            // Should always be in sync with same function on Client
            StatePayload statePayload = playerCharacter.ProcessMovement(input);

            if (overwritePosition)
            {
                statePayload.position = queuedPositionOverwrite;
                overwritePosition = false;
                playerCharacter.characterLocomotion.characterController.enabled = false;
                transform.position = statePayload.position;
                playerCharacter.characterLocomotion.characterController.enabled = true;
            }

            currentPosition = statePayload.position;
            currentRotation = statePayload.rotation;
            return statePayload;
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

            Gizmos.DrawSphere(currentPosition, 0.5f);
        }
    }
}