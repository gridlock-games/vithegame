using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Utility;
using Vi.Player;

namespace Vi.UI
{
    public class PauseMenu : Menu
    {
        [SerializeField] private VideoSettingsMenu displaySettingsMenu; //Obselete
        [SerializeField] private AudioSettingsMenu audioSettingsMenu; //Obselete
        [SerializeField] private GameSettingsMenu gameSettingsMenu; //Obselete
        [SerializeField] private ControlsSettingsMenu controlSettingsMenu; //Obselete
        [SerializeField] private UIModificationMenu UIModificationMenu; //Obselete
        [SerializeField] private SettingMenuController settingMenu;
        [SerializeField] private Button returnToCharSelectButton;
        [SerializeField] private Text applicationVersionText;

        public void OpenSettingsMenu()
        {
            GameObject _settings = Instantiate(settingMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenVideoMenu() //Obselete
        {
            GameObject _settings = Instantiate(displaySettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenAudioMenu() //Obselete
        {
            GameObject _settings = Instantiate(audioSettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenGameMenu() //Obselete
        {
            GameObject _settings = Instantiate(gameSettingsMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenControlMenu() //Obselete
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

            applicationVersionText.text = "Version: " + Application.version;
        }

        public IEnumerator ReturnToCharacterSelect()
        {
            returnToCharSelectButton.interactable = false;
            if (NetworkManager.Singleton.IsListening)
            {
                PlayerDataManager.Singleton.wasDisconnectedByClient = true;
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            NetSceneManager.Singleton.LoadScene("Character Select");
            // The button gets destroyed when the networkmanager shuts down due to the player object despawning
            if (returnToCharSelectButton) { returnToCharSelectButton.interactable = true; }
        }
    }
}