using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ActionVFXChildTriggerParticleSystem : MonoBehaviour
    {
        private ActionVFXParticleSystem actionVFXParticleSystem;
        private void Awake()
        {
            actionVFXParticleSystem = GetComponentInParent<ActionVFXParticleSystem>();
        }

        private void OnParticleTrigger()
        {
            actionVFXParticleSystem.OnParticleTrigger();
        }
    }
}