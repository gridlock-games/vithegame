using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Utility
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
        private static List<Rigidbody> activeRigidbodies = new List<Rigidbody>();

        public static void AddRigidbody(Rigidbody rb)
        {
            if (activeRigidbodies.Contains(rb))
            {
                Debug.LogWarning("Trying to add a rigidbody " + rb.name + " but it is already present in list");
            }
            else
            {
                activeRigidbodies.Add(rb);
            }
        }

        public static void RemoveRigidbody(Rigidbody rb)
        {
            if (activeRigidbodies.Contains(rb))
            {
                activeRigidbodies.Remove(rb);
            }
            else
            {
                Debug.LogWarning("Trying to remove a rigidbody " + rb.name + " but it is not present in list");
            }
        }

        public static void SimulateOneRigidbody(Rigidbody rigidbodyToSimulate)
        {
            foreach (Rigidbody rb in activeRigidbodies)
            {
                if (!rb) { Debug.LogWarning("There is a null rigidbody in the rigidbody list"); continue; }
                if (rb != rigidbodyToSimulate) { rb.Sleep(); }
            }

            Physics.autoSimulation = false;
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
        }
    }
}