using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Net;
using System.IO;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private GameObject initialParent;
        [Header("Authentication")]
        [SerializeField] private GameObject authenticationParent;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text loginErrorText;
        [Header("Online Play Menu")]
        [SerializeField] private GameObject playParent;
        [SerializeField] private Text welcomeUserText;
        [Header("Editor Only")]
        [SerializeField] private Button startHubServerButton;
        [SerializeField] private Button startLobbyServerButton;

        private bool startServerCalled;
        private const int hubPort = 7777;
        public void StartHubServer()
        {
            if (startServerCalled) { return; }
            startServerCalled = true;

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            networkTransport.ConnectionData.Port = hubPort;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub");
        }

        public void StartLobbyServer()
        {
            if (startServerCalled) { return; }
            startServerCalled = true;

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

            List<int> portList = new List<int>();
            foreach (WebRequestManager.Server server in WebRequestManager.Singleton.Servers)
            {
                portList.Add(int.Parse(server.port));
            }

            int lobbyPort = hubPort - 1;
            portList.Sort();
            portList.Reverse();
            foreach (int port in portList)
            {
                lobbyPort = port - 1;
                if (!portList.Contains(lobbyPort))
                    break;
            }

            networkTransport.ConnectionData.Port = (ushort)lobbyPort;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        public void PlayOnline()
        {
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);
            WebRequestManager.Singleton.SetPlayingOffline(false);
        }

        public void PlayOffline()
        {
            WebRequestManager.Singleton.SetPlayingOffline(true);
            GoToCharacterSelect();
        }

        public void ReturnToInitialElements()
        {
            initialParent.SetActive(true);
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
            initialParent.SetActive(true);
            WebRequestManager.Singleton.RefreshServers();
            startHubServerButton.gameObject.SetActive(Application.isEditor);
            startLobbyServerButton.gameObject.SetActive(Application.isEditor);
        }

        private void Update()
        {
            startHubServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;
            startLobbyServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;

            if (!WebRequestManager.Singleton.IsRefreshingServers)
            {
                bool isHubInBuild = SceneUtility.GetBuildIndexByScenePath("Player Hub") != -1;
                bool isLobbyInBuild = SceneUtility.GetBuildIndexByScenePath("Lobby") != -1;
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null | !(isHubInBuild & isLobbyInBuild))
                {
                    if (isHubInBuild & isLobbyInBuild)
                    {
                        //StoreClient("Headless Client");
                    }
                    else if (isHubInBuild)
                    {
                        StartHubServer();
                    }
                    else if (isLobbyInBuild)
                    {
                        StartLobbyServer();
                    }
                }
            }
            
            loginButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;

            if (!initialParent.activeSelf)
            {
                authenticationParent.SetActive(!WebRequestManager.Singleton.IsLoggedIn);
                playParent.SetActive(WebRequestManager.Singleton.IsLoggedIn);
            }
            else
            {
                authenticationParent.SetActive(false);
                playParent.SetActive(false);
            }
            
            welcomeUserText.text = "Welcome " + usernameInput.text;
            loginErrorText.text = WebRequestManager.Singleton.LogInErrorText;
        }
    }
}

