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
            GameObject.Find("DebugOverlay").SendMessage("ToggleDebugOverlay", debugOverlayToggle.isOn);
        }

        private void Start()
        {
            GameObject.Find("DebugOverlay").SendMessage("ToggleDebugOverlay", debugOverlayToggle.isOn);
            volumeSlider.value = AudioListener.volume;

            goBackScenesButton.onClick.AddListener(delegate { StartCoroutine(ReturnToMainMenu()); });
        }

        public IEnumerator ReturnToMainMenu()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Network Manager shutdown");
                NetworkManager.Singleton.Shutdown();
                //yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening | !NetworkManager.Singleton.ShutdownInProgress);
                Debug.Log("shutdown complete");
            }

            Debug.Log("Loading main menu");
            NetSceneManager.Singleton.LoadScene("Main Menu");
            yield return null;
        }
    }
}
