using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.UI
{
    public class PauseMenu : Menu
    {
        [SerializeField] private VideoSettingsMenu displaySettingsMenu;
        [SerializeField] private AudioSettingsMenu audioSettingsMenu;
        [SerializeField] private GameSettingsMenu gameSettingsMenu;
        [SerializeField] private ControlsSettingsMenu controlSettingsMenu;
        [SerializeField] private UIModificationMenu UIModificationMenu;
        [SerializeField] private Button returnToCharSelectButton;

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

        public void OpenUIModificiationMenu()
        {
            GameObject _settings = Instantiate(UIModificationMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            returnToCharSelectButton.onClick.AddListener(delegate { PersistentLocalObjects.Singleton.StartCoroutine(ReturnToCharacterSelect()); });
            returnToCharSelectButton.GetComponentInChildren<Text>().text = "RETURN TO CHARACTER SELECT";
            returnToCharSelectButton.gameObject.SetActive(!NetSceneManager.Singleton.IsSceneGroupLoaded("Main Menu"));
        }

        public IEnumerator ReturnToCharacterSelect()
        {
            returnToCharSelectButton.interactable = false;
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            NetSceneManager.Singleton.LoadScene("Character Select");
            // The button gets destroyed when the networkmanager shuts down due to the player object despawning
            if (returnToCharSelectButton) { returnToCharSelectButton.interactable = true; }
        }
    }
}
