using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Utility
{
    public class NetworkPhysicsSimulation : MonoBehaviour
    {
        private struct RigidbodyData
        {
            private Rigidbody rb;
            private Vector3 position;
            private Quaternion rotation;
            private Vector3 velocity;
            private Vector3 angularVelocity;

            public RigidbodyData(Rigidbody rb)
            {
                this.rb = rb;
                position = rb.position;
                rotation = rb.rotation;
                velocity = rb.linearVelocity;
                angularVelocity = rb.angularVelocity;
            }

            public void ApplyDataToBody()
            {
                rb.position = position;
                rb.rotation = rotation;
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = velocity;
                    rb.angularVelocity = angularVelocity;
                }
            }
        }

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
            if (!activeRigidbodies.Remove(rb))
            {
                Debug.LogWarning("Trying to remove a rigidbody " + rb.name + " but it is not present in list");
            }
        }

        public static void SimulateOneRigidbody(Rigidbody rigidbodyToSimulate, bool changeSimulationMode = true)
        {
            List<RigidbodyData> rigidbodyDataBeforeSimulation = new List<RigidbodyData>();
            foreach (Rigidbody rb in activeRigidbodies)
            {
                if (!rb) { Debug.LogWarning("There is a null rigidbody in the rigidbody list"); continue; }
                if (rb != rigidbodyToSimulate)
                {
                    rigidbodyDataBeforeSimulation.Add(new RigidbodyData(rb));
                    rb.Sleep();
                }
            }

            List<ParticleSystemData> particleSystemDataBeforeSimulation = new List<ParticleSystemData>();
            foreach (ParticleSystem ps in activeParticleSystems)
            {
                particleSystemDataBeforeSimulation.Add(new ParticleSystemData(ps));
            }

            if (changeSimulationMode) { Physics.simulationMode = SimulationMode.Script; }
            Physics.Simulate(Time.fixedDeltaTime);
            if (changeSimulationMode) { Physics.simulationMode = SimulationMode.FixedUpdate; }

            foreach (RigidbodyData rigidbodyData in rigidbodyDataBeforeSimulation)
            {
                rigidbodyData.ApplyDataToBody();
            }

            foreach (ParticleSystemData particleSystemData in particleSystemDataBeforeSimulation)
            {
                particleSystemData.ApplyDataToParticleSystem();
            }
        }

        private struct ParticleSystemData
        {
            public ParticleSystem ps;
            public float time;

            public ParticleSystemData(ParticleSystem particleSystem)
            {
                ps = particleSystem;
                time = particleSystem.time;
            }

            public void ApplyDataToParticleSystem()
            {
                ps.time = time;
            }
        }

        private static List<ParticleSystem> activeParticleSystems = new List<ParticleSystem>();
        public static void AddParticleSystem(ParticleSystem ps)
        {
            if (activeParticleSystems.Contains(ps))
            {
                Debug.LogWarning("Trying to add a particle system " + ps.name + " but it is already present in list");
            }
            else
            {
                activeParticleSystems.Add(ps);
            }
        }

        public static void RemoveParticleSystem(ParticleSystem ps)
        {
            if (!activeParticleSystems.Remove(ps))
            {
                Debug.LogWarning("Trying to remove a particle system " + ps.name + " but it is not present in list");
            }
        }
    }
}