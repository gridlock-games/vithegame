using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using UnityEngine.UI;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;

        public void StartHubServer()
        {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Port = 7777;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub");
            NetSceneManager.Singleton.LoadScene("Player Hub Environment");
        }

        public void StartLobbyServer()
        {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Port = 7776;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        public void GoToCharacterSelect()
        {
            NetSceneManager.Singleton.LoadScene("Character Select");
        }

        public void GoToTrainingRoom()
        {
            NetworkManager.Singleton.StartHost();
            NetSceneManager.Singleton.LoadScene("Training Room");
        }

        public void OpenSettingsMenu()
        {
            Instantiate(pauseMenu.gameObject);
        }
    }
}

