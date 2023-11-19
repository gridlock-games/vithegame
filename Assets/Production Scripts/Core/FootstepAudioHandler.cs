using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class FootstepAudioHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footStepSounds;
        [SerializeField] private float volume = 1;

        Vector3 lastFootstepPosition;
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            if (collision.relativeVelocity.magnitude < 1) { return; }
            if (Vector3.Distance(collision.GetContact(0).point, lastFootstepPosition) < 1) { return; }

            Debug.Log(Time.time + " " + collision.collider);
            AudioManager.Singleton.PlayClipAtPoint(footStepSounds[Random.Range(0, footStepSounds.Length)], collision.GetContact(0).point, volume);
            lastFootstepPosition = collision.GetContact(0).point;
        }
    }
}