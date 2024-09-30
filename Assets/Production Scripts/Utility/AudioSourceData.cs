using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Utility
{
    public class AudioSourceData : MonoBehaviour
    {
        public float OriginalVolume { get; private set; }

        public AudioSource AudioSource { get; private set; }

        private void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

        public void Initialize(AudioSource audioSource)
        {
            OriginalVolume = audioSource.volume;
        }

        private void OnDisable()
        {
            OriginalVolume = default;
        }
    }
}