using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;

namespace Vi.UI
{
    public class PauseMenu : Menu
    {
        [SerializeField] private DisplaySettingsMenu displaySettingsMenu;
        [SerializeField] private ControlsSettingsMenu controlSettingsMenu;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle debugOverlayToggle;

        public void OpenDisplayMenu()
        {
            GameObject _settings = Instantiate(displaySettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenControlMenu()
        {
            GameObject _settings = Instantiate(controlSettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
            PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        }

        public void SetDebugOverlay()
        {
            GameObject.Find("DebugOverlay").SendMessage("ToggleDebugOverlay", debugOverlayToggle.isOn);
        }

        private void Start()
        {
            GameObject.Find("DebugOverlay").SendMessage("ToggleDebugOverlay", debugOverlayToggle.isOn);
            volumeSlider.value = AudioListener.volume;
        }
    }
}
