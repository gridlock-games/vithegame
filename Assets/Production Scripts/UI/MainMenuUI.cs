using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using UnityEngine.UI;
using System.Net;
using Firebase.Auth;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private ContentManager contentManager;
        [Header("Initial Group")]
        [SerializeField] private GameObject initialParent;
        [SerializeField] private Button googleSignInButton;
        [Header("Authentication")]
        [SerializeField] private Image viLogo;
        [SerializeField] private GameObject authenticationParent;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField emailInput;
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

            if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
            {
                networkTransport.ConnectionData.Address = "127.0.0.1";
            }

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

            if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
            {
                networkTransport.ConnectionData.Address = "127.0.0.1";
            }

            List<int> portList = new List<int>();
            foreach (WebRequestManager.Server server in WebRequestManager.Singleton.LobbyServers)
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
            if (PlayerPrefs.HasKey("username")) { usernameInput.text = PlayerPrefs.GetString("username"); } else { usernameInput.text = ""; }
            if (PlayerPrefs.HasKey("password")) { passwordInput.text = PlayerPrefs.GetString("password"); } else { passwordInput.text = ""; }

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);
            WebRequestManager.Singleton.SetPlayingOffline(false);

            emailInput.gameObject.SetActive(false);
            loginButton.GetComponentInChildren<Text>().text = "LOGIN";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(Login);
        }

        public void OpenCreateAccount()
        {
            usernameInput.text = "";
            passwordInput.text = "";
            emailInput.text = "";

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);
            WebRequestManager.Singleton.SetPlayingOffline(false);

            emailInput.gameObject.SetActive(true);
            loginButton.GetComponentInChildren<Text>().text = "SUBMIT";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(delegate { StartCoroutine(CreateAccount()); });
        }

        public void PlayOffline()
        {
            WebRequestManager.Singleton.SetPlayingOffline(true);
            GoToCharacterSelect();
        }

        public void ReturnToInitialElements()
        {
            WebRequestManager.Singleton.ResetLogInErrorText();
            initialParent.SetActive(true);
            viLogo.enabled = true;
        }

        public void GoToCharacterSelect()
        {
            NetSceneManager.Singleton.LoadScene("Character Select");
        }

        public void OpenSettingsMenu()
        {
            Instantiate(pauseMenu.gameObject);
        }

        public void OpenContentManager()
        {
            Instantiate(contentManager.gameObject);
        }

        public IEnumerator CreateAccount()
        {
            PlayerPrefs.SetString("username", usernameInput.text);
            PlayerPrefs.SetString("password", passwordInput.text);
            yield return WebRequestManager.Singleton.CreateAccount(usernameInput.text, emailInput.text, passwordInput.text);

            if (string.IsNullOrEmpty(WebRequestManager.Singleton.LogInErrorText))
            {
                ReturnToInitialElements();
            }
        }

        public void Login()
        {
            PlayerPrefs.SetString("username", usernameInput.text);
            PlayerPrefs.SetString("password", passwordInput.text);
            StartCoroutine(WebRequestManager.Singleton.Login(usernameInput.text, passwordInput.text));
        }

        private string googleWebClientId = "583444002427-2496ljq7in3noe48o0nrllktt9e5r2ti.apps.googleusercontent.com";

        public IEnumerator LoginWithGoogle()
        {
            Debug.Log("Attempting login with google");

            //Credential credential = GoogleAuthProvider.GetCredential(googleIdToken, googleAccessToken);
            Credential credential = GoogleAuthProvider.GetCredential(null, googleWebClientId);
            auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInAndRetrieveDataWithCredentialAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInAndRetrieveDataWithCredentialAsync encountered an error: " + task.Exception);
                    return;
                }

                Firebase.Auth.AuthResult result = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    result.User.DisplayName, result.User.UserId);
            });


            yield return null;
        }

        public void Logout()
        {
            WebRequestManager.Singleton.Logout();
        }

        private FirebaseAuth auth;

        private void Start()
        {
            initialParent.SetActive(true);
            WebRequestManager.Singleton.RefreshServers();
            startHubServerButton.gameObject.SetActive(Application.isEditor);
            startLobbyServerButton.gameObject.SetActive(Application.isEditor);

            googleSignInButton.onClick.AddListener(() => StartCoroutine(LoginWithGoogle()));
            auth = FirebaseAuth.DefaultInstance;
        }

        private void Update()
        {
            startHubServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;
            startLobbyServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;

            if (!WebRequestManager.Singleton.IsRefreshingServers)
            {
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-launch-as-hub-server") != -1)
                {
                    StartHubServer();
                }
                else if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-launch-as-lobby-server") != -1)
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

