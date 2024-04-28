using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class ActionVFXParticleSystem : ActionVFX
    {
        [SerializeField] private bool shouldUseAttackerPositionForHitAngles;

        private Attributes attacker;
        private ActionClip attack;

        public void InitializeVFX(Attributes attacker, ActionClip attack)
        {
            this.attacker = attacker;
            this.attack = attack;
        }

        private ParticleSystem[] particleSystems;
        private void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps.trigger.enabled)
                {
                    ps.gameObject.AddComponent<ActionVFXChildTriggerParticleSystem>();
                }
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on particle systems are triggers! " + this); }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            foreach (ParticleSystem ps in particleSystems)
            {
                bool skip = false;
                for (int i = 0; i < ps.trigger.colliderCount; i++)
                {
                    if (ps.trigger.GetCollider(i) == other)
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                {
                    ps.trigger.AddCollider(other);
                }
            }
        }

        private Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter = new Dictionary<Attributes, RuntimeWeapon.HitCounterData>();

        public void ProcessOnParticleEnterMessage(ParticleSystem ps)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter, out ParticleSystem.ColliderData enterColliderData);

            for (int particleIndex = 0; particleIndex < numEnter; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < enterColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)enterColliderData.GetCollider(particleIndex, colliderIndex);
                    if (col.TryGetComponent(out NetworkCollider networkCollider))
                    {
                        if (networkCollider.Attributes)
                        {
                            bool canHit = true;
                            if (hitCounter.ContainsKey(networkCollider.Attributes))
                            {
                                if (hitCounter[networkCollider.Attributes].hitNumber >= attacker.GetComponent<WeaponHandler>().CurrentActionClip.maxHitLimit) { canHit = false; }
                                if (Time.time - hitCounter[networkCollider.Attributes].timeOfHit < attacker.GetComponent<WeaponHandler>().CurrentActionClip.GetTimeBetweenHits()) { canHit = false; }
                            }

                            if (canHit)
                            {
                                if (networkCollider.Attributes.ProcessProjectileHit(attacker, null, hitCounter, attack, col.ClosestPointOnBounds(enter[particleIndex].position), shouldUseAttackerPositionForHitAngles ? attacker.transform.position : transform.position))
                                {
                                    if (!hitCounter.ContainsKey(networkCollider.Attributes))
                                    {
                                        hitCounter.Add(networkCollider.Attributes, new(1, Time.time));
                                    }
                                    else
                                    {
                                        hitCounter[networkCollider.Attributes] = new(hitCounter[networkCollider.Attributes].hitNumber + 1, Time.time);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside, out ParticleSystem.ColliderData insideColliderData);

            for (int particleIndex = 0; particleIndex < numInside; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < insideColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)insideColliderData.GetCollider(particleIndex, colliderIndex);
                    if (col.TryGetComponent(out NetworkCollider networkCollider))
                    {
                        if (networkCollider.Attributes)
                        {
                            bool canHit = true;
                            if (hitCounter.ContainsKey(networkCollider.Attributes))
                            {
                                if (hitCounter[networkCollider.Attributes].hitNumber >= attacker.GetComponent<WeaponHandler>().CurrentActionClip.maxHitLimit) { canHit = false; }
                                if (Time.time - hitCounter[networkCollider.Attributes].timeOfHit < attacker.GetComponent<WeaponHandler>().CurrentActionClip.GetTimeBetweenHits()) { canHit = false; }
                            }

                            if (canHit)
                            {
                                if (networkCollider.Attributes.ProcessProjectileHit(attacker, null, hitCounter, attack, col.ClosestPointOnBounds(inside[particleIndex].position), shouldUseAttackerPositionForHitAngles ? attacker.transform.position : transform.position))
                                {
                                    if (!hitCounter.ContainsKey(networkCollider.Attributes))
                                    {
                                        hitCounter.Add(networkCollider.Attributes, new(1, Time.time));
                                    }
                                    else
                                    {
                                        hitCounter[networkCollider.Attributes] = new(hitCounter[networkCollider.Attributes].hitNumber + 1, Time.time);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
        }
    }
}