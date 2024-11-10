using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;
using CandyCoded.HapticFeedback;

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

        private void OnEnable()
        {
            button.onClick.AddListener(Play2DAudio);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Play2DAudio);
        }

        public void Play2DAudio()
        {
            AudioManager.Singleton.Play2DClip(null, audioClip, volume);
            HapticFeedback.LightFeedback();
        }
    }
}