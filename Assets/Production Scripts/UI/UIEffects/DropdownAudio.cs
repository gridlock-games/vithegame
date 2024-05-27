using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vi.Utility;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private const float volume = 2;

        private void Start()
        {
            GetComponent<TMP_Dropdown>().onValueChanged.AddListener(Play2DAudio);
        }

        public void Play2DAudio(int value)
        {
            AudioManager.Singleton.Play2DClip(audioClip, volume);
        }
    }
}