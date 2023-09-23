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

            //Collider[] collidersInRange = Physics.OverlapBox(startPosition + transform.rotation * positionOffset, colliderLookBoxExtents / 2);
            Collider[] collidersInRange = Physics.OverlapBox(transform.position, colliderLookBoxExtents, transform.rotation);

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
                    if (col.name == "Greatsword_Training_Dummy")
                    {
                        Debug.Log(Time.time + " " + col);
                    }

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
                        attacker.AddMeleeHitsToQueue(transform.TransformPoint(enter[i].position), new GameObject[] { targetMelee.gameObject }, attack);
                    }
                }
            }

            // set
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, enter);
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

