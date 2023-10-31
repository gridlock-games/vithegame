using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ActionVFXParticleSystem : ActionVFX
    {
        private ParticleSystem ps;
        private void Start()
        {
            ps = GetComponent<ParticleSystem>();
        }

        public Vector3 colliderLookBoxExtents = Vector3.one;
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
                    Attributes targetAttributes = enterColliderData.GetCollider(particleIndex, colliderIndex).GetComponentInParent<Attributes>();
                    if (targetAttributes)
                    {
                        Debug.Log(targetAttributes);
                    }
                }
            }

            List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside, out ParticleSystem.ColliderData insideColliderData);

            for (int particleIndex = 0; particleIndex < numInside; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < insideColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Attributes targetAttributes = insideColliderData.GetCollider(particleIndex, colliderIndex).GetComponentInParent<Attributes>();
                    if (targetAttributes)
                    {
                        Debug.Log(targetAttributes);
                    }
                }
            }

            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
        }
    }
}