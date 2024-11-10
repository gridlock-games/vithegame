using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;
using CandyCoded.HapticFeedback;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public class PlayAudioClipOnToggleChange : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 2;

        private Toggle toggle;
        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(Play2DAudio);
        }

        public void Play2DAudio(bool isOn)
        {
            AudioManager.Singleton.Play2DClip(null, audioClip, volume);
            HapticFeedback.MediumFeedback();
        }
    }
}