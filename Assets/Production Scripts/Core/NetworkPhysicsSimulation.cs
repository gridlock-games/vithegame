using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        public static void AddStepsProcessed(int numSteps)
        {
            timer -= Time.fixedDeltaTime * numSteps;
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
                Physics.Simulate(Time.fixedDeltaTime);
            }

            // Here you can access the transforms state right after the simulation, if needed
        }
    }
}