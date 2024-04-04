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
        [SerializeField] private Button goBackScenesButton;

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
            PlayerPrefs.SetString("DebugOverlayEnabled", (!bool.Parse(PlayerPrefs.GetString("DebugOverlayEnabled"))).ToString());
        }

        private void Start()
        {
            debugOverlayToggle.SetIsOnWithoutNotify(bool.Parse(PlayerPrefs.GetString("DebugOverlayEnabled")));
            volumeSlider.value = AudioListener.volume;

            goBackScenesButton.onClick.AddListener(delegate { ReturnToCharacterSelect(); });
            goBackScenesButton.GetComponentInChildren<Text>().text = "RETURN TO CHARACTER SELECT";

            goBackScenesButton.gameObject.SetActive(!NetSceneManager.Singleton.IsSceneGroupLoaded("Main Menu"));
        }

        public void ReturnToCharacterSelect()
        {
            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(); }

            NetSceneManager.Singleton.LoadScene("Character Select");
        }
    }
}
