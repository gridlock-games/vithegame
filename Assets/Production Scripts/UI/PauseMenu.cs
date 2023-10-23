using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class PauseMenu : Menu
    {
        [SerializeField] private DisplaySettingsMenu displaySettingsMenu;
        [SerializeField] private GameObject controlSettingsMenu;
        [SerializeField] private Slider volumeSlider;

        public void OpenDisplayMenu()
        {
            GameObject _settings = Instantiate(displaySettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenControlMenu()
        {
            GameObject _settings = Instantiate(controlSettingsMenu);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
        }

        private void Start()
        {
            volumeSlider.value = AudioListener.volume;
        }
    }
}
