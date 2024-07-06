using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.Utility
{
    public class KeepParticleSystemsPlaying : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] particleSystemsToExclude;

        List<ParticleSystem> particleSystemsToEvaluate = new List<ParticleSystem>();
        private void Start()
        {
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
            {
                if (particleSystemsToExclude.Contains(ps)) { continue; }
                particleSystemsToEvaluate.Add(ps);
            }
        }

        private void Update()
        {
            foreach (ParticleSystem particleSystem in particleSystemsToEvaluate)
            {
                if (!particleSystem.isPlaying) { particleSystem.Play(false); }
            }
        }
    }
}