using Firebase.Auth;
using jomarcentermjm.PlatformAPI;
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
                    serverIP = "127.0.0.1";
                }
                else
                {
                    serverIP = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
                }
            }

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingServers);

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = serverIP;

            networkTransport.ConnectionData.Port = hubPort;

            networkTransport.MaxPacketQueueSize = 512;

            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Player Hub");
            NetSceneManager.Singleton.LoadScene("Player Hub Environment");
        }

        public void StartLobbyServerButton()
        { StartCoroutine(StartLobbyServer()); }

        private IEnumerator StartLobbyServer()
        {
            if (startServerCalled) { yield break; }
            startServerCalled = true;
            AudioListener.volume = 0;

            var serverConfig = Path.Join(Application.dataPath, "ServerConfig.txt");

#if UNITY_STANDALONE_OSX
                Debug.Log("MACOSX");
                serverConfig = @"/Users/odaleroxas/Documents/Builds/mac/headless/ServerConfig.txt";
#endif

            Debug.Log(serverConfig);

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
                    serverIP = "127.0.0.1";
                }
                else
                {
                    serverIP = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
                }
            }

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingServers);

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = serverIP;

            List<int> portList = new List<int>();
            foreach (WebRequestManager.Server server in System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address))
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

            networkTransport.MaxPacketQueueSize = 512;
            networkTransport.MaxSendQueueSize = 512;

            NetworkManager.Singleton.StartServer();
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        private const string automatedClientUsername = "roxasodale91";
        private const string automatedClientPassword = "123456";

        private bool startAutomatedClientCalled;

        public void StartAutomatedClient()
        {
            if (startAutomatedClientCalled) { return; }
            startAutomatedClientCalled = true;
            AudioListener.volume = 0;

            StartCoroutine(LaunchAutoClient());
        }

        private IEnumerator LaunchAutoClient()
        {
            LoginWithVi();

            usernameInput.text = automatedClientUsername;
            passwordInput.text = automatedClientPassword;

            yield return Login();

            if (!WebRequestManager.Singleton.IsLoggedIn) { Debug.LogError("Automated client failed to login"); yield break; }

            WebRequestManager.Singleton.RefreshCharacters();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);

            if (WebRequestManager.Singleton.Characters.Count == 0) { Debug.LogError("Automated client has no character options"); yield break; }

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(WebRequestManager.Singleton.Characters[0]._id.ToString());

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsCheckingGameVersion);
            WebRequestManager.Singleton.RefreshServers();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingServers);

            if (WebRequestManager.Singleton.HubServers.Length == 0) { Debug.LogError("Automated client has no hub server to connect to"); yield break; }

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = WebRequestManager.Singleton.HubServers[0].ip;
            networkTransport.ConnectionData.Port = ushort.Parse(WebRequestManager.Singleton.HubServers[0].port);

            NetworkManager.Singleton.StartClient();
        }

        public void LoginWithVi()
        {
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
            SteamUserAccountData suad;
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
            WebRequestManager.Singleton.RefreshServers();
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
        }

        public void QuitGame()
        {
            Application.Quit();
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

        public void LoginWithGoogle()
        {
            Debug.Log("Logging in with Google");
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
            WebRequestManager.Singleton.Logout();
            initialParent.SetActive(true);
            FasterPlayerPrefs.Singleton.DeleteKey("LastSignInType");
            FasterPlayerPrefs.Singleton.DeleteKey("username");
            FasterPlayerPrefs.Singleton.DeleteKey("password");
            FasterPlayerPrefs.Singleton.DeleteKey("GoogleIdTokenResponse");
            FasterPlayerPrefs.Singleton.DeleteKey("FacebookIdTokenResponse");
            FasterPlayerPrefs.Singleton.DeleteKey("AccountName");
        }

        public void ForgotPassword()
        {
            Debug.LogError("Not implemented yet!");
        }

        private FirebaseAuth auth;

        private void Start()
        {
            initialParent.SetActive(true);
            startHubServerButton.gameObject.SetActive(Application.isEditor);
            startLobbyServerButton.gameObject.SetActive(Application.isEditor);
            startAutoClientButton.gameObject.SetActive(Application.isEditor);
            initialErrorText.text = "";

            APIURLInputField.text = WebRequestManager.Singleton.GetAPIURL();

            foreach (Button button in authenticationButtons)
            {
                button.interactable = false;
            }

            if (!WebRequestManager.IsServerBuild())
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

        private IEnumerator AutomaticallyAttemptLogin()
        {
            yield return new WaitUntil(() => WebRequestManager.Singleton.GameIsUpToDate);

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
        }

        private void dlpSetupAndLogin(DeepLinkProcessing.loginSiteSource loginSource)
        {
            Debug.Log($"Prepare deeplink login to look for {loginSource} Oauth");
            DeepLinkProcessing dlp = GameObject.FindObjectOfType<DeepLinkProcessing>();
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
            loginMethodText.text = WebRequestManager.Singleton.IsCheckingGameVersion ? "Checking Game Version..." : WebRequestManager.Singleton.IsLoggingIn ? "Logging In..." : "Please Select Login Method";

            startHubServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;
            startLobbyServerButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;
            startAutoClientButton.interactable = !WebRequestManager.Singleton.IsRefreshingServers;

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
                button.interactable = !WebRequestManager.Singleton.IsLoggingIn & !WebRequestManager.Singleton.IsCheckingGameVersion;
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
            if (PlatformRichPresence.instance != null)
            {
                //Change logic here that would handle scenario where the player is host.
                PlatformRichPresence.instance.UpdatePlatformStatus("Logging to Vi", "Login Menu");
            }
        }
    }
}