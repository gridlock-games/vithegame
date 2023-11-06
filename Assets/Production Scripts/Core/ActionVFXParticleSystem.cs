using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(Rigidbody))]
    public class ActionVFXParticleSystem : ActionVFX
    {
        private Attributes attacker;
        private ActionClip attack;

        public void InitializeVFX(Attributes attacker, ActionClip attack)
        {
            this.attacker = attacker;
            this.attack = attack;
        }

        private ParticleSystem ps;
        private void Start()
        {
            ps = GetComponent<ParticleSystem>();
            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on particle systems are triggers! " + this); }
            }
        }

        private void OnTriggerEnter(Collider other)
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

        private Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter = new Dictionary<Attributes, RuntimeWeapon.HitCounterData>();

        private void OnParticleTrigger()
        {
            List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter, out ParticleSystem.ColliderData enterColliderData);

            for (int particleIndex = 0; particleIndex < numEnter; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < enterColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)enterColliderData.GetCollider(particleIndex, colliderIndex);
                    Attributes attributes = col.GetComponentInParent<Attributes>();
                    if (attributes == attacker) { continue; }
                    if (attributes)
                    {
                        bool canHit = true;
                        if (hitCounter.ContainsKey(attributes))
                        {
                            if (hitCounter[attributes].hitNumber >= attacker.GetComponent<WeaponHandler>().CurrentActionClip.maxHitLimit) { canHit = false; }
                            if (Time.time - hitCounter[attributes].timeOfHit < attacker.GetComponent<WeaponHandler>().CurrentActionClip.timeBetweenHits) { canHit = false; }
                        }

                        if (canHit)
                        {
                            if (attributes.ProcessProjectileHit(attacker, attack, col.ClosestPointOnBounds(enter[particleIndex].position), transform.position))
                            {
                                if (!hitCounter.ContainsKey(attributes))
                                {
                                    hitCounter.Add(attributes, new(1, Time.time));
                                }
                                else
                                {
                                    hitCounter[attributes] = new(hitCounter[attributes].hitNumber, Time.time);
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
                    Attributes attributes = col.GetComponentInParent<Attributes>();
                    if (attributes == attacker) { continue; }
                    if (attributes)
                    {
                        bool canHit = true;
                        if (hitCounter.ContainsKey(attributes))
                        {
                            if (hitCounter[attributes].hitNumber >= attacker.GetComponent<WeaponHandler>().CurrentActionClip.maxHitLimit) { canHit = false; }
                            if (Time.time - hitCounter[attributes].timeOfHit < attacker.GetComponent<WeaponHandler>().CurrentActionClip.timeBetweenHits) { canHit = false; }
                        }

                        if (canHit)
                        {
                            if (attributes.ProcessProjectileHit(attacker, attack, col.ClosestPointOnBounds(inside[particleIndex].position), transform.position))
                            {
                                if (!hitCounter.ContainsKey(attributes))
                                {
                                    hitCounter.Add(attributes, new(1, Time.time));
                                }
                                else
                                {
                                    hitCounter[attributes] = new(hitCounter[attributes].hitNumber+1, Time.time);
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