using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    public class FootstepAudioHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footStepSounds;
        private const float volume = 0.05f;

        Attributes attributes;
        private void Awake()
        {
            attributes = GetComponentInParent<Attributes>();
        }

        private bool footRaised;
        private const float footRaisedDistanceThreshold = 0.2f;
        private void LateUpdate()
        {
            if (!attributes.IsSpawned) { return; }
            if (attributes.GetAilment() == ActionClip.Ailment.Death) { return; }

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (footRaised)
                {
                    if (hit.distance <= footRaisedDistanceThreshold)
                    {
                        AudioManager.Singleton.PlayClipAtPoint(gameObject, footStepSounds[Random.Range(0, footStepSounds.Length)], transform.position, volume);
                        footRaised = false;
                    }
                }
                else
                {
                    footRaised = hit.distance > footRaisedDistanceThreshold;
                }
            }
            else
            {
                footRaised = true;
            }
        }
    }
}