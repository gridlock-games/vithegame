using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vi.Utility;
#if UNITY_ANDROID || UNITY_IOS
using CandyCoded.HapticFeedback;
#endif

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

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private void OnEnable()
        {
            RefreshStatus();
            dropdown.onValueChanged.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(Play2DAudio);
        }

        private bool UIVibrationsEnabled;
        private void RefreshStatus()
        {
            UIVibrationsEnabled = FasterPlayerPrefs.Singleton.GetBool("UIVibrationsEnabled");
        }

        public void Play2DAudio(int value)
        {
            AudioManager.Singleton.Play2DClip(null, audioClip, volume);
#if UNITY_ANDROID || UNITY_IOS
            HapticFeedback.LightFeedback();
#endif
        }
    }
}