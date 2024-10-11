using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core;
using Vi.Core.MovementHandlers;

namespace Vi.Isolated
{
    public class FootstepAudioHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footStepSounds;
        private const float volume = 0.05f;

        CombatAgent combatAgent;
        private void OnEnable()
        {
            combatAgent = GetComponentInParent<CombatAgent>();
        }

        private void OnDisable()
        {
            footRaised = false;
        }

        private bool footRaised;
        private const float footRaisedDistanceThreshold = 0.2f;
        private void LateUpdate()
        {
            if (!combatAgent) { return; }
            if (!combatAgent.IsSpawned) { return; }
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }

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