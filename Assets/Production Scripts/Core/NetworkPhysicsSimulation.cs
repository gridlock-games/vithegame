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

        private void FixedUpdate()
        {
            if (Physics.autoSimulation)
                return; // do nothing if the automatic simulation is enabled

            if (networkManager.IsClient)
                return;

            timer += Time.fixedDeltaTime;

            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }
    }
}