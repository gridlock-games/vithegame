using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        private static float timer;

        void Update()
        {
            if (Physics.autoSimulation)
                return; // do nothing if the automatic simulation is enabled

            if (networkManager.IsClient)
                return;

            timer += Time.deltaTime;

            // Catch up with the game time.
            // Advance the physics simulation in portions of Time.fixedDeltaTime
            // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                List<RigidbodyData> rigidbodyDataBeforeSimulate = new List<RigidbodyData>();
                foreach (Rigidbody rb in allRigidbodies)
                {
                    rigidbodyDataBeforeSimulate.Add(new RigidbodyData(rb));
                }

                Physics.Simulate(Time.fixedDeltaTime);

                for (int i = 0; i < allRigidbodies.Length; i++)
                {
                    if (!excludedRigidbodies.Contains(allRigidbodies[i])) { continue; }

                    allRigidbodies[i].position = rigidbodyDataBeforeSimulate[i].position;
                    allRigidbodies[i].velocity = rigidbodyDataBeforeSimulate[i].velocity;
                    allRigidbodies[i].rotation = rigidbodyDataBeforeSimulate[i].rotation;
                    allRigidbodies[i].angularVelocity = rigidbodyDataBeforeSimulate[i].angularVelocity;

                    excludedRigidbodies.Remove(allRigidbodies[i]);
                }
            }

            // Here you can access the transforms state right after the simulation, if needed
        }

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

        public static List<Rigidbody> excludedRigidbodies = new List<Rigidbody>();

        public static void AddExcludedRigidbody(Rigidbody excludedRigidbody)
        {
            if (!excludedRigidbodies.Contains(excludedRigidbody))
            {
                excludedRigidbodies.Add(excludedRigidbody);
            }
        }

        public static void SimulateCertainObjects(Rigidbody[] rigidbodiesToSimulate)
        {
            Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<RigidbodyData> rigidbodyDataBeforeSimulate = new List<RigidbodyData>();
            foreach (Rigidbody rb in allRigidbodies)
            {
                rigidbodyDataBeforeSimulate.Add(new RigidbodyData(rb));
            }

            Physics.Simulate(Time.fixedDeltaTime);

            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (rigidbodiesToSimulate.Contains(allRigidbodies[i])) { continue; }

                allRigidbodies[i].position = rigidbodyDataBeforeSimulate[i].position;
                allRigidbodies[i].velocity = rigidbodyDataBeforeSimulate[i].velocity;
                allRigidbodies[i].rotation = rigidbodyDataBeforeSimulate[i].rotation;
                allRigidbodies[i].angularVelocity = rigidbodyDataBeforeSimulate[i].angularVelocity;
            }
        }
    }
}