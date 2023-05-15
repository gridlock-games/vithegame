using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class PauseMenu : Menu
    {
        public GameObject displaySettingsMenu;
        public GameObject controlSettingsMenu;
        public Slider volumeSlider;

        public void OpenDisplayMenu()
        {
            GameObject _settings = Instantiate(displaySettingsMenu);
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

        public void RestartLevel()
        {
            Application.LoadLevel(Application.loadedLevel);
        }

        private void Start()
        {
            volumeSlider.value = AudioListener.volume;
        }
    }
}