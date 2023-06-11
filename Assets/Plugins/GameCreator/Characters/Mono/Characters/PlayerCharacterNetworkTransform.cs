using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using System;

namespace GameCreator.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterNetworkTransform : NetworkBehaviour
    {
        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;
            public Quaternion rotation;
            public SampledAnimationCurve rootMotionCurve;
            
            public InputPayload(int tick, Vector2 inputVector, Quaternion rotation, AnimationCurve curve)
            {
                this.tick = tick;
                this.inputVector = inputVector;
                this.rotation = rotation;
                rootMotionCurve = new SampledAnimationCurve(curve, 100);
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeNetworkSerializable(ref rootMotionCurve);
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

        public struct SampledAnimationCurve : INetworkSerializable, IDisposable
        {
            public NativeArray<float> sampledAnimationCurve;
            public bool animationCurveExists;

            /// <param name="samples">Must be 2 or higher</param>
            public SampledAnimationCurve(AnimationCurve ac, int samples)
            {
                sampledAnimationCurve = new NativeArray<float>(samples, Allocator.Persistent);

                animationCurveExists = ac != null;
                if (!animationCurveExists) { return; }

                animationCurveExists = ac.keys.Length >= 2;
                if (!animationCurveExists) { return; }

                float timeFrom = ac.keys[0].time;
                float timeTo = ac.keys[^1].time;
                float timeStep = (timeTo - timeFrom) / (samples - 1);

                for (int i = 0; i < samples; i++)
                {
                    sampledAnimationCurve[i] = ac.Evaluate(timeFrom + (i * timeStep));
                }
            }

            public void Dispose()
            {
                sampledAnimationCurve.Dispose();
            }

            /// <param name="time">Must be from 0 to 1</param>
            public float EvaluateLerp(float time)
            {
                int len = sampledAnimationCurve.Length - 1;
                float clamp01 = time < 0 ? 0 : (time > 1 ? 1 : time);
                float floatIndex = (clamp01 * len);
                int floorIndex = (int)math.floor(floatIndex);
                if (floorIndex == len)
                {
                    return sampledAnimationCurve[len];
                }

                float lowerValue = sampledAnimationCurve[floorIndex];
                float higherValue = sampledAnimationCurve[floorIndex + 1];
                return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref animationCurveExists);

                if (!animationCurveExists) { return; }

                // Length
                int length = 0;
                if (!serializer.IsReader)
                {
                    length = sampledAnimationCurve.Length;
                }

                serializer.SerializeValue(ref length);

                // Array
                if (serializer.IsReader)
                {
                    if (sampledAnimationCurve.IsCreated)
                    {
                        // Make sure the existing array is disposed and not leaked
                        sampledAnimationCurve.Dispose();
                    }
                    sampledAnimationCurve = new NativeArray<float>(length, Allocator.Persistent);
                }

                for (int n = 0; n < length; ++n)
                {
                    // NataveArray doesn't have a by-ref index operator
                    // so we have to read, serialize, write. This works in both
                    // reading and writing contexts - in reading, `val` gets overwritten
                    // so the current value doesn't matter; in writing, `val` is unchanged,
                    // so Array[n] = val is the same as Array[n] = Array[n].
                    // NativeList also exists which does have a by-ref `ElementAt()` method.
                    var val = sampledAnimationCurve[n];
                    serializer.SerializeValue(ref val);
                    sampledAnimationCurve[n] = val;
                }
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

        public bool sendCurve;
        public AnimationCurve testCurve;
        private AnimationCurve nullCurve;
        private void HandleClientTick()
        {
            if (!latestServerState.Equals(default(StatePayload)) &&
                (lastProcessedState.Equals(default(StatePayload)) ||
                !latestServerState.Equals(lastProcessedState)))
            {
                //HandleServerReconciliation();
            }

            if (IsOwner)
            {
                InputPayload inputPayload = new InputPayload(currentTick,
                    new Vector2(Input.GetAxisRaw(AXIS_H), Input.GetAxisRaw(AXIS_V)),
                    transform.rotation,
                    sendCurve ? nullCurve : testCurve);
                SendInputServerRpc(inputPayload);
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
                Debug.Log("Position Error: " + positionError);

                playerCharacter.characterLocomotion.characterController.enabled = false;
                transform.position = latestServerState.position;
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
        private void SendInputServerRpc(InputPayload inputPayload)
        {
            if (!IsHost)
                inputQueue.Enqueue(inputPayload);
            Debug.Log(inputPayload.rootMotionCurve.animationCurveExists);
            // Send input to all clients that aren't the owner of this object
            List<ulong> clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            clientIds.Remove(OwnerClientId);
            SendInputClientRpc(inputPayload, new ClientRpcParams() { Send = { TargetClientIds = clientIds } });
        }

        [ClientRpc] private void SendInputClientRpc(InputPayload inputPayload, ClientRpcParams clientRpcParams) { inputQueue.Enqueue(inputPayload); }

        private StatePayload ProcessInput(InputPayload input)
        {
            // Should always be in sync with same function on Client
            return playerCharacter.ProcessMovement(input);
        }
    }
}