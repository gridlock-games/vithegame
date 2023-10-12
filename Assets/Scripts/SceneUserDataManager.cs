using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using LightPat.Core;
using GameCreator.Melee;
using UnityEngine.Networking;
using System.Collections;
using GameCreator.Characters;
using UnityEngine.Rendering;

public class SceneUserDataManager : MonoBehaviour
{
    [SerializeField] private GameObject placeholderContainer;
    [SerializeField] private Transform startSpawnLoc;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] public GameObject spotlightPrefab;
    [SerializeField] public GameObject cameraPrefab;
    [SerializeField] private GameObject charDesc_Panel;
    [SerializeField] private Text charDesc_Name;
    [SerializeField] private Text charDesc_Role;
    [SerializeField] private Text charDesc_Lore;
    [SerializeField] private Button charDesc_Select;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private Image gridImgPrefab;
    [SerializeField] private Text loadingText;
    [SerializeField] private TMP_Dropdown serverSelector;

    private GameObject selectedObject;
    private List<GameObject> placeholderObjects = new List<GameObject>();
    private int currentIndex = -1;
    public float cameraDistance = 5.0f;
    private DataManager datamanager = new DataManager();
    private bool isPreviewActive = false;
    private bool connectingToPlayerHub;

    ClientManager clientManager = new ClientManager();

    IPManager iPManager = new IPManager();

    private void Awake() 
    {
        StartCoroutine(iPManager.CheckAPI());
    }

    public void ConnectToPlayerHub()
    {
        if (connectingToPlayerHub) { return; }

        string payloadString = "";
        string displayName = "";
        foreach (char c in System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData))
        {
            if (c.ToString() == ClientManager.GetPayLoadParseString()) { break; }
            displayName += c;
        }

        Debug.Log(displayName);

        // Find player object by weapon type
        var playerModelOptions = ClientManager.Singleton.GetPlayerModelOptions();
        for (int i = 0; i < playerModelOptions.Length; i++)
        {
            if (playerModelOptions[i].playerPrefab.GetComponent<SwitchMelee>().GetCurrentWeaponType() == selectedObject.GetComponent<SwitchMelee>().GetCurrentWeaponType())
            {
                payloadString = displayName + ClientManager.GetPayLoadParseString() + i + ClientManager.GetPayLoadParseString() + skinIndex;
                Debug.Log(payloadString);
                break;
            }
        }

        if (payloadString == "")
        {
            Debug.LogError("Couldn't find a matching player prefab in ClientManager's list. Change the weapon type in switch melee of one of the player prefabs to match your chosen character's weapon type.");
            return;
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(payloadString);

        connectingToPlayerHub = true;

        // Disable character buttons while scene loads
        foreach (GameObject characterOptionImage in characterOptionImages)
        {
            characterOptionImage.GetComponent<Button>().interactable = false;
        }

        loadingText.text = "Connecting to player hub...";
        NetworkManager.Singleton.StartClient();

        var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        Debug.Log("Started Client at " + networkTransport.ConnectionData.Address + ". Port: " + networkTransport.ConnectionData.Port);
    }

    public void UpdateTargetIP()
    {
        var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        networkTransport.ConnectionData.Address = playerHubServerList[serverSelector.value].ip;
        networkTransport.ConnectionData.Port = ushort.Parse(playerHubServerList[serverSelector.value].port);
    }

    private int skinIndex;
    public void ChangeSkin()
    {
        // Find player object by weapon type
        GameObject[] skinArray = new GameObject[0];
        var playerModelOptions = ClientManager.Singleton.GetPlayerModelOptions();
        for (int i = 0; i < playerModelOptions.Length; i++)
        {
            if (playerModelOptions[i].playerPrefab.GetComponent<SwitchMelee>().GetCurrentWeaponType() == selectedObject.GetComponent<SwitchMelee>().GetCurrentWeaponType())
            {
                skinArray = playerModelOptions[i].skinOptions;
                break;
            }
        }

        skinIndex += 1;
        if (skinIndex > skinArray.Length-1) { skinIndex = 0; }

        selectedObject.GetComponent<CharacterAnimator>().ChangeModel(skinArray[skinIndex]);
    }

    private List<ClientManager.Server> playerHubServerList = new List<ClientManager.Server>();
    private IEnumerator RefreshServerList()
    {
        serverSelector.ClearOptions();
        // Get list of servers in the API
        UnityWebRequest getRequest = UnityWebRequest.Get(iPManager.ServerAPIURL);

        yield return getRequest.SendWebRequest();

        if (getRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get Request Error in SceneUserDataManager.ConnectToPlayerHubCoroutine() " + getRequest.error);
        }

        string json = getRequest.downloadHandler.text;

        bool playerHubServerFound = false;

        if (json != "[]")
        {
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

                if (server.type == 1)
                {
                    playerHubServerFound = true;
                    playerHubServerList.Add(server);
                }
            }
        }

        if (!playerHubServerFound)
        {
            Debug.LogError("Player Hub Server not found in API. Is there a server with the type set to 1?");
            yield break;
        }

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (ClientManager.Server server in playerHubServerList)
        {
            options.Add(new TMP_Dropdown.OptionData(server.label + " | " + server.ip + " | " + server.port));
        }

        serverSelector.AddOptions(options);

        var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        networkTransport.ConnectionData.Address = playerHubServerList[0].ip;
        networkTransport.ConnectionData.Port = ushort.Parse(playerHubServerList[0].port);
        getRequest.Dispose();
    }

    void Start()
    {
        this.datamanager = DataManager.Instance;
        Instantiate(cameraPrefab);
        this.InitDataReferences();
        StartCoroutine(RefreshServerList());
    }

    private void Update()
    {
        charDesc_Select.gameObject.SetActive(playerHubServerList.Count > 0);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null & playerHubServerList.Count > 0)
        {
            ConnectToPlayerHub();
        }
    }

    private void InitDataReferences()
    {
        var _userdata = datamanager;
        if (_userdata)
        {
            if (nameTMP)
            {
                nameTMP.text = this.datamanager.Data.account_name;
            }
            this.PostCharacterSelectAnalytics("CharacterSelect");
            this.SpawnGameObjectsHorizontally();
        }
    }

    private List<GameObject> characterOptionImages = new List<GameObject>();
    private void SpawnGameObjectsHorizontally()
    {
        gridLayoutGroup.padding = new RectOffset(10, 10, 60, 0);

        var playerModelOptions = ClientManager.Singleton.GetPlayerModelOptions();
        for (int i = 0; i < playerModelOptions.Length; i++)
        {
            gridImgPrefab.sprite = playerModelOptions[i].characterImage;
            gridImgPrefab.gameObject.name = playerModelOptions[i].name;
            gridImgPrefab.gameObject.SetActive(true);
            characterOptionImages.Add(Instantiate(gridImgPrefab.gameObject, gridLayoutGroup.transform));
        }

        SelectGameObject(null);
    }

    private void SelectPreviousObject()
    {
        if (placeholderObjects.Count == 0)
            return;

        // Decrement the current index
        currentIndex--;

        // Wrap around to the last index if the current index is less than 0
        if (currentIndex < 0)
        {
            currentIndex = placeholderObjects.Count - 1;
        }

        // Select the game object at the current index
        SelectGameObject(placeholderObjects[currentIndex]);
    }

    private void SelectNextObject()
    {
        if (placeholderObjects.Count == 0)
            return;

        // Increment the current index
        currentIndex++;

        // Wrap around to the first index if the current index is greater than or equal to the length of the array
        if (currentIndex >= placeholderObjects.Count)
        {
            currentIndex = 0;
        }

        // Select the game object at the current index
        SelectGameObject(placeholderObjects[currentIndex]);
    }

    public void SelectGameObject(GameObject selectedObject)
    {
        if (isPreviewActive) return;

        ClientManager.PlayerModelOption charDesc = null;

        if (this.selectedObject == null)
        {
            charDesc = ClientManager.Singleton.GetPlayerModelOptions()[0];
        }
        else
        {
            Destroy(this.selectedObject, 0f);
            charDesc = ClientManager.Singleton.GetPlayerModelOptions().FirstOrDefault(model => model.name == selectedObject.name.Replace("(Clone)", ""));
        }

        if (charDesc == null) return;
        if (charDesc.playerPrefab == this.selectedObject) return;

        Vector3 spawnPosition = startSpawnLoc.position;

        this.charDesc_Name.text = charDesc.name;
        this.charDesc_Role.text = charDesc.role;
        this.charDesc_Lore.text = charDesc.characterDescription;

        // Store the selected game object
        this.selectedObject = Instantiate(charDesc.playerPrefab, spawnPosition, Quaternion.Euler(0f, 180f, 0f));
        this.selectedObject.GetComponent<SwitchMelee>().SwitchWeaponBeforeSpawn();
        skinIndex = -1;
        ChangeSkin();
    }

    public void PostCharacterSelectAnalytics(string _panel, string _character = "0")
    {
        var _userdata = datamanager.Data;
        var _shopData = _userdata.characterAnalytics.FirstOrDefault(x => x.panelName == _panel);

        var _panelData = new UserModel.ShopAnalyticsModel
        {
            panelName = _panel,
            dateTime_added = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
            count = 1
        };
        if (_shopData == null)
        {
            if (_character != "0")
            {
                _panelData.character.Add(new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = 1
                });
            }
            _userdata.characterAnalytics.Add(_panelData);
            datamanager.PostUserdata(_userdata, false);
            return;
        }

        var _index = _userdata.characterAnalytics.IndexOf(_shopData);
        _panelData.count += _shopData.count;
        if (_character != "0")
        {
            var _chardata = _userdata.characterAnalytics[_index].character.FirstOrDefault(x => x.name == _character);
            if (_chardata == null)
            {
                _userdata.characterAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = 1
                });
            }
            else
            {
                var _charIndex = _userdata.characterAnalytics[_index].character.IndexOf(_chardata);
                _userdata.characterAnalytics[_index].character[_charIndex] = new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = _chardata.count + 1
                };
            }
        }
        else
        {
            _userdata.characterAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
            {
                name = _character,
                count = 1
            });
        }
        _userdata.characterAnalytics[_index] = new UserModel.ShopAnalyticsModel
        {
            panelName = _panelData.panelName,
            dateTime_added = _panelData.dateTime_added,
            count = _panelData.count,
            character = _userdata.characterAnalytics[_index].character
        };

        datamanager.PostUserdata(_userdata, false);
    }

    private void OnDrawGizmosSelected()
    {
        if (selectedObject != null)
        {
            // Draw a wire sphere around the selected game object
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(selectedObject.transform.position, 1f);
        }
    }

    private void NextScene()
    {
        // Selected CharacterObject is this.selectedObject
    }
}
