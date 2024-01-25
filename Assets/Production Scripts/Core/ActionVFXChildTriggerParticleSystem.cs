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
            ParticleSystem.TriggerModule triggerModule = ps.trigger;
            triggerModule.colliderQueryMode = ParticleSystemColliderQueryMode.All;
            triggerModule.enter = ParticleSystemOverlapAction.Callback;
            triggerModule.exit = ParticleSystemOverlapAction.Ignore;
            triggerModule.inside = ParticleSystemOverlapAction.Callback;
            triggerModule.outside = ParticleSystemOverlapAction.Ignore;
            ParticleSystem.MainModule mainModule = ps.main;
            mainModule.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }

        private void OnParticleTrigger()
        {
            actionVFXParticleSystem.ProcessOnParticleEnterMessage(ps);
        }
    }
}