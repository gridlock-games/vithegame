using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;
#if UNITY_ANDROID || UNITY_IOS
using CandyCoded.HapticFeedback;
#endif

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

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private void OnEnable()
        {
            RefreshStatus();
            toggle.onValueChanged.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(Play2DAudio);
        }
        
        private bool UIVibrationsEnabled;
        private void RefreshStatus()
        {
            UIVibrationsEnabled = FasterPlayerPrefs.Singleton.GetBool("UIVibrationsEnabled");
        }

        public void Play2DAudio(bool isOn)
        {
            AudioManager.Singleton.Play2DClip(null, audioClip, volume);
#if UNITY_ANDROID || UNITY_IOS
            if (UIVibrationsEnabled)
            {
                HapticFeedback.LightFeedback();
            }
#endif
        }
    }
}