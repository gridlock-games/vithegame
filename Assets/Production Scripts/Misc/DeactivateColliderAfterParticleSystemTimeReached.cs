using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Misc
{
    [RequireComponent(typeof(ParticleSystem))]
    public class DeactivateColliderAfterParticleSystemTimeReached : MonoBehaviour
    {
        [SerializeField] private float timeThreshold = 4.5f;

        private ParticleSystem ps;
        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private bool thresholdReached;
        private void Update()
        {
            if (thresholdReached) { return; }

            if (ps.time >= timeThreshold)
            {
                thresholdReached = true;

                foreach (Collider col in GetComponentsInChildren<Collider>())
                {
                    if (col.isTrigger) { continue; }

                    col.enabled = false;
                }
            }
        }
    }
}