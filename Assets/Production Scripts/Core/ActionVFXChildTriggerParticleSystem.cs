using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ActionVFXChildTriggerParticleSystem : MonoBehaviour
    {
        private ActionVFXParticleSystem actionVFXParticleSystem;
        private ParticleSystem ps;
        private void Awake()
        {
            actionVFXParticleSystem = GetComponentInParent<ActionVFXParticleSystem>();
            ps = GetComponent<ParticleSystem>();
        }

        private void OnParticleTrigger()
        {
            actionVFXParticleSystem.ProcessOnParticleEnterMessage(ps);
        }
    }
}