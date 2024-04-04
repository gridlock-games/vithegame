using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class AudioSettingsMenu : Menu
    {
        [SerializeField] private Slider volumeSlider;

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
            PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        }

        private void Start()
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume");
        }
    }
}
