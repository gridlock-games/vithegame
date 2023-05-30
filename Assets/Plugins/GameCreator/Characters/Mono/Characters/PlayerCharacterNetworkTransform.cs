using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterNetworkTransform : NetworkBehaviour
    {
        public bool interpolate = true;
        [Range(0.001f, 1)]
        public float positionNetworkSendThreshold = 0.001f;
        [Range(0.001f, 360)]
        public float rotationAngleNetworkSendThreshold = 0.001f;
        [Range(1, 100)]
        public int positionTeleportThreshold = 10;

        private NetworkVariable<int> transformParentId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> currentScale = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        float rotationSpeed = 10;

        public Vector3 GetPosition() { return currentPosition.Value; }
        public Quaternion GetRotation() { return currentRotation.Value; }
        public Vector3 GetScale() { return currentScale.Value; }

        public void SetParent(NetworkObject newParent)
        {
            if (newParent == null)
                transformParentId.Value = -1;
            else
                transformParentId.Value = (int)newParent.NetworkObjectId;
        }

        public override void OnNetworkSpawn()
        {
            transformParentId.OnValueChanged += OnTransformParentIdChange;
            currentRotation.OnValueChanged += OnRotationChanged;
        }

        public override void OnNetworkDespawn()
        {
            transformParentId.OnValueChanged -= OnTransformParentIdChange;
            currentRotation.OnValueChanged -= OnRotationChanged;
        }

        void OnTransformParentIdChange(int previous, int current)
        {
            if (previous != -1)
            {
                NetworkObject oldParent = NetworkManager.SpawnManager.SpawnedObjects[(ulong)previous];
                foreach (Collider c in oldParent.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider thisCol in GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c, thisCol, false);
                    }
                }
            }

            if (current != -1)
            {
                NetworkObject newParent = NetworkManager.SpawnManager.SpawnedObjects[(ulong)current];
                foreach (Collider c in newParent.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider thisCol in GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c, thisCol, true);
                    }
                }

                transform.SetParent(newParent.transform, true);
            }
            else
            {
                transform.SetParent(null, true);
            }
        }

        void OnRotationChanged(Quaternion prevRotation, Quaternion newRotation)
        {
            rotationSpeed = Quaternion.Angle(transform.localRotation, newRotation);
            if (!interpolate)
                transform.localRotation = currentRotation.Value;
        }

        Quaternion lastRotation;
        private void LateUpdate()
        {
            if (!IsSpawned) { return; }

            // Rotation Syncing
            if (IsOwner)
            {
                if (Quaternion.Angle(transform.localRotation, currentRotation.Value) > rotationAngleNetworkSendThreshold)
                    currentRotation.Value = transform.localRotation;
            }
            else // If we are not the owner
            {
                if (interpolate)
                {
                    if (transformParentId.Value == -1) { transform.localRotation = Quaternion.Slerp(lastRotation, currentRotation.Value, Time.deltaTime * rotationSpeed); }
                    else { transform.localRotation = Quaternion.Slerp(lastRotation, currentRotation.Value, Time.deltaTime * rotationSpeed); }
                }
                else
                {
                    if (transformParentId.Value == -1) { transform.localRotation = currentRotation.Value; }
                    else { transform.localRotation = currentRotation.Value; }
                }
                lastRotation = transform.localRotation;

                transform.localScale = currentScale.Value;
            }

            // Position Syncing
            if (IsServer)
            {
                if (Vector3.Distance(transform.localPosition, currentPosition.Value) > positionNetworkSendThreshold)
                    currentPosition.Value = transform.localPosition;
                if (transform.localScale != currentScale.Value)
                    currentScale.Value = transform.localScale;
            }
            else // If we are not the server
            {
                float localDistance = Vector3.Distance(transform.position, currentPosition.Value);
                if (localDistance > positionTeleportThreshold)
                {
                    if (IsOwner)
                        Debug.Log(Time.time + "Teleporting character: " + OwnerClientId + " - " + name);
                    transform.localPosition = currentPosition.Value;
                }

                transform.localScale = currentScale.Value;
            }
        }
    }
}