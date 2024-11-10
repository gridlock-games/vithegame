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
    [RequireComponent(typeof(Button))]
    public class PlayAudioClipOnButtonPress : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 2;

        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private void OnEnable()
        {
            RefreshStatus();
            button.onClick.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Play2DAudio);
        }
        
        private bool UIVibrationsEnabled;
        private void RefreshStatus()
        {
            UIVibrationsEnabled = FasterPlayerPrefs.Singleton.GetBool("UIVibrationsEnabled");
        }

        public void Play2DAudio()
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