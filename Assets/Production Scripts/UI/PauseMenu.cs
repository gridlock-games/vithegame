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
        [SerializeField] private CharacterSelectMenu characterSelectMenu;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button characterSelectButton;

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

        public void OpenCharacterSelectMenu()
        {
            GameObject _characterSelect = Instantiate(characterSelectMenu.gameObject);
            _characterSelect.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _characterSelect;
            gameObject.SetActive(false);
        }

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
            PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        }

        private void Start()
        {
            volumeSlider.value = AudioListener.volume;
            characterSelectButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
        }
    }
}
