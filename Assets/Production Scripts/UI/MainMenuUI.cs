using Firebase.Auth;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.UI.SimpleGoogleSignIn;

namespace Vi.UI
{
  public class MainMenuUI : MonoBehaviour
  {
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private ContentManager contentManager;

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
    [SerializeField] private Button switchLoginFormButton;
    [SerializeField] private Text loginErrorText;

    [Header("OAuth")]
    [SerializeField] private GameObject oAuthParent;
    [SerializeField] private Button oAuthReturnBtn;
    [SerializeField] private Text oAuthMessageText;

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
      AudioListener.volume = 0;

      var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
      networkTransport.ConnectionData.Address = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

      if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
      {
        networkTransport.ConnectionData.Address = "127.0.0.1";
      }

      networkTransport.ConnectionData.Port = hubPort;
      NetworkManager.Singleton.StartServer();
      NetSceneManager.Singleton.LoadScene("Player Hub");
      NetSceneManager.Singleton.LoadScene("Player Hub Environment");
    }

    public void StartLobbyServer()
    {
      if (startServerCalled) { return; }
      startServerCalled = true;
      AudioListener.volume = 0;

      var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
      networkTransport.ConnectionData.Address = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

      if (Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer | Application.isEditor)
      {
        networkTransport.ConnectionData.Address = "127.0.0.1";
      }

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
      NetworkManager.Singleton.StartServer();
      NetSceneManager.Singleton.LoadScene("Lobby");
    }

    public void LoginWithVi()
    {
      if (PersistentLocalObjects.Singleton.HasKey("username")) { usernameInput.text = PersistentLocalObjects.Singleton.GetString("username"); } else { usernameInput.text = ""; }
      if (PersistentLocalObjects.Singleton.HasKey("password")) { passwordInput.text = PersistentLocalObjects.Singleton.GetString("password"); } else { passwordInput.text = ""; }

      initialParent.SetActive(false);

      emailInput.gameObject.SetActive(false);
      loginButton.GetComponentInChildren<Text>().text = "LOGIN";

      loginButton.onClick.RemoveAllListeners();
      loginButton.onClick.AddListener(delegate { StartCoroutine(Login()); });

      switchLoginFormButton.GetComponentInChildren<Text>().text = "CREATE ACCOUNT";
      switchLoginFormButton.onClick.RemoveAllListeners();
      switchLoginFormButton.onClick.AddListener(OpenCreateAccount);
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

      switchLoginFormButton.GetComponentInChildren<Text>().text = "GO TO LOGIN";
      switchLoginFormButton.onClick.RemoveAllListeners();
      switchLoginFormButton.onClick.AddListener(OpenViLogin);
    }

    public void OpenViLogin()
    {
      if (PersistentLocalObjects.Singleton.HasKey("username")) { usernameInput.text = PersistentLocalObjects.Singleton.GetString("username"); } else { usernameInput.text = ""; }
      if (PersistentLocalObjects.Singleton.HasKey("password")) { passwordInput.text = PersistentLocalObjects.Singleton.GetString("password"); } else { passwordInput.text = ""; }

      viLogo.enabled = false;
      initialParent.SetActive(false);
      authenticationParent.SetActive(true);

      emailInput.gameObject.SetActive(false);
      loginButton.GetComponentInChildren<Text>().text = "LOGIN";

      loginButton.onClick.RemoveAllListeners();
      loginButton.onClick.AddListener(delegate { StartCoroutine(Login()); });

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

    public void QuitGame()
    {
      Application.Quit();
    }

    public IEnumerator CreateAccount()
    {
      PersistentLocalObjects.Singleton.SetString("username", usernameInput.text);
      PersistentLocalObjects.Singleton.SetString("password", passwordInput.text);

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
      PersistentLocalObjects.Singleton.SetString("LastSignInType", "Vi");
      PersistentLocalObjects.Singleton.SetString("username", usernameInput.text);
      PersistentLocalObjects.Singleton.SetString("password", passwordInput.text);
      usernameInput.interactable = false;
      passwordInput.interactable = false;

      yield return WebRequestManager.Singleton.Login(usernameInput.text, passwordInput.text);

      welcomeUserText.text = "Welcome " + PersistentLocalObjects.Singleton.GetString("username");
      usernameInput.interactable = true;
      passwordInput.interactable = true;
    }

    //private const string googleSignInClientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
    //private const string googleSignInSecretId = "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";

    private const string googleSignInClientId = "583444002427-p8hrsdv9p38migp7db30mch3qeluodda.apps.googleusercontent.com";
    private const string googleSignInSecretId = "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";

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

    private IEnumerator WaitForGoogleAuth(GoogleAuth.GoogleIdTokenResponse tokenData)
    {
      Credential credential = GoogleAuthProvider.GetCredential(tokenData.id_token, null);
      System.Threading.Tasks.Task<AuthResult> task = auth.SignInAndRetrieveDataWithCredentialAsync(credential);

      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsCanceled)
      {
        loginErrorText.text = "Login with google was cancelled.";
        oAuthParent.SetActive(false);
        yield break;
      }

      if (task.IsFaulted)
      {
        loginErrorText.text = "Login with google encountered an error.";
        oAuthParent.SetActive(false);
        yield break;
      }

      AuthResult authResult = task.Result;
      yield return WebRequestManager.Singleton.LoginWithFirebaseUserId(authResult.User.Email, authResult.User.UserId);

      if (WebRequestManager.Singleton.IsLoggedIn)
      {
        initialParent.SetActive(false);
        oAuthParent.SetActive(false);
        welcomeUserText.text = "Welcome " + authResult.User.DisplayName;
        PersistentLocalObjects.Singleton.SetString("LastSignInType", "Google");
        PersistentLocalObjects.Singleton.SetString("GoogleIdTokenResponse", JsonUtility.ToJson(tokenData));
      }
      else
      {
        oAuthParent.SetActive(false);
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
      initialParent.SetActive(true);
    }

    private FirebaseAuth auth;

    private void Start()
    {
      initialParent.SetActive(true);
      WebRequestManager.Singleton.RefreshServers();
      startHubServerButton.gameObject.SetActive(Application.isEditor);
      startLobbyServerButton.gameObject.SetActive(Application.isEditor);
      initialErrorText.text = "";

      if (!WebRequestManager.IsServerBuild())
      {
        auth = FirebaseAuth.DefaultInstance;
        StartCoroutine(AutomaticallyAttemptLogin());
      }
    }

    private IEnumerator AutomaticallyAttemptLogin()
    {
      if (PersistentLocalObjects.Singleton.HasKey("LastSignInType"))
      {
        switch (PersistentLocalObjects.Singleton.GetString("LastSignInType"))
        {
          case "Vi":
            usernameInput.text = PersistentLocalObjects.Singleton.GetString("username");
            passwordInput.text = PersistentLocalObjects.Singleton.GetString("password");
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
            yield return WaitForGoogleAuth(JsonUtility.FromJson<GoogleAuth.GoogleIdTokenResponse>(PersistentLocalObjects.Singleton.GetString("GoogleIdTokenResponse")));

            break;

          default:
            Debug.LogError("Not sure how to handle last sign in type " + PersistentLocalObjects.Singleton.GetString("LastSignInType"));
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
      { GoogleAuth.ShutdownListner(); }
    }

    private void openDialogue(string platformname)
    {
      oAuthParent.SetActive(true);
      oAuthMessageText.text = $"Waiting for {platformname} Login";
    }

    private void Update()
    {
      loginMethodText.text = WebRequestManager.Singleton.IsLoggingIn ? "Logging in..." : "Please Select Login Method";

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
      returnButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
      switchLoginFormButton.interactable = !WebRequestManager.Singleton.IsLoggingIn;
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
      loginErrorText.text = WebRequestManager.Singleton.LogInErrorText;
    }
  }
}