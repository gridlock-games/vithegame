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
        [SerializeField] private InputField usernameInputField;
        [SerializeField] private Button playGameButton;

        private void Awake()
        {
            playGameButton.interactable = usernameInputField.text.Length > 0;
        }

        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub");
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

        public void OnUsernameChange()
        {
            playGameButton.interactable = usernameInputField.text.Length > 0;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(usernameInputField.text + "|0|0");
        }
    }
}

