using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vi.Utility;
using CandyCoded.HapticFeedback;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 2;

        private TMP_Dropdown dropdown;
        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(Play2DAudio);
        }

        public void Play2DAudio(int value)
        {
            AudioManager.Singleton.Play2DClip(null, audioClip, volume);
            HapticFeedback.HeavyFeedback();
        }
    }
}