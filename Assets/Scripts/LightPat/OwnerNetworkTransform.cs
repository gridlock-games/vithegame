using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class OwnerNetworkTransform : NetworkBehaviour
    {
        public bool interpolate = true;
        [Range(0.001f, 1)]
        public float positionThreshold = 0.001f;
        [Range(0.001f, 360)]
        public float rotAngleThreshold = 0.001f;

        private NetworkVariable<int> transformParentId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> currentScale = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        float positionSpeed;
        float rotationSpeed;

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
            currentPosition.OnValueChanged += OnPositionChanged;
            currentRotation.OnValueChanged += OnRotationChanged;
        }

        public override void OnNetworkDespawn()
        {
            transformParentId.OnValueChanged -= OnTransformParentIdChange;
            currentPosition.OnValueChanged -= OnPositionChanged;
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

        void OnPositionChanged(Vector3 prevPosition, Vector3 newPosition)
        {
            if (!interpolate)
            {
                transform.localPosition = currentPosition.Value;
            }
            else
            {
                positionSpeed = Vector3.Distance(transform.localPosition, currentPosition.Value);
                if (positionSpeed < 10)
                    positionSpeed = 10;
            }
        }

        void OnRotationChanged(Quaternion prevRotation, Quaternion newRotation)
        {
            rotationSpeed = Quaternion.Angle(transform.localRotation, newRotation);
            if (!interpolate)
                transform.localRotation = currentRotation.Value;
        }

        Vector3 lastPosition;
        Quaternion lastRotation;
        private void LateUpdate()
        {
            if (!IsSpawned) { return; }

            if (IsOwner)
            {
                if (Vector3.Distance(transform.localPosition, currentPosition.Value) > positionThreshold)
                    currentPosition.Value = transform.localPosition;
                if (Quaternion.Angle(transform.localRotation, currentRotation.Value) > rotAngleThreshold)
                    currentRotation.Value = transform.localRotation;
                if (transform.localScale != currentScale.Value)
                    currentScale.Value = transform.localScale;
            }
            else
            {
                if (interpolate)
                {
                    if (transformParentId.Value == -1)
                    {
                        transform.localPosition = Vector3.Lerp(lastPosition, currentPosition.Value, Time.deltaTime * positionSpeed);
                        transform.localRotation = Quaternion.Slerp(lastRotation, currentRotation.Value, Time.deltaTime * rotationSpeed);
                    }
                    else
                    {
                        Transform parent = NetworkManager.SpawnManager.SpawnedObjects[(ulong) transformParentId.Value].transform;

                        //transform.localPosition = parent.position + Vector3.Lerp(lastPosition, currentPosition.Value, Time.deltaTime * positionSpeed);
                        //transform.localRotation = parent.rotation * Quaternion.Slerp(lastRotation, currentRotation.Value, Time.deltaTime * rotationSpeed);
                        transform.localPosition = Vector3.Lerp(lastPosition, currentPosition.Value, Time.deltaTime * positionSpeed);
                        transform.localRotation = Quaternion.Slerp(lastRotation, currentRotation.Value, Time.deltaTime * rotationSpeed);
                    }
                }
                else
                {
                    if (transformParentId.Value == -1)
                    {
                        transform.localPosition = currentPosition.Value;
                        transform.localRotation = currentRotation.Value;
                    }
                    else
                    {
                        Transform parent = NetworkManager.SpawnManager.SpawnedObjects[(ulong)transformParentId.Value].transform;

                        //transform.localPosition = parent.position + currentPosition.Value;
                        //transform.localRotation = parent.rotation * currentRotation.Value;
                        transform.localPosition = currentPosition.Value;
                        transform.localRotation = currentRotation.Value;
                    }
                }
                // If we are not the owner
                lastPosition = transform.localPosition;
                lastRotation = transform.localRotation;

                transform.localScale = currentScale.Value;
            }
        }
    }
}