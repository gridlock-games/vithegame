using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    [RequireComponent(typeof(ParticleSystem))]
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
        }

        [SerializeField] private Vector3 colliderLookBoxExtents = Vector3.one;

        private Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter = new Dictionary<Attributes, RuntimeWeapon.HitCounterData>();

        private void OnParticleTrigger()
        {
            Collider[] collidersInRange = Physics.OverlapBox(transform.position, colliderLookBoxExtents, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            foreach (Collider col in collidersInRange)
            {
                bool skip = false;
                for (int i = 0; i < ps.trigger.colliderCount; i++)
                {
                    if (ps.trigger.GetCollider(i) == col)
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                {
                    ps.trigger.AddCollider(col);
                }
            }

            List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter, out ParticleSystem.ColliderData enterColliderData);

            for (int particleIndex = 0; particleIndex < numEnter; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < enterColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)enterColliderData.GetCollider(particleIndex, colliderIndex);
                    Attributes attributes = col.GetComponentInParent<Attributes>();
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
                            if (attributes.ProcessProjectileHit(attacker, attack, col.ClosestPointOnBounds(enter[particleIndex].position), 0))
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
                            if (attributes.ProcessProjectileHit(attacker, attack, col.ClosestPointOnBounds(inside[particleIndex].position), 0))
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