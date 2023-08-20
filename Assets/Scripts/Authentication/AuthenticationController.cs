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
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class AuthenticationController : MonoBehaviour
{
    [SerializeField] private GameObject btn_SignIn;
    [SerializeField] private GameObject btn_Signedin;
    [SerializeField] private GameObject btn_SignOut;
    [SerializeField] private GameObject btn_StartGame;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private TextMeshProUGUI infoDisplayText;

    

    private Color startGameColor;
    private Color displayNameColor;

    private DataManager datamanager;
    private void Start()
    {
        datamanager = DataManager.Instance;
        startGameColor = btn_StartGame.GetComponent<Image>().color;
        displayNameColor = displayNameInput.GetComponent<Image>().color;
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
        bool isHubInBuild = SceneUtility.GetBuildIndexByScenePath("Hub") != -1;
        bool isLobbyInBuild = SceneUtility.GetBuildIndexByScenePath("Lobby") != -1;
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null | !(isHubInBuild & isLobbyInBuild))
        {
            StartCoroutine(StartServer(IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim()).ToString()));
        }
        else // If we are not a headless build
        {
            bool signedIn = PlayerPrefs.HasKey("email");

            // if( btn_SignIn ) {
            //     btn_SignIn.SetActive(!signedIn);
            //     btn_StartGame.SetActive(signedIn);
            // }

            if ( btn_SignIn && btn_StartGame && displayNameInput) {
                // btn_Signedin.SetActive(signedIn);
                // btn_SignOut.SetActive(signedIn);
                // displayNameInput.gameObject.SetActive(signedIn);

                if (signedIn)
                {
                    // btn_SignIn.GetComponent<Button>().interactable = false;
                    btn_SignIn.GetComponent<Image>().color = new Color(192f / 255f, 192f / 255f, 192f / 255f);
                    btn_StartGame.GetComponent<Button>().interactable = true;
                    btn_StartGame.GetComponent<Image>().color = startGameColor;
                    displayNameInput.interactable = true;
                    displayNameInput.GetComponent<Image>().color = displayNameColor;
                    infoDisplayText.SetText("Provide your IGN and click on play to start");
                }
                else
                {
                    // btn_SignIn.GetComponent<Button>().interactable = true;
                    btn_StartGame.GetComponent<Button>().interactable = false;
                    btn_StartGame.GetComponent<Image>().color = new Color(192f / 255f, 192f / 255f, 192f / 255f);
                    displayNameInput.interactable = false;
                    displayNameInput.GetComponent<Image>().color = new Color(192f / 255f, 192f / 255f, 192f / 255f);
                    infoDisplayText.SetText("Sign-in with Google to start");
                }

                if (transitioningToCharacterSelect)
                {
                    btn_StartGame.GetComponent<Button>().interactable = false;
                    displayNameInput.interactable = false;
                    infoDisplayText.SetText("Loading Character Select Screen, please wait...");
                }
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

    public void SignInv2() {
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
            StoreClient(displayNameInput.text);
        }
    }

    public void SignOut()
    {
        Debug.Log("Deleting all player prefs");
        PlayerPrefs.DeleteAll();
    }

    private bool startServerCalled;
    private IEnumerator StartServer(string targetIP)
    {
        if (startServerCalled) { yield break; }
        startServerCalled = true;
        var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        networkTransport.ConnectionData.Address = targetIP;

        UnityWebRequest getRequest = UnityWebRequest.Get(ClientManager.serverEndPointURL);

        yield return getRequest.SendWebRequest();

        if (getRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Get Request Error in ClientManager.UpdateServerPopulation() " + getRequest.error);
        }

        string json = getRequest.downloadHandler.text;
        List<int> portList = new List<int>();

        foreach (string jsonSplit in json.Split("},"))
        {
            string finalJsonElement = jsonSplit;
            if (finalJsonElement[0] == '[')
            {
                finalJsonElement = finalJsonElement.Remove(0, 1);
            }

            if (finalJsonElement[^1] == ']')
            {
                finalJsonElement = finalJsonElement.Remove(finalJsonElement.Length - 1, 1);
            }

            if (finalJsonElement[^1] != '}')
            {
                finalJsonElement += "}";
            }

            ClientManager.Server server = JsonUtility.FromJson<ClientManager.Server>(finalJsonElement);

            if (server.ip == networkTransport.ConnectionData.Address)
                portList.Add(int.Parse(server.port));
        }

        foreach (int port in portList)
        {
            Debug.Log(port);
        }

        if (NetworkManager.Singleton.StartServer())
        {
            // If lobby is in our build settings, change scene to lobby. Otherwise, change scene to hub.
            if (SceneUtility.GetBuildIndexByScenePath("Lobby") != -1)
            {
                networkTransport.ConnectionData.Port = 7000;

                Debug.Log("Started Server at " + networkTransport.ConnectionData.Address + ". Make sure you opened port " + networkTransport.ConnectionData.Port + " for UDP traffic!");

                ClientManager.Singleton.ChangeScene("Lobby", false);
            }
            else
            {
                networkTransport.ConnectionData.Port = 7777;
                
                Debug.Log("Started Server at " + networkTransport.ConnectionData.Address + ". Make sure you opened port " + networkTransport.ConnectionData.Port + " for UDP traffic!");

                ClientManager.Singleton.ChangeScene("Hub", true, "OutdoorCastleArena");
            }
        }
    }

    private void StoreClient(string displayName)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(displayName.Replace(ClientManager.GetPayLoadParseString(), ""));

        SceneManager.LoadScene("CharacterSelect");
    }
}
