using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class PlayAudioClipOnButtonPress : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 1;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(Play2DAudio);
        }

        public void Play2DAudio()
        {
            AudioManager.Singleton.Play2DClip(audioClip, volume);
        }
    }
}