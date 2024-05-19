using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public class PlayAudioClipOnToggleChange : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 2;

        private void Awake()
        {
            GetComponent<Toggle>().onValueChanged.AddListener(Play2DAudio);
        }

        public void Play2DAudio(bool isOn)
        {
            AudioManager.Singleton.Play2DClip(audioClip, volume);
        }
    }
}