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
        [SerializeField] private VideoSettingsMenu displaySettingsMenu;
        [SerializeField] private AudioSettingsMenu audioSettingsMenu;
        [SerializeField] private GameSettingsMenu gameSettingsMenu;
        [SerializeField] private ControlsSettingsMenu controlSettingsMenu;
        [SerializeField] private Button goBackScenesButton;

        public void OpenVideoMenu()
        {
            GameObject _settings = Instantiate(displaySettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenAudioMenu()
        {
            GameObject _settings = Instantiate(audioSettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenGameMenu()
        {
            GameObject _settings = Instantiate(gameSettingsMenu.gameObject);
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

        private void Start()
        {
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
