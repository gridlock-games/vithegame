using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
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