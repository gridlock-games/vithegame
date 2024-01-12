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
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text loginErrorText;
        [SerializeField] private GameObject authenticationParent;
        [SerializeField] private GameObject playParent;
        [SerializeField] private Text welcomeUserText;

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

        public void OpenSettingsMenu()
        {
            Instantiate(pauseMenu.gameObject);
        }

        public void Login()
        {
            StartCoroutine(WebRequestManager.Singleton.Login(usernameInput.text, passwordInput.text));
        }

        public void Logout()
        {
            WebRequestManager.Singleton.Logout();
        }

        private void Start()
        {
            if (!Application.isEditor)
            {
                usernameInput.text = "";
                passwordInput.text = "";
            }
        }

        private void Update()
        {
            loginButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;

            authenticationParent.SetActive(!WebRequestManager.Singleton.IsLoggedIn);
            playParent.SetActive(WebRequestManager.Singleton.IsLoggedIn);

            welcomeUserText.text = "Welcome " + usernameInput.text;
            loginErrorText.text = WebRequestManager.Singleton.LogInErrorText;
        }
    }
}

