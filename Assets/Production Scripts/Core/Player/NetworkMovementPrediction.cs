using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Player
{
    [RequireComponent(typeof(MovementHandler))]
    public class NetworkMovementPrediction : NetworkBehaviour
    {
        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public bool initialized;
            public bool isControllable;
            public Vector2 inputVector;
            public Quaternion rotation;
            //public PlayerCharacter.RootMotionResult rootMotionResult;

            public InputPayload(int tick, bool isControllable, Vector2 inputVector, Quaternion rotation) // , PlayerCharacter.RootMotionResult rootMotionResult
            {
                this.tick = tick;
                initialized = true;
                this.isControllable = isControllable;
                this.inputVector = inputVector;
                this.rotation = rotation;
                //this.rootMotionResult = rootMotionResult;
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

                //serializer.SerializeNetworkSerializable(ref rootMotionResult);
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

        private MovementHandler movementHandler;

        private void Start()
        {
            movementHandler = GetComponent<MovementHandler>();
        }

        public override void OnNetworkSpawn()
        {

        }

        public override void OnNetworkDespawn()
        {

        }
    }
}