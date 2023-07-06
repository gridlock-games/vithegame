using System;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using System.Net;
using UnityEngine.Rendering;
using UnityEngine.UI;
using LightPat.Core;

public class AuthenticationController : MonoBehaviour
{
    public string playerHubIPAddress = "128.199.214.19";

    [SerializeField] private GameObject btn_SignIn;
    [SerializeField] private GameObject btn_Signedin;
    [SerializeField] private GameObject btn_SignOut;
    [SerializeField] private GameObject btn_StartGame;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private TextMeshProUGUI infoDisplayText;

    private DataManager datamanager;
    private void Start()
    {
        datamanager = DataManager.Instance;
        //check if data already cached if not activate sign in page
        if (PlayerPrefs.HasKey("email"))
        {
            RestClient.Get($"{datamanager.firebaseURL}.json", GetCacheResponse);
        }
    }

    private bool transitioningToCharacterSelect;
    private void Update()
    {
        // If we are a headless build
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            StartServer(IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim()).ToString());
        }
        else // If we are not a headless build
        {
            bool signedIn = PlayerPrefs.HasKey("email");

            btn_Signedin.SetActive(signedIn);
            btn_SignOut.SetActive(signedIn);
            btn_StartGame.SetActive(signedIn);
            displayNameInput.gameObject.SetActive(signedIn);

            btn_SignIn.SetActive(!signedIn);

            if (displayNameInput.text == "" & signedIn)
            {
                btn_StartGame.GetComponent<Button>().interactable = false;
                infoDisplayText.SetText("Enter a display name to play");
            }
            else
            {
                btn_StartGame.GetComponent<Button>().interactable = true;
                infoDisplayText.SetText("");
            }

            if (transitioningToCharacterSelect)
            {
                btn_StartGame.GetComponent<Button>().interactable = false;
                infoDisplayText.SetText("Loading Character Select Screen, please wait...");
            }
        }
    }

    // Callback on getting data and save as user model
    private void GetCacheResponse(RequestException error, ResponseHelper helper)
    {
        var data = AuthHelper.GetUserData(helper.Text, PlayerPrefs.GetString("email"), datamanager.secretId);
        if (data == null)
        {
            return;
        }
        data.last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        datamanager.PostUserdata(data);
    }

    public void SignIn()
    {
        if (datamanager != null)
        {
            GoogleAuth.Auth(datamanager.clientId, datamanager.secretId, (success, error, info) =>
            {
                if (success)
                {
                    //Check if data already exist in firebase. data equal to null post new data.
                    RestClient.Get($"{datamanager.firebaseURL}.json", (exception, helper) =>
                    {
                        var data = AuthHelper.GetUserData(helper.Text, info.email, datamanager.secretId);
                        if (data == null)
                        {
                            data = new UserModel
                            {
                                account_name = info.name,
                                email = info.email,
                                display_picture = info.picture,
                                date_created = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                                last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                            };
                            datamanager.PostUserdata(data);
                            return;
                        }
                        data.last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                        datamanager.PostUserdata(data);
                    });
                }
                //if Google login fail
                Debug.LogError(error);
            });
        }
    }

    private bool startingGame;
    public void StartGame()
    {
        if (startingGame) { return; }
        startingGame = true;
        if (PlayerPrefs.HasKey("email"))
        {
            transitioningToCharacterSelect = true;
            StoreClient(playerHubIPAddress, displayNameInput.text);
            SceneManager.LoadScene("CharacterSelect");
        }
    }

    public void SignOut()
    {
        Debug.Log("Deleting all player prefs");
        PlayerPrefs.DeleteAll();
    }

    private bool startServerCalled;
    private void StartServer(string targetIP)
    {
        if (startServerCalled) { return; }
        startServerCalled = true;
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;

        if (NetworkManager.Singleton.StartServer())
        {
            Debug.Log("Started Server at " + targetIP + ". Make sure you opened port 7777 for UDP traffic!");
            // If lobby is in our build settings, change scene to lobby. Otherwise, change scene to hub.
            NetworkManager.Singleton.SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath("Lobby") != -1 ? "Lobby" : "Hub", LoadSceneMode.Single);
        }
    }

    private void StoreClient(string targetIP, string displayName)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(displayName.Replace(ClientManager.GetPayLoadParseString(), ""));
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;
    }
}
