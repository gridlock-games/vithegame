using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class FootstepAudioHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footStepSounds;
        [SerializeField] private float volume = 1;

        NetworkObject networkObject;
        private void Awake()
        {
            networkObject = GetComponentInParent<NetworkObject>();
        }

        private bool footRaised;
        private void LateUpdate()
        {
            if (!networkObject.IsSpawned) { return; }

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                if (footRaised)
                {
                    if (hit.distance <= 0.15f)
                    {
                        AudioManager.Singleton.PlayClipAtPoint(footStepSounds[Random.Range(0, footStepSounds.Length)], transform.position, volume);
                        footRaised = false;
                    }
                }
                else
                {
                    footRaised = hit.distance > 0.15f;
                }
            }
            else
            {
                footRaised = true;
            }
        }
    }
}