using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using UnityEngine.UI;
using System.Net;
using Firebase.Auth;
using Vi.UI.SimpleGoogleSignIn;
using Proyecto26;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private ContentManager contentManager;
        [Header("Initial Group")]
        [SerializeField] private GameObject initialParent;
        [SerializeField] private Text initialErrorText;
        [SerializeField] private Button[] authenticationButtons;
        [Header("Authentication")]
        [SerializeField] private Image viLogo;
        [SerializeField] private GameObject authenticationParent;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField emailInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button switchLoginFormButton;
        [SerializeField] private Text loginErrorText;
        [Header("Play Menu")]
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
            while (lobbyPort > 0)
            {
                if (!portList.Contains(lobbyPort)) { break; }
                lobbyPort--;
            }

            networkTransport.ConnectionData.Port = (ushort)lobbyPort;
            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        public void LoginWithVi()
        {
            if (PlayerPrefs.HasKey("username")) { usernameInput.text = PlayerPrefs.GetString("username"); } else { usernameInput.text = ""; }
            if (PlayerPrefs.HasKey("password")) { passwordInput.text = PlayerPrefs.GetString("password"); } else { passwordInput.text = ""; }

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);

            emailInput.gameObject.SetActive(false);
            loginButton.GetComponentInChildren<Text>().text = "LOGIN";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(Login);

            switchLoginFormButton.GetComponentInChildren<Text>().text = "CREATE ACCOUNT";
            switchLoginFormButton.onClick.RemoveAllListeners();
            switchLoginFormButton.onClick.AddListener(OpenCreateAccount);
        }

        public void OpenCreateAccount()
        {
            usernameInput.text = "";
            passwordInput.text = "";
            emailInput.text = "";

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);

            emailInput.gameObject.SetActive(true);
            loginButton.GetComponentInChildren<Text>().text = "SUBMIT";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(delegate { StartCoroutine(CreateAccount()); });

            switchLoginFormButton.GetComponentInChildren<Text>().text = "GO TO LOGIN";
            switchLoginFormButton.onClick.RemoveAllListeners();
            switchLoginFormButton.onClick.AddListener(OpenViLogin);
        }

        public void OpenViLogin()
        {
            if (PlayerPrefs.HasKey("username")) { usernameInput.text = PlayerPrefs.GetString("username"); } else { usernameInput.text = ""; }
            if (PlayerPrefs.HasKey("password")) { passwordInput.text = PlayerPrefs.GetString("password"); } else { passwordInput.text = ""; }

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);

            emailInput.gameObject.SetActive(false);
            loginButton.GetComponentInChildren<Text>().text = "LOGIN";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(Login);

            switchLoginFormButton.GetComponentInChildren<Text>().text = "CREATE ACCOUNT";
            switchLoginFormButton.onClick.RemoveAllListeners();
            switchLoginFormButton.onClick.AddListener(OpenCreateAccount);
        }

        public void ReturnToInitialElements()
        {
            WebRequestManager.Singleton.ResetLogInErrorText();
            initialParent.SetActive(true);
            viLogo.enabled = true;
            initialErrorText.text = "";
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

        private const string googleSignInClientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
        private const string googleSignInSecretId = "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";

        public void LoginWithGoogle()
        {
            initialErrorText.text = "Google sign in not implemented yet";
            return;

            GoogleAuth.Auth(googleSignInClientId, googleSignInSecretId, (success, error, tokenData) =>
            {
                if (success)
                {
                    StartCoroutine(WaitForGoogleAuth(tokenData));
                }
                else
                {
                    Debug.LogError("Google sign in error - " + error);
                }
            });
        }

        private IEnumerator WaitForGoogleAuth(GoogleAuth.GoogleIdTokenResponse tokenData)
        {
            Credential credential = GoogleAuthProvider.GetCredential(tokenData.id_token, null);
            System.Threading.Tasks.Task<AuthResult> task = auth.SignInAndRetrieveDataWithCredentialAsync(credential);

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsCanceled)
            {
                loginErrorText.text = "Login with google was cancelled.";
                yield break;
            }

            if (task.IsFaulted)
            {
                loginErrorText.text = "Login with google encountered an error.";
                yield break;
            }

            AuthResult authResult = task.Result;
            Debug.Log("User signed in successfully: " + authResult.User.DisplayName + " (" + authResult.User.UserId + ")");

            yield return WebRequestManager.Singleton.LoginWithFirebaseUserId(authResult.User.Email, authResult.User.UserId);

            if (WebRequestManager.Singleton.IsLoggedIn)
            {
                initialParent.SetActive(false);
            }
            else
            {
                initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
            }
        }

        public void LoginWithFacebook()
        {
            initialErrorText.text = "Facebook sign in not implemented yet";
        }

        public void LoginWithApple()
        {
            initialErrorText.text = "Apple sign in not implemented yet";
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
            auth = FirebaseAuth.DefaultInstance;
            initialErrorText.text = "";

            StartCoroutine(AutomaticallyAttemptLogin());
        }

        private IEnumerator AutomaticallyAttemptLogin()
        {
            if (PlayerPrefs.HasKey("username") & PlayerPrefs.HasKey("password"))
            {
                usernameInput.text = PlayerPrefs.GetString("username");
                passwordInput.text = PlayerPrefs.GetString("password");
                Login();

                yield return new WaitUntil(() => !WebRequestManager.Singleton.IsLoggingIn);

                if (WebRequestManager.Singleton.IsLoggedIn)
                {
                    initialParent.SetActive(false);
                }
                else
                {
                    initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
                }
            }
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
            foreach (Button button in authenticationButtons)
            {
                button.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            }

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

            viLogo.enabled = playParent.activeSelf | initialParent.activeSelf;

            welcomeUserText.text = "Welcome " + usernameInput.text;
            loginErrorText.text = WebRequestManager.Singleton.LogInErrorText;
        }
    }
}

