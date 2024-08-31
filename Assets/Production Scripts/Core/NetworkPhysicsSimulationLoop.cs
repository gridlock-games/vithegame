using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class NetworkPhysicsSimulationLoop : MonoBehaviour
    {
        private NetworkManager networkManager;
        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        private float timer;
        private void Update()
        {
            //if (networkManager.NetworkTickSystem == null)
            //{
            //    Physics.autoSimulation = true;

            //    timer = 0;
            //}
            //else
            //{
            //    Physics.autoSimulation = false;

            //    timer += Time.deltaTime;

            //    // Catch up with the game time.
            //    // Advance the physics simulation in portions of Time.fixedDeltaTime
            //    // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
            //    while (timer >= Time.fixedDeltaTime)
            //    {
            //        timer -= Time.fixedDeltaTime;
            //        Physics.Simulate(Time.fixedDeltaTime);
            //    }

            //    // Here you can access the transforms state right after the simulation, if needed
            //}
        }
    }
}