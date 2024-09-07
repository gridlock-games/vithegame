using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
        private struct RigidbodyData
        {
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
            public Vector3 angularVelocity;

            public RigidbodyData(Rigidbody rb)
            {
                position = rb.position;
                velocity = rb.velocity;
                rotation = rb.rotation;
                angularVelocity = rb.angularVelocity;
            }
        }

        public static void SimulateCertainObjects(Rigidbody[] rigidbodiesToSimulate)
        {
            Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Rigidbody rb in allRigidbodies)
            {
                if (!rigidbodiesToSimulate.Contains(rb)) { rb.Sleep(); }
            }

            Physics.autoSimulation = false;
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
        }
    }
}