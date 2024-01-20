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

        private bool startServerCalled;
        public void StartHubServer()
        {
            if (startServerCalled) { return; }
            startServerCalled = true;

            //string path = Application.dataPath;
            //path = path.Substring(0, path.LastIndexOf('/'));
            //path = path.Substring(0, path.LastIndexOf('/'));
            //path = Path.Join(path, new DirectoryInfo(System.Array.Find(Directory.GetDirectories(path), a => a.ToLower().Contains("lobby"))).Name);
            //path = Path.Join(path, Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer ? "VitheGame.exe" : "VitheGame.x86_64");

            //System.Diagnostics.Process.Start(path);

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Port = 7777;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub");
        }

        public void StartLobbyServer()
        {
            if (startServerCalled) { return; }
            startServerCalled = true;

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Port = 7776;
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
        }

        private void Update()
        {
            bool isHubInBuild = SceneUtility.GetBuildIndexByScenePath("Hub") != -1;
            bool isLobbyInBuild = SceneUtility.GetBuildIndexByScenePath("Lobby") != -1;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null | !(isHubInBuild & isLobbyInBuild))
            {
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
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

