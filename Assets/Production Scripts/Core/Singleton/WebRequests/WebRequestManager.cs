using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;
using System.Text.RegularExpressions;
using System.Linq;
using Vi.Utility;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Vi.Core
{
    [RequireComponent(typeof(UserManager))]
    [RequireComponent(typeof(ServerManager))]
    [RequireComponent(typeof(CharacterManager))]
    [RequireComponent(typeof(LeaderboardManager))]
    public class WebRequestManager : MonoBehaviour
    {
        private static WebRequestManager _singleton;

        public static WebRequestManager Singleton
        {
            get
            {
                return _singleton;
            }
        }

        public UserManager UserManager { get; private set; }
        public ServerManager ServerManager { get; private set; }
        public CharacterManager CharacterManager { get; private set; }
        public LeaderboardManager LeaderboardManager { get; private set; }

        private void Awake()
        {
            UserManager = GetComponent<UserManager>();
            ServerManager = GetComponent<ServerManager>();
            CharacterManager = GetComponent<CharacterManager>();
            LeaderboardManager = GetComponent<LeaderboardManager>();

            _singleton = this;

            if (FasterPlayerPrefs.Singleton.HasString("APIURL"))
            {
                SetAPIURL(FasterPlayerPrefs.Singleton.GetString("APIURL"));
            }
        }

        public const string ProdAPIURL = "http://38.60.246.146:80/";
        public const string DevAPIURL = "http://154.90.36.42:80/";

        private string APIURL = ProdAPIURL;

        public string GetAPIURL(bool removeTrailingSlash)
        {
            if (removeTrailingSlash)
            {
                return APIURL[0..^1];
            }
            else
            {
                return APIURL;
            }
        }

        public void SetAPIURL(string newAPIURL)
        {
            APIURL = newAPIURL + "/";
            FasterPlayerPrefs.Singleton.SetString("APIURL", newAPIURL);

            CheckGameVersion(true);
            UserManager.Logout();
        }

        private void Start()
        {
            CheckGameVersion(false);
#if UNITY_EDITOR
            //StartCoroutine(CreateItems());
#endif
        }

#if UNITY_EDITOR
        private IEnumerator CreateItems()
        {
            if (!Application.isEditor) { Debug.LogError("Trying to create items from a non-editor instance!"); yield break; }

            if (FasterPlayerPrefs.InternetReachability == NetworkReachability.NotReachable) { Debug.LogWarning("No internet connection, can't create items"); yield break; }

            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "items/getItems");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.CreateItems() " + getRequest.error + APIURL + "servers/duels");
                getRequest.Dispose();
                yield break;
            }

            List<Item> itemList = JsonConvert.DeserializeObject<List<Item>>(getRequest.downloadHandler.text);

            getRequest.Dispose();

            yield return new WaitUntil(() => PlayerDataManager.IsCharacterReferenceLoaded());
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            for (int i = 0; i < weaponOptions.Length; i++)
            {
                CharacterReference.WeaponOption weaponOption = weaponOptions[i];

                if (itemList.Exists(item => item._id == weaponOption.itemWebId)) { continue; }

                Debug.Log("Creating weapon item: " + (i + 1) + " of " + weaponOptions.Length + " " + weaponOption.weapon.name);

                CreateItemPayload payload = new CreateItemPayload(ItemClass.WEAPON, weaponOption.name, 1, 1, 1, 1, 1, 1, false, false, false, weaponOption.isBasicGear,
                    weaponOption.weapon.name, weaponOption.weapon.name, weaponOption.weapon.name, weaponOption.weapon.name);

                string json = JsonConvert.SerializeObject(payload);
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

                UnityWebRequest postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                    postRequest.SetRequestHeader("Content-Type", "application/json");
                    yield return postRequest.SendWebRequest();
                }

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Post request error in WebRequestManager.CreateItems()" + postRequest.error);
                }

                weaponOption.itemWebId = postRequest.downloadHandler.text;
                UnityEditor.EditorUtility.SetDirty(PlayerDataManager.Singleton.GetCharacterReference());

                postRequest.Dispose();
            }

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetAllArmorEquipmentOptions();

            for (int i = 0; i < wearableEquipmentOptions.Count; i++)
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = wearableEquipmentOptions[i];

                if (CharacterReference.equipmentTypesThatAreForCharacterCustomization.Contains(wearableEquipmentOption.equipmentType)) { continue; }
                if (itemList.Exists(item => item._id == wearableEquipmentOption.itemWebId)) { continue; }

                Debug.Log("Creating armor item: " + (i + 1) + " of " + wearableEquipmentOptions.Count + " " + wearableEquipmentOption.name);

                CreateItemPayload payload = new CreateItemPayload(ItemClass.ARMOR, wearableEquipmentOption.name, 1, 1, 1, 1, 1, 1, false, false, false, wearableEquipmentOption.isBasicGear,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanMale, PlayerDataManager.Singleton.GetCharacterReference().EmptyWearableEquipment).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanFemale, PlayerDataManager.Singleton.GetCharacterReference().EmptyWearableEquipment).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcMale, PlayerDataManager.Singleton.GetCharacterReference().EmptyWearableEquipment).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcFemale, PlayerDataManager.Singleton.GetCharacterReference().EmptyWearableEquipment).name);

                string json = JsonConvert.SerializeObject(payload);
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

                UnityWebRequest postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                    postRequest.SetRequestHeader("Content-Type", "application/json");
                    yield return postRequest.SendWebRequest();
                }

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Post request error in WebRequestManager.CreateItems()" + postRequest.error);
                }

                wearableEquipmentOption.itemWebId = postRequest.downloadHandler.text;
                UnityEditor.EditorUtility.SetDirty(PlayerDataManager.Singleton.GetCharacterReference());

                postRequest.Dispose();
            }
        }

        private struct CreateItemPayload
        {
            public string @class;
            public string name;
            public int weight;

            [JsonProperty("attributes.strength")]
            public int attributesstrength;

            [JsonProperty("attributes.agility")]
            public int attributesagility;

            [JsonProperty("attributes.zzzz")]
            public int attributeszzzz;

            [JsonProperty("attributes.vitality")]
            public int attributesvitality;

            [JsonProperty("attributes.dexterity")]
            public int attributesdexterity;
            public bool isCraftOnly;
            public bool isCashExclusive;
            public bool isPassExclusive;
            public CharacterManager.ModelNames modelNames;
            public bool isBasicGear;

            public CreateItemPayload(ItemClass @class, string name, int weight,
                int attributesstrength, int attributesagility, int attributeszzzz, int attributesvitality, int attributesdexterity,
                bool isCraftOnly, bool isCashExclusive, bool isPassExclusive, bool isBasicGear,
                string humanMaleModelName, string humanFemaleModelName, string orcMaleModelName, string orcFemaleModelName)
            {
                this.@class = @class.ToString();
                this.name = name;
                this.weight = weight;
                this.attributesstrength = attributesstrength;
                this.attributesagility = attributesagility;
                this.attributeszzzz = attributeszzzz;
                this.attributesvitality = attributesvitality;
                this.attributesdexterity = attributesdexterity;
                this.isCraftOnly = isCraftOnly;
                this.isCashExclusive = isCashExclusive;
                this.isPassExclusive = isPassExclusive;
                this.modelNames = new CharacterManager.ModelNames(humanMaleModelName, humanFemaleModelName, orcMaleModelName, orcFemaleModelName);
                this.isBasicGear = isBasicGear;
            }
        }

        private enum ItemClass
        {
            WEAPON,
            ARMOR,
            ETC
        }

        private struct Item
        {
            public string _id;
            public string @class;
            public string name;
            public int weight;
            private ItemAttributes attributes;
            public string isCraftOnly;
            public bool isCashExclusive;
            public bool isPassExclusive;
            public string modelName;
            public int __v;
            public string id;

            private struct ItemAttributes
            {
                public int str;
                public int agi;
                public int @int;
                public int vit;
                public int dex;
            }

        }
#endif

        public void CheckGameVersion(bool force)
        {
            if (!force)
            {
                if (IsCheckingGameVersion) { return; }
            }
            if (gameVersionCheckCoroutine != null) { StopCoroutine(gameVersionCheckCoroutine); }
            gameVersionCheckCoroutine = StartCoroutine(CheckGameVersionRequest());
        }

        public bool GameIsUpToDate { get; private set; }
        public string GameVersionErrorMessage { get; private set; } = "";

        public string GetGameVersion() { return gameVersion.Version; }

        [SerializeField] private GameObject alertBoxPrefab;
        public bool IsCheckingGameVersion { get; private set; }
        private GameVersion gameVersion;
        private Coroutine gameVersionCheckCoroutine;
        private IEnumerator CheckGameVersionRequest()
        {
            if (FasterPlayerPrefs.InternetReachability == NetworkReachability.NotReachable)
            {
                yield return new WaitUntil(() => SceneManager.GetActiveScene() == gameObject.scene);

                if (!FasterPlayerPrefs.IsPlayingOffline)
                {
                    Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Error while checking game version, are you connected to the internet?";
                    GameIsUpToDate = true;
                    GameVersionErrorMessage = "";
                }

                UserManager.SetOfflineVariables();
                yield break;
            }

            IsCheckingGameVersion = true;

            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/version");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.VersionGetRequest() " + getRequest.error + APIURL + "game/version");
                getRequest.Dispose();
                if (FasterPlayerPrefs.InternetReachability != NetworkReachability.NotReachable)
                {
                    yield return new WaitUntil(() => SceneManager.GetActiveScene() == gameObject.scene);
                    Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Error while checking game version. Servers may be offline.";
                }
                IsCheckingGameVersion = false;
                yield break;
            }

            Version version = JsonConvert.DeserializeObject<Version>(getRequest.downloadHandler.text);
            gameVersion = version.gameversion;

            getRequest.Dispose();

            GameIsUpToDate = Application.version == gameVersion.Version;
            GameVersionErrorMessage = "";

            IsCheckingGameVersion = false;

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                if (float.Parse(Application.version) > float.Parse(gameVersion.Version))
                {
                    Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Servers not updated yet, online play is unavailable.";
                    GameVersionErrorMessage = "SERVERS NOT UPDATED YET";
                }
                else if (float.Parse(Application.version) < float.Parse(gameVersion.Version))
                {
                    Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Game is out of date, please update.";
                    GameVersionErrorMessage = "GAME IS OUT OF DATE";
                }
            }
        }

        private class GameVersion
        {
            public string Version;
            public string Type;

            public GameVersion(string version, string type)
            {
                Version = version;
                Type = type;
            }
        }

        private class Version
        {
            public GameVersion gameversion;

            public Version(string version, string type)
            {
                gameversion = new GameVersion(version, type);
            }
        }

        public IEnumerator SetGameVersion()
        {
            Version payload = new Version(Application.version, "Live");

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "game/version", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "game/version", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.SetGameVersion()" + postRequest.error);
            }
            postRequest.Dispose();
        }
    }
}