using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Melee
{
    public class ParticleSystemProjectile : Projectile
    {
        [Header("Particle System Projectile Settings")]
        public Vector3 colliderLookBoxExtents = Vector3.one;
        public float particleRadius = 1;
        public int maxHits = 100;

        public void Initialize(CharacterMelee attacker, MeleeClip attack)
        {
            if (this.attacker) { Debug.LogError("Initialize() already called, why are you calling it again idiot?"); return; }
            this.attacker = attacker;
            this.attack = attack;
        }

        private ParticleSystem ps;
        private ApplyStatusOnProjectileCollision applyStatusOnProjectileCollision;

        private new void Start()
        {
            base.Start();
            ps = GetComponent<ParticleSystem>();
            applyStatusOnProjectileCollision = GetComponent<ApplyStatusOnProjectileCollision>();
        }

        private Dictionary<CharacterMelee, int> hitCounter = new Dictionary<CharacterMelee, int>();

        void OnParticleTrigger()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (!attacker) { Debug.LogError("Attacker has not been initialized yet! Call the Initialize() method"); return; }

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

            // particles
            List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();

            // get
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, enter, out var enterData);

            // iterate
            for (int i = 0; i < numEnter; i++)
            {
                Collider[] potentialHits = Physics.OverlapSphere(transform.TransformPoint(enter[i].position), particleRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                Debug.DrawRay(transform.TransformPoint(enter[i].position), Vector3.up * particleRadius, Color.green, 1);
                for (int j = 0; j < potentialHits.Length; j++)
                {
                    CharacterMelee targetMelee = potentialHits[j].GetComponentInParent<CharacterMelee>();
                    if (targetMelee == attacker) { continue; }
                    if (targetMelee)
                    {
                        bool hitCounterContainsMelee = hitCounter.ContainsKey(targetMelee);
                        if (hitCounterContainsMelee)
                        {
                            if (hitCounter[targetMelee] >= maxHits) { continue; }
                        }
                        
                        CharacterMelee.HitResult hitResult = attacker.ProcessProjectileHit(attacker, targetMelee, transform.TransformPoint(enter[i].position), attack);
                        if (hitResult != CharacterMelee.HitResult.Ignore)
                        {
                            if (hitCounterContainsMelee)
                            {
                                hitCounter[targetMelee] += 1;
                            }
                            else
                            {
                                hitCounter.Add(targetMelee, 1);
                            }
                        }

                        if (targetMelee.TryGetComponent(out CharacterStatusManager characterStatusManager))
                        {
                            applyStatusOnProjectileCollision.ApplyStatus(characterStatusManager);
                        }
                    }
                }
            }

            // set
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, enter);
        }

        public struct ProjectileHit
        {
            public CharacterMelee attacker;
            public CharacterMelee targetMelee;
            public Vector3 impactPosition;
            public MeleeClip attack;

            public ProjectileHit(CharacterMelee attacker, CharacterMelee targetMelee, Vector3 impactPosition, MeleeClip attack)
            {
                this.attacker = attacker;
                this.targetMelee = targetMelee;
                this.impactPosition = impactPosition;
                this.attack = attack;
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, colliderLookBoxExtents);
            }
        }
    }
}

