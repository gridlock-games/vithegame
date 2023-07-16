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
        public Vector3 particleSize = new Vector3(0.1f, 0.1f, 0.1f);

        private CharacterMelee attacker;

        public void Initialize(CharacterMelee attacker)
        {
            if (this.attacker) { Debug.LogError("Initialize() already called, why are you calling it again idiot?"); return; }
            this.attacker = attacker;
        }

        ParticleSystem ps;
        Vector3 startPosition;


        private void Start()
        {
            ps = GetComponent<ParticleSystem>();
            startPosition = transform.position;
        }

        void OnParticleTrigger()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (!attacker) { Debug.LogError("Attacker has not been initialized yet! Call the Initialize() method"); return; }

            Collider[] collidersInRange = Physics.OverlapBox(startPosition + transform.rotation * positionOffset, colliderLookBoxExtents / 2);
            
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
                Collider[] potentialHits = Physics.OverlapBox(transform.TransformPoint(enter[i].position), particleSize);
                for (int j = 0; j < potentialHits.Length; j++)
                {
                    CharacterMelee targetMelee = potentialHits[j].GetComponentInParent<CharacterMelee>();
                    if (targetMelee)
                    {
                        attacker.AddHitsToQueue(transform.TransformPoint(enter[i].position), new GameObject[] { targetMelee.gameObject });
                    }
                }
            }

            // set
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, enter);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(startPosition + transform.rotation * positionOffset, colliderLookBoxExtents);
        }
    }
}

