using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Melee
{
    public class ApplyDamageOnParticleSystemCollision : MonoBehaviour
    {
        public Vector3 positionOffset;
        public Vector3 colliderLookBoxExtents = Vector3.one;
        public float particleRadius = 0.1f;
        public bool onTriggerEnter;

        private CharacterMelee attacker;
        private MeleeClip attack;

        public void Initialize(CharacterMelee attacker, MeleeClip attack)
        {
            if (this.attacker) { Debug.LogError("Initialize() already called, why are you calling it again idiot?"); return; }
            this.attacker = attacker;
            this.attack = attack;
        }

        private ParticleSystem ps;

        private void Start()
        {
            ps = GetComponent<ParticleSystem>();
        }

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
            int numEnter = ps.GetTriggerParticles(onTriggerEnter ? ParticleSystemTriggerEventType.Enter : ParticleSystemTriggerEventType.Inside, enter, out var enterData);

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
                        attacker.ProcessProjectileHit(attacker, targetMelee, transform.TransformPoint(enter[i].position), attack);
                    }
                }
            }

            // set
            ps.SetTriggerParticles(onTriggerEnter ? ParticleSystemTriggerEventType.Enter : ParticleSystemTriggerEventType.Inside, enter);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + transform.rotation * positionOffset, colliderLookBoxExtents);
            }
        }
    }
}

