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

        private void Start()
        {
            returnToCharSelectButton.onClick.AddListener(delegate { PersistentLocalObjects.Singleton.StartCoroutine(ReturnToCharacterSelect()); });
            returnToCharSelectButton.GetComponentInChildren<Text>().text = "CHARACTER SELECT";
            returnToCharSelectButton.gameObject.SetActive(!NetSceneManager.Singleton.IsSceneGroupLoaded("Main Menu"));

            applicationVersionText.text = "Version: " + Application.version;
        }

        private IEnumerator ReturnToCharacterSelect()
        {
            returnToCharSelectButton.interactable = false;
            if (NetworkManager.Singleton.IsListening)
            {
                PlayerDataManager.Singleton.WasDisconnectedByClient = true;
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            NetSceneManager.Singleton.LoadScene("Character Select");
            // The button gets destroyed when the networkmanager shuts down due to the player object despawning
            if (returnToCharSelectButton) { returnToCharSelectButton.interactable = true; }
        }
    }
}