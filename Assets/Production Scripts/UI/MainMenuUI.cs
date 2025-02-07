using Firebase.Auth;
// using jomarcentermjm.PlatformAPI;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.UI.SimpleGoogleSignIn;
using Vi.UI.FBAuthentication;
using Vi.Utility;
using Proyecto26;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using jomarcentermjm.steamauthentication;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private ContentManager contentManager;

        [SerializeField] private NewsManager newsManager;

        [SerializeField] private GameObject devSettingsParent;
        [SerializeField] private InputField APIURLInputField;

        [Header("Initial Group")]
        [SerializeField] private GameObject initialParent;

        [SerializeField] private Text loginMethodText;
        [SerializeField] private Text initialErrorText;
        [SerializeField] private Button[] authenticationButtons;

        [Header("Authentication")]
        [SerializeField] private Image viLogo;

        [SerializeField] private GameObject authenticationParent;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField emailInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button openLoginFormButton;
        [SerializeField] private Button openRegisterAccountButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Text loginErrorText;

        [Header("Individual Login Buttons")]
        [SerializeField] private GameObject steamLoginButton;

        [Header("OAuth")]
        [SerializeField] private GameObject oAuthParent;

        [SerializeField] private Text oAuthMessageText;

        [Header("Play Menu")]
        [SerializeField] private GameObject playParent;
        [SerializeField] private Button playButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private GameObject loadingProgressParent;
        [SerializeField] private Text loadingProgressText;
        [SerializeField] private Image loadingProgresssBar;

        [SerializeField] private Text welcomeUserText;
        [SerializeField] private Image welcomeUserImage;

        [SerializeField] private Sprite facebookImageSprite;
        [SerializeField] private Sprite googleImageSprite;
        [SerializeField] private Sprite steamImageSprite;
        [SerializeField] private Sprite baseImageSprite;

        [Header("Editor Only")]
        [SerializeField] private Button startHubServerButton;

        [SerializeField] private Button startLobbyServerButton;
        [SerializeField] private Button startAutoClientButton;

        private bool startServerCalled;
        private const int hubPort = 7777;

        public void StartHubServerButton()
        { StartCoroutine(StartHubServer()); }

        private string GetLocalIP()
        {
            string output = "127.0.0.1";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        // IPv4
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }

        private IEnumerator StartHubServer()
        {
            if (startServerCalled) { yield break; }

            var serverConfig = Path.Join(Application.dataPath, "ServerConfig.txt");

#if UNITY_STANDALONE_OSX
                Debug.Log("Will Use OSX Option");
                serverConfig =  @"/Users/odaleroxas/Documents/Builds/mac/headless/ServerConfig.txt";
#endif

            startServerCalled = true;
            AudioListener.volume = 0;

            yield return new WaitUntil(() => !ObjectPoolingManager.Singleton.IsLoadingOrPooling);

            string serverIP = null;
            if (File.Exists(serverConfig))
            {
                string[] lines = File.ReadAllLines(serverConfig);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i == 0)
                    {
                        serverIP = lines[i];
                    }
                    else if (i == 1)
                    {
                        WebRequestManager.Singleton.SetAPIURL(lines[i]);
                    }
                }
            }
            else
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
                {
                    serverIP = GetLocalIP();
                }
                else
                {
                    yield return WebRequestManager.Singleton.ServerManager.GetPublicIP();
                    serverIP = WebRequestManager.Singleton.ServerManager.PublicIP;
                }
            }

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.ServerManager.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.ServerManager.IsRefreshingServers);

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.SetConnectionData(serverIP, hubPort, FasterPlayerPrefs.serverListenAddress);

            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub", "Player Hub Environment");
        }

        public void StartLobbyServerButton()
        { StartCoroutine(StartLobbyServer()); }

        private IEnumerator StartLobbyServer()
        {
            if (startServerCalled) { yield break; }
            startServerCalled = true;
            AudioListener.volume = 0;

            yield return new WaitUntil(() => !ObjectPoolingManager.Singleton.IsLoadingOrPooling);

            var serverConfig = Path.Join(Application.dataPath, "ServerConfig.txt");

#if UNITY_STANDALONE_OSX
                Debug.Log("MACOSX");
                serverConfig = @"/Users/odaleroxas/Documents/Builds/mac/headless/ServerConfig.txt";
#endif

            string serverIP = null;
            if (File.Exists(serverConfig))
            {
                string[] lines = File.ReadAllLines(serverConfig);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i == 0)
                    {
                        serverIP = lines[i];
                    }
                    else if (i == 1)
                    {
                        WebRequestManager.Singleton.SetAPIURL(lines[i]);
                    }
                }
            }
            else
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
                {
                    serverIP = GetLocalIP();
                }
                else
                {
                    yield return WebRequestManager.Singleton.ServerManager.GetPublicIP();
                    serverIP = WebRequestManager.Singleton.ServerManager.PublicIP;
                }
            }

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.ServerManager.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.ServerManager.IsRefreshingServers);

            List<int> portList = new List<int>();
            foreach (ServerManager.Server server in System.Array.FindAll(WebRequestManager.Singleton.ServerManager.LobbyServers, item => item.ip == serverIP))
            {
                portList.Add(int.Parse(server.port));
            }

            int lobbyPort = hubPort - 1;
            while (lobbyPort > 0)
            {
                if (!portList.Contains(lobbyPort)) { break; }
                lobbyPort--;
            }

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.SetConnectionData(serverIP, (ushort)lobbyPort, FasterPlayerPrefs.serverListenAddress);

            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        private const string automatedClientUsername = "LightPat";
        private const string automatedClientPassword = "patrick11";

        private bool startAutomatedClientCalled;

        public void StartAutomatedClient()
        {
            if (startAutomatedClientCalled) { return; }
            startAutomatedClientCalled = true;
            AudioListener.volume = 0;

            FasterPlayerPrefs.IsAutomatedClient = true;
            StartCoroutine(LaunchAutoClient());
        }

        private IEnumerator LaunchAutoClient()
        {
            yield return new WaitUntil(() => !ObjectPoolingManager.Singleton.IsLoadingOrPooling);

            LoginWithVi();

            usernameInput.text = automatedClientUsername;
            passwordInput.text = automatedClientPassword;

            yield return Login();

            if (!WebRequestManager.Singleton.IsLoggedIn) { Debug.LogError("Automated client failed to login"); startAutomatedClientCalled = false; yield break; }

            WebRequestManager.Singleton.RefreshCharacters();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);

            if (WebRequestManager.Singleton.Characters.Count == 0) { Debug.LogError("Automated client has no character options"); startAutomatedClientCalled = false; yield break; }

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(WebRequestManager.Singleton.Characters[0]._id.ToString());

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.ServerManager.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.ServerManager.IsRefreshingServers);

            if (WebRequestManager.Singleton.ServerManager.HubServers.Length == 0) { Debug.LogError("Automated client has no hub server to connect to"); startAutomatedClientCalled = false; yield break; }

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.SetConnectionData(WebRequestManager.Singleton.ServerManager.HubServers[0].ip, ushort.Parse(WebRequestManager.Singleton.ServerManager.HubServers[0].port), FasterPlayerPrefs.serverListenAddress);
            
            NetworkManager.Singleton.StartClient();
        }

        public void LoginWithVi()
        {
            FasterPlayerPrefs.IsPlayingOffline = false;

            if (FasterPlayerPrefs.Singleton.HasString("username")) { usernameInput.text = FasterPlayerPrefs.Singleton.GetString("username"); } else { usernameInput.text = ""; }
            if (FasterPlayerPrefs.Singleton.HasString("password")) { passwordInput.text = FasterPlayerPrefs.Singleton.GetString("password"); } else { passwordInput.text = ""; }

            initialParent.SetActive(false);
            initialErrorText.text = "";

            emailInput.gameObject.SetActive(false);
            loginButton.GetComponentInChildren<Text>().text = "LOGIN";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(delegate { StartCoroutine(Login()); });

            forgotPasswordButton.gameObject.SetActive(true);
            openRegisterAccountButton.gameObject.SetActive(true);
            openLoginFormButton.gameObject.SetActive(false);
        }

        public FirebaseUser _firebasesteamuser;
        public SteamUserAccountData _SteamUserAccountData;
        private string _steamusername;
        private bool steamAuthExternalstepdone;

        public void LoginWithSteam()
        {
            Debug.Log("Logging in with Steam");
            dlpSetupAndLogin(DeepLinkProcessing.loginSiteSource.steam);
            openDialogue("Steam");
            SteamAuthentication.Auth((success, error, tokenData, steamuserAccountData, steamusername) =>
            {
                if (success)
                {
                    Debug.Log("Success");
                    updateSteamData(tokenData, steamuserAccountData, steamusername);

                    //StartCoroutine(WaitForSteamAuth(tokenData, steamuserAccountData, steamusername));
                }
                else
                {
                    Debug.LogError("Steam sign in error - " + error);
                    initialErrorText.text = error;
                    oAuthParent.SetActive(false);
                }
            });
        }

        private void updateSteamData(FirebaseUser user, SteamUserAccountData suad, string steamUsername)
        {
            _firebasesteamuser = user;
            _SteamUserAccountData = suad;
            _steamusername = steamUsername;
            steamAuthExternalstepdone = true;
        }

        private IEnumerator WaitForSteamAuth(FirebaseUser fireAuth, SteamUserAccountData suad, string steamUsername)
        {
            Debug.Log("Waiting on endpoint");
            yield return WebRequestManager.Singleton.LoginWithFirebaseUserId(suad.userEmail, fireAuth.UserId);

            if (WebRequestManager.Singleton.IsLoggedIn)
            {
                initialParent.SetActive(false);
                oAuthParent.SetActive(false);
                welcomeUserText.text = steamUsername;
                welcomeUserImage.sprite = steamImageSprite;
                FasterPlayerPrefs.Singleton.SetString("LastSignInType", "Steam");
                FasterPlayerPrefs.Singleton.SetString("AccountName", steamUsername);
            }
            else
            {
                oAuthParent.SetActive(false);
                initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
            }
        }

        public void OpenCreateAccount()
        {
            usernameInput.text = "";
            passwordInput.text = "";
            emailInput.text = "";

            initialParent.SetActive(false);

            emailInput.gameObject.SetActive(true);
            loginButton.GetComponentInChildren<Text>().text = "SUBMIT";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(delegate { StartCoroutine(CreateAccount()); });

            forgotPasswordButton.gameObject.SetActive(false);
            openRegisterAccountButton.gameObject.SetActive(false);
            openLoginFormButton.gameObject.SetActive(true);
        }

        public void OpenViLogin()
        {
            if (FasterPlayerPrefs.Singleton.HasString("username")) { usernameInput.text = FasterPlayerPrefs.Singleton.GetString("username"); } else { usernameInput.text = ""; }
            if (FasterPlayerPrefs.Singleton.HasString("password")) { passwordInput.text = FasterPlayerPrefs.Singleton.GetString("password"); } else { passwordInput.text = ""; }

            viLogo.enabled = false;
            initialParent.SetActive(false);
            authenticationParent.SetActive(true);

            emailInput.gameObject.SetActive(false);
            loginButton.GetComponentInChildren<Text>().text = "LOGIN";

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(delegate { StartCoroutine(Login()); });

            forgotPasswordButton.gameObject.SetActive(true);
            openRegisterAccountButton.gameObject.SetActive(true);
            openLoginFormButton.gameObject.SetActive(false);
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
            WebRequestManager.Singleton.ServerManager.RefreshServers();
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

        public void OpenNewsScreen()
        {
            newsManager.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        public void OpenDevSettings()
        {
            devSettingsParent.SetActive(true);
        }

        public void CloseDevSettings()
        {
            devSettingsParent.SetActive(false);
        }

        public void SetAPIURL()
        {
            if (!APIURLInputField.gameObject.activeInHierarchy) { return; }
            WebRequestManager.Singleton.SetAPIURL(APIURLInputField.text);
            OpenViLogin();
        }

        public void SetAPIURLToProd()
        {
            APIURLInputField.text = WebRequestManager.ProdAPIURL[0..^1];
            SetAPIURL();
        }

        public void SetAPIURLToDev()
        {
            APIURLInputField.text = WebRequestManager.DevAPIURL[0..^1];
            SetAPIURL();
        }

        public void QuitGame()
        {
            FasterPlayerPrefs.QuitGame();
        }

        public IEnumerator CreateAccount()
        {
            FasterPlayerPrefs.Singleton.SetString("username", usernameInput.text);
            FasterPlayerPrefs.Singleton.SetString("password", passwordInput.text);

            emailInput.interactable = false;
            usernameInput.interactable = false;
            passwordInput.interactable = false;

            yield return WebRequestManager.Singleton.CreateAccount(usernameInput.text, emailInput.text, passwordInput.text);

            emailInput.interactable = true;
            usernameInput.interactable = true;
            passwordInput.interactable = true;

            if (string.IsNullOrEmpty(WebRequestManager.Singleton.LogInErrorText))
            {
                ReturnToInitialElements();
            }
        }

        public IEnumerator Login()
        {
            FasterPlayerPrefs.Singleton.SetString("LastSignInType", "Vi");
            FasterPlayerPrefs.Singleton.SetString("username", usernameInput.text);
            FasterPlayerPrefs.Singleton.SetString("password", passwordInput.text);
            FasterPlayerPrefs.Singleton.SetString("AccountName", usernameInput.text);
            usernameInput.interactable = false;
            passwordInput.interactable = false;

            yield return WebRequestManager.Singleton.Login(usernameInput.text, passwordInput.text);

            welcomeUserText.text = FasterPlayerPrefs.Singleton.GetString("username");
            welcomeUserImage.sprite = baseImageSprite;
            usernameInput.interactable = true;
            passwordInput.interactable = true;
        }

        //private const string googleSignInClientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
        //private const string googleSignInSecretId = "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";

        private const string googleSignInClientId = "583444002427-p8hrsdv9p38migp7db30mch3qeluodda.apps.googleusercontent.com";
        private const string googleSignInSecretId = "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";

        //private const string facebookSignInClientId = "582767463749884";
        //private const string facebookSignInSecretId = "d5bd937c38b1b7843431cbfacd0ceeef";

        private const string facebookSignInClientId = "461749126721789";
        private const string facebookSignInSecretId = "c59261b57e535726b45dad280071d34d";

        public void LoginAsOfflineGuest()
        {
            FasterPlayerPrefs.IsPlayingOffline = true;
            WebRequestManager.Singleton.CheckGameVersion(true);

            initialParent.SetActive(false);
            initialErrorText.text = "";
        }

        public void LoginWithGoogle()
        {
            Debug.Log("Logging in with Google");
            FasterPlayerPrefs.IsPlayingOffline = false;
            dlpSetupAndLogin(DeepLinkProcessing.loginSiteSource.google);
            openDialogue("Google");
            GoogleAuth.Auth(googleSignInClientId, googleSignInSecretId, (success, error, tokenData) =>
            {
                if (success)
                {
                    StartCoroutine(WaitForGoogleAuth(tokenData));
                }
                else
                {
                    Debug.LogError("Google sign in error - " + error);
                    oAuthParent.SetActive(false);
                }
            });
        }

        private IEnumerator WaitForGoogleAuth(GoogleAuth.GoogleIdTokenResponse tokenData, bool refresh = false)
        {
            GoogleAuth.GoogleIdTokenResponse refreshedTokenData;
            Task<GoogleAuth.GoogleIdTokenResponse> refreshedTokenDataTask;
            Debug.Log(tokenData.id_token);
            Debug.Log(tokenData.refresh_token);
            Credential credential = new Credential();
            if (refresh)
            {
                refreshedTokenDataTask = GenerateNewGoogleToken(tokenData);
                yield return new WaitUntil(() => refreshedTokenDataTask.IsCompleted);
                refreshedTokenData = refreshedTokenDataTask.Result;
                credential = GoogleAuthProvider.GetCredential(refreshedTokenData.id_token, refreshedTokenData.access_token);
            }
            Debug.Log(tokenData.id_token);

            if (!refresh)
            {
                credential = GoogleAuthProvider.GetCredential(tokenData.id_token, tokenData.access_token);
            }

            System.Threading.Tasks.Task<AuthResult> task = auth.SignInAndRetrieveDataWithCredentialAsync(credential);

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsCanceled)
            {
                initialErrorText.text = "Login with google was cancelled.";
                oAuthParent.SetActive(false);
                yield break;
            }

            if (task.IsFaulted)
            {
                initialErrorText.text = "Login with google encountered an error.";
                oAuthParent.SetActive(false);
                yield break;
            }

            AuthResult authResult = task.Result;
            Debug.Log(authResult.User.Email);
            Debug.Log(authResult.User.UserId);
            oAuthMessageText.text = $"Waiting for Firebase Authentication";
            yield return WebRequestManager.Singleton.LoginWithFirebaseUserId(authResult.User.Email, authResult.User.UserId);

            if (WebRequestManager.Singleton.IsLoggedIn)
            {
                initialParent.SetActive(false);
                oAuthParent.SetActive(false);
                welcomeUserText.text = authResult.User.DisplayName;
                welcomeUserImage.sprite = googleImageSprite;
                FasterPlayerPrefs.Singleton.SetString("LastSignInType", "Google");
                FasterPlayerPrefs.Singleton.SetString("GoogleIdTokenResponse", JsonUtility.ToJson(tokenData));
                FasterPlayerPrefs.Singleton.SetString("AccountName", authResult.User.DisplayName);
            }
            else
            {
                oAuthParent.SetActive(false);
                initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
            }
        }

        private async Task<GoogleAuth.GoogleIdTokenResponse> GenerateNewGoogleToken(GoogleAuth.GoogleIdTokenResponse tokenData)
        {
            var baseUri = "https://oauth2.googleapis.com/token";
            var httpClient = new HttpClient();
            var parameters = new Dictionary<string, string>();
            parameters["client_id"] = googleSignInClientId;
            parameters["client_secret"] = googleSignInSecretId;
            parameters["refresh_token"] = tokenData.refresh_token;
            parameters["grant_type"] = "refresh_token";

            var response = await httpClient.PostAsync(baseUri, new FormUrlEncodedContent(parameters));
            var contents = response.Content.ReadAsStringAsync();
            Debug.Log(contents);
            GoogleAuth.GoogleIdTokenResponse converting = JsonUtility.FromJson<GoogleAuth.GoogleIdTokenResponse>(contents.Result);
            Debug.Log(converting);
            return converting;
        }

        public void LoginWithFacebook()
        {
            Debug.Log("Logging in with Facebook");
            dlpSetupAndLogin(DeepLinkProcessing.loginSiteSource.facebook);
            openDialogue("Facebook");
            FacebookAuth.Auth(facebookSignInClientId, facebookSignInSecretId, (success, error, tokenData) =>
            {
                if (success)
                {
                    StartCoroutine(WaitForFacebookAuth(tokenData));
                }
                else
                {
                    Debug.LogError("Facebook sign in error - " + error);
                    oAuthParent.SetActive(false);
                }
            });
        }

        private IEnumerator WaitForFacebookAuth(FacebookAuth.FacebookIdTokenResponse tokenData)
        {
            Debug.Log(tokenData.access_token);
            Debug.Log(tokenData.id_token);
            Debug.Log(tokenData.token_type);
            Debug.Log(tokenData.scope);
            Credential credential = FacebookAuthProvider.GetCredential(tokenData.access_token);
            System.Threading.Tasks.Task<AuthResult> task = auth.SignInAndRetrieveDataWithCredentialAsync(credential);

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsCanceled)
            {
                initialErrorText.text = "Login with Facebook was cancelled.";
                oAuthParent.SetActive(false);
                yield break;
            }

            if (task.IsFaulted)
            {
                initialErrorText.text = "Login with Facebook encountered an error.";
                oAuthParent.SetActive(false);
                yield break;
            }

            AuthResult authResult = task.Result;
            oAuthMessageText.text = $"Waiting for Firebase Authentication";
            Debug.Log(authResult.User.Email);
            Debug.Log(authResult.User.UserId);
            yield return WebRequestManager.Singleton.LoginWithFirebaseUserId(authResult.User.Email, authResult.User.UserId);

            if (WebRequestManager.Singleton.IsLoggedIn)
            {
                initialParent.SetActive(false);
                oAuthParent.SetActive(false);
                welcomeUserText.text = authResult.User.DisplayName;
                welcomeUserImage.sprite = facebookImageSprite;
                FasterPlayerPrefs.Singleton.SetString("LastSignInType", "Facebook");
                FasterPlayerPrefs.Singleton.SetString("FacebookIdTokenResponse", JsonUtility.ToJson(tokenData));
                FasterPlayerPrefs.Singleton.SetString("AccountName", authResult.User.DisplayName);
            }
            else
            {
                oAuthParent.SetActive(false);
                initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
            }
        }

        public void LoginWithApple()
        {
            initialErrorText.text = "Apple sign in not implemented yet";
        }

        public void Logout()
        {
            FasterPlayerPrefs.IsPlayingOffline = false;
            WebRequestManager.Singleton.Logout();
            initialParent.SetActive(true);
            FasterPlayerPrefs.Singleton.DeleteKey("LastSignInType");
            FasterPlayerPrefs.Singleton.DeleteKey("username");
            FasterPlayerPrefs.Singleton.DeleteKey("password");
            FasterPlayerPrefs.Singleton.DeleteKey("GoogleIdTokenResponse");
            FasterPlayerPrefs.Singleton.DeleteKey("FacebookIdTokenResponse");
            FasterPlayerPrefs.Singleton.DeleteKey("AccountName");
        }

        private FirebaseAuth auth;

        private void Start()
        {
            initialParent.SetActive(true);
            startHubServerButton.gameObject.SetActive(Application.isEditor);
            startLobbyServerButton.gameObject.SetActive(Application.isEditor);
            startAutoClientButton.gameObject.SetActive(Application.isEditor);
            initialErrorText.text = "";

            APIURLInputField.text = WebRequestManager.Singleton.GetAPIURL(true);

            foreach (Button button in authenticationButtons)
            {
                button.interactable = false;
            }

            if (!FasterPlayerPrefs.IsServerPlatform)
            {
                auth = FirebaseAuth.DefaultInstance;
                if (WebRequestManager.Singleton.IsLoggedIn & FasterPlayerPrefs.Singleton.HasString("LastSignInType"))
                {
                    welcomeUserText.text = FasterPlayerPrefs.Singleton.GetString("AccountName");
                    switch (FasterPlayerPrefs.Singleton.GetString("LastSignInType"))
                    {
                        case "Vi":
                            welcomeUserImage.sprite = baseImageSprite;
                            break;

                        case "Google":
                            welcomeUserImage.sprite = googleImageSprite;
                            break;

                        case "Facebook":
                            welcomeUserImage.sprite = facebookImageSprite;
                            break;

                        case "Steam":
                            welcomeUserImage.sprite = steamImageSprite;
                            break;

                        default:
                            Debug.LogError("Not sure how to handle last sign in type " + FasterPlayerPrefs.Singleton.GetString("LastSignInType"));
                            break;
                    }
                }
                else
                {
                    StartCoroutine(AutomaticallyAttemptLogin());
                }
                //HandlePlatformAPI();
            }

            //Running on Steam Version
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                steamLoginButton.SetActive(true);
            }

            initialParent.SetActive(!WebRequestManager.Singleton.IsLoggedIn);
        }

        private bool isAutomaticallyLoggingIn;
        private IEnumerator AutomaticallyAttemptLogin()
        {
            isAutomaticallyLoggingIn = true;
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);

            if (FasterPlayerPrefs.Singleton.HasString("LastSignInType"))
            {
                switch (FasterPlayerPrefs.Singleton.GetString("LastSignInType"))
                {
                    case "Vi":
                        usernameInput.text = FasterPlayerPrefs.Singleton.GetString("username");
                        passwordInput.text = FasterPlayerPrefs.Singleton.GetString("password");
                        yield return Login();
                        if (WebRequestManager.Singleton.IsLoggedIn)
                        {
                            initialParent.SetActive(false);
                        }
                        else
                        {
                            initialErrorText.text = WebRequestManager.Singleton.LogInErrorText;
                        }
                        break;

                    case "Google":
                        yield return WaitForGoogleAuth(JsonUtility.FromJson<GoogleAuth.GoogleIdTokenResponse>(FasterPlayerPrefs.Singleton.GetString("GoogleIdTokenResponse")), true);
                        break;

                    case "Facebook":
                        yield return WaitForFacebookAuth(JsonUtility.FromJson<FacebookAuth.FacebookIdTokenResponse>(FasterPlayerPrefs.Singleton.GetString("FacebookIdTokenResponse")));
                        break;
                    case "Steam":
                        //added steam
                        LoginWithSteam();
                        break;

                    default:
                        Debug.LogError("Not sure how to handle last sign in type " + FasterPlayerPrefs.Singleton.GetString("LastSignInType"));
                        break;
                }
            }
            isAutomaticallyLoggingIn = false;
        }

        private void dlpSetupAndLogin(DeepLinkProcessing.loginSiteSource loginSource)
        {
            Debug.Log($"Prepare deeplink login to look for {loginSource} Oauth");
            DeepLinkProcessing dlp = FindFirstObjectByType<DeepLinkProcessing>();
            dlp.SetLoginSource(loginSource);
        }

        public void CloseOAuthDialogue()
        {
            oAuthParent.SetActive(false);

            //Shutdown any possible Listner - Google
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                GoogleAuth.ShutdownListner();
            }
        }

        private void openDialogue(string platformname)
        {
            oAuthParent.SetActive(true);
            oAuthMessageText.text = $"Waiting for {platformname} Login";
        }

        private void Update()
        {
            playButton.gameObject.SetActive(!ObjectPoolingManager.Singleton.IsLoadingOrPooling);
            logoutButton.gameObject.SetActive(!ObjectPoolingManager.Singleton.IsLoadingOrPooling);

            loadingProgressParent.SetActive(ObjectPoolingManager.Singleton.IsLoadingOrPooling);

            if (loadingProgressParent.activeSelf)
            {
                if (!PlayerDataManager.CharacterReferenceHandle.IsValid())
                {
                    loadingProgressText.text = "Loading Player Data Manager...";
                    loadingProgresssBar.fillAmount = 0;
                }
                else if (!PlayerDataManager.CharacterReferenceHandle.IsDone)
                {
                    loadingProgressText.text = "Loading Character Equipment " + (PlayerDataManager.CharacterReferenceHandle.PercentComplete * 100).ToString("F0") + "%";
                    loadingProgresssBar.fillAmount = PlayerDataManager.CharacterReferenceHandle.PercentComplete;
                }
                else if (!PlayerDataManager.ControlsImageMappingHandle.IsValid())
                {
                    loadingProgressText.text = "Loading Controls Image Mapping...";
                    loadingProgresssBar.fillAmount = 0;
                }
                else if (!PlayerDataManager.ControlsImageMappingHandle.IsDone)
                {
                    loadingProgressText.text = "Loading Controls Image Mapping " + (PlayerDataManager.ControlsImageMappingHandle.PercentComplete * 100).ToString("F0") + "%";
                    loadingProgresssBar.fillAmount = PlayerDataManager.ControlsImageMappingHandle.PercentComplete;
                }
                else
                {
                    float progress = ObjectPoolingManager.Singleton.GetPooledObjectList().LoadCompletedCount / (float)ObjectPoolingManager.Singleton.GetPooledObjectList().TotalReferenceCount;
                    loadingProgressText.text = "Loading Rest Of Assets " + (progress * 100).ToString("F0") + "%";
                    loadingProgresssBar.fillAmount = progress;
                }
            }

            if (WebRequestManager.Singleton.IsCheckingGameVersion)
            {
                loginMethodText.text = "Checking Game Version...";
            }
            else if (isAutomaticallyLoggingIn)
            {
                loginMethodText.text = "Logging In...";
            }
            else if (WebRequestManager.Singleton.IsLoggingIn)
            {
                loginMethodText.text = "Logging In...";
            }
            else
            {
                loginMethodText.text = "Please Select Login Method";
            }

            startHubServerButton.interactable = !WebRequestManager.Singleton.ServerManager.IsRefreshingServers & playButton.gameObject.activeInHierarchy & WebRequestManager.Singleton.GetAPIURL(true) != WebRequestManager.ProdAPIURL[0..^1];
            startLobbyServerButton.interactable = !WebRequestManager.Singleton.ServerManager.IsRefreshingServers & playButton.gameObject.activeInHierarchy & WebRequestManager.Singleton.GetAPIURL(true) != WebRequestManager.ProdAPIURL[0..^1];
            startAutoClientButton.interactable = !WebRequestManager.Singleton.ServerManager.IsRefreshingServers & playButton.gameObject.activeInHierarchy & WebRequestManager.Singleton.GetAPIURL(true) != WebRequestManager.ProdAPIURL[0..^1];

            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-launch-as-hub-server") != -1)
            {
                StartCoroutine(StartHubServer());
            }
            else if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-launch-as-lobby-server") != -1)
            {
                StartCoroutine(StartLobbyServer());
            }
            else if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-launch-as-automated-client") != -1)
            {
                StartAutomatedClient();
            }

            loginButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            returnButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            openLoginFormButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            openRegisterAccountButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            forgotPasswordButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
            foreach (Button button in authenticationButtons)
            {
                button.interactable = !WebRequestManager.Singleton.IsLoggingIn & !WebRequestManager.Singleton.IsCheckingGameVersion & !isAutomaticallyLoggingIn;
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
            loginErrorText.text = WebRequestManager.Singleton.LogInErrorText;

            if (steamAuthExternalstepdone)
            {
                Debug.Log(_SteamUserAccountData.steamID);
                StartCoroutine(WaitForSteamAuth(_firebasesteamuser, _SteamUserAccountData, _steamusername));
                steamAuthExternalstepdone = false;
            }
        }

        public void HandlePlatformAPI()
        {
            //Rich presence
            // if (PlatformRichPresence.instance != null)
            // {
            //     //Change logic here that would handle scenario where the player is host.
            //     PlatformRichPresence.instance.UpdatePlatformStatus("Logging to Vi", "Login Menu");
            // }
        }

        public void OpenViDiscord()
        {
            Application.OpenURL(FasterPlayerPrefs.persistentDiscordInviteLink);
        }

        // Used for forgot password button
        public void EnableGameObject(GameObject target)
        {
            target.SetActive(true);
        }

        public void DisableGameObject(GameObject target)
        {
            target.SetActive(false);
        }
    }
}