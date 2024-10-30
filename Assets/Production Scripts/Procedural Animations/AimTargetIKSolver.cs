using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        private NetworkObject networkObject;
        private void OnEnable()
        {
            networkObject = transform.root.GetComponent<NetworkObject>();
        }

        private void OnDisable()
        {
            networkObject = null;
        }

        private void Update()
        {
            if (!networkObject) { return; }

            if (!networkObject.IsSpawned)
            {
                transform.localPosition = new Vector3(0, 1, 2);
            }
        }
    }
}