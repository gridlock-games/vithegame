using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;

namespace Vi.Core
{
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

        private void Awake()
        {
            _singleton = this;
        }

        private const string APIURL = "154.90.35.191/";

        public List<Server> Servers { get; private set; } = new List<Server>();

        public bool IsRefreshingServers { get; private set; }
        public void RefreshServers() { StartCoroutine(ServerGetRequest()); }
        private IEnumerator ServerGetRequest()
        {
            if (IsRefreshingServers) { yield break; }
            IsRefreshingServers = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "servers/duels");
            yield return getRequest.SendWebRequest();

            Servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.ServerGetRequest() " + getRequest.error + APIURL + "servers/duels");
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                if (json != "[]")
                {
                    foreach (string jsonSplit in json.Split("},"))
                    {
                        string finalJsonElement = jsonSplit;
                        if (finalJsonElement[0] == '[')
                            finalJsonElement = finalJsonElement.Remove(0, 1);
                        if (finalJsonElement[^1] == ']')
                            finalJsonElement = finalJsonElement.Remove(finalJsonElement.Length - 1, 1);
                        if (finalJsonElement[^1] != '}')
                            finalJsonElement += "}";
                        Servers.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                    }
                }
            }
            catch
            {
                Servers = new List<Server>() { new Server("1", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777"), new Server("2", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776") };
            }

            getRequest.Dispose();
            IsRefreshingServers = false;
        }
        
        public IEnumerator ServerPutRequest(ServerPutPayload payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "servers/duels", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.ServerPutRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator ServerPostRequest(ServerPostPayload payload)
        {
            WWWForm form = new WWWForm();
            form.AddField("type", payload.type);
            form.AddField("population", payload.population);
            form.AddField("progress", payload.progress);
            form.AddField("ip", payload.ip);
            form.AddField("label", payload.label);
            form.AddField("port", payload.port);

            UnityWebRequest postRequest = UnityWebRequest.Post(APIURL + "servers/duels", form);
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.ServerPostRequest()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct Server
        {
            public string _id;
            public int type;
            public int population;
            public int progress;
            public string ip;
            public string label;
            public string __v;
            public string port;

            public Server(string _id, int type, int population, int progress, string ip, string label, string __v, string port)
            {
                this._id = _id;
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.__v = __v;
                this.port = port;
            }
        }

        public struct ServerPostPayload
        {
            public int type;
            public int population;
            public int progress;
            public string ip;
            public string label;
            public string port;

            public ServerPostPayload(int type, int population, int progress, string ip, string label, string port)
            {
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.port = port;
            }
        }

        public struct ServerPutPayload
        {
            public string serverId;
            public int population;
            public int progress;
            public string label;
            public string port;

            public ServerPutPayload(string serverId, int population, int progress, string label, string port)
            {
                this.serverId = serverId;
                this.population = population;
                this.progress = progress;
                this.label = label;
                this.port = port;
            }
        }
        
        // TODO Change the string at the end to be the account ID of whoever we sign in under
        //private string currentlyLoggedInUserId = "652b4e237527296665a5059b";
        public bool IsLoggedIn { get; private set; }
        public bool IsLoggingIn { get; private set; }
        public string LogInErrorText { get; private set; }
        private string currentlyLoggedInUserId = "";

        public IEnumerator Login(string username, string password)
        {
            IsLoggingIn = true;
            LoginPayload payload = new LoginPayload(username, password);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "auth/users/login", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login()" + postRequest.error);

                IsLoggedIn = false;
                currentlyLoggedInUserId = default;

                LogInErrorText = "Server offline";
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                currentlyLoggedInUserId = loginResultPayload.userId;

                LogInErrorText = IsLoggedIn ? "" : "Invalid Username or Password";
            }

            postRequest.Dispose();
            IsLoggingIn = false;
        }

        public void Logout()
        {
            IsLoggedIn = false;
            currentlyLoggedInUserId = default;
            LogInErrorText = default;
        }

        public struct LoginPayload
        {
            public string username;
            public string password;

            public LoginPayload(string username, string password)
            {
                this.username = username;
                this.password = password;
            }
        }

        private struct LoginResultPayload
        {
            public string userId;
            public bool login;
            public bool isPlayer;

            public LoginResultPayload(string userId, bool login, bool isPlayer)
            {
                this.userId = userId;
                this.login = login;
                this.isPlayer = isPlayer;
            }
        }

        public struct CreateUserPayload
        {
            public string username;
            public string email;
            public string password;
            public bool isPlayer;

            public CreateUserPayload(string username, string email, string password)
            {
                this.username = username;
                this.email = email;
                this.password = password;
                isPlayer = true;
            }
        }

        public List<Character> Characters { get; private set; } = new List<Character>();

        public bool IsRefreshingCharacters { get; private set; }
        public void RefreshCharacters() { StartCoroutine(CharacterGetRequest()); }
        private IEnumerator CharacterGetRequest()
        {
            if (IsRefreshingCharacters) { yield break; }
            IsRefreshingCharacters = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "characters/" + currentlyLoggedInUserId);
            yield return getRequest.SendWebRequest();

            Characters.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.CharacterGetRequest() " + APIURL + "characters/" + currentlyLoggedInUserId);
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                foreach (CharacterJson jsonStruct in JsonConvert.DeserializeObject<List<CharacterJson>>(json))
                {
                    Characters.Add(jsonStruct.ToCharacter());
                }
            }
            catch
            {
                Characters = new List<Character>() { GetDefaultCharacter(), GetDefaultCharacter() };
            }

            getRequest.Dispose();
            IsRefreshingCharacters = false;
        }

        public bool IsGettingCharacterById { get; private set; }
        public Character CharacterById { get; private set; }
        public void GetCharacterById(string characterId) { StartCoroutine(CharacterByIdGetRequest(characterId)); }

        private IEnumerator CharacterByIdGetRequest(string characterId)
        {
            if (IsGettingCharacterById) { yield break; }
            IsGettingCharacterById = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "characters/" + "getCharacter/" + characterId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.CharacterByIdGetRequest() " + APIURL + "characters/" + "getCharacter/" + characterId);
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                CharacterById = JsonConvert.DeserializeObject<CharacterJson>(json).ToCharacter();
            }
            catch
            {
                CharacterById = GetDefaultCharacter();
            }

            getRequest.Dispose();
            IsGettingCharacterById = false;
        }

        public IEnumerator UpdateCharacterCosmetics(Character character)
        {
            CharacterCosmeticPutPayload payload = new CharacterCosmeticPutPayload(character._id.ToString(), character.slot, character.eyeColor.ToString(), character.hair.ToString(),
                character.bodyColor.ToString(), character.beard.ToString(), character.brows.ToString(), character.name.ToString(), character.model.ToString());

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "updateCharacterCosmetic", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterCosmetics()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator UpdateCharacterLoadout(Character character)
        {
            CharacterLoadoutPutPayload payload = new CharacterLoadoutPutPayload(character._id.ToString(), character.loadoutPreset1.loadoutSlot.ToString(),
                character.loadoutPreset1.headGearItemId.ToString(), character.loadoutPreset1.armorGearItemId.ToString(), character.loadoutPreset1.armsGearItemId.ToString(),
                character.loadoutPreset1.bootsGearItemId.ToString(), character.loadoutPreset1.weapon1ItemId.ToString(), character.loadoutPreset1.weapon2ItemId.ToString());

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "saveLoadOut", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterLoadout()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator CharacterPostRequest(Character character)
        {
            CharacterPostPayload payload = new CharacterPostPayload(character.userId.ToString(), character.slot, character.eyeColor.ToString(), character.hair.ToString(),
                character.bodyColor.ToString(), character.beard.ToString(), character.brows.ToString(), character.name.ToString(), character.model.ToString());

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public IEnumerator CharacterDisableRequest(string characterId)
        {
            CharacterDisablePayload payload = new CharacterDisablePayload(characterId);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "disableCharacter", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.CharacterDisableRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public Character GetDefaultCharacter() { return new Character("", "Human_Male", "", 0, 1, GetDefaultLoadout()); }

        public Loadout GetDefaultLoadout() { return new Loadout("1", "", "", "", "", "GreatSwordWeapon", "HammerWeapon", true); }

        public struct Character : INetworkSerializable
        {
            public FixedString32Bytes _id;
            public FixedString32Bytes name;
            public FixedString32Bytes model;
            public FixedString32Bytes bodyColor;
            public FixedString32Bytes eyeColor;
            public FixedString32Bytes beard;
            public FixedString32Bytes brows;
            public FixedString32Bytes hair;
            public CharacterAttributes attributes;
            public Loadout loadoutPreset1;
            public FixedString32Bytes userId;
            public int slot;
            public int level;
            public int experience;

            public Character(string _id, string model, string name, int experience, int level, Loadout loadoutPreset1)
            {
                slot = 0;
                this._id = _id;
                this.model = model;
                this.name = name;
                this.experience = experience;
                bodyColor = "";
                eyeColor = "";
                beard = "";
                brows = "";
                hair = "";
                attributes = new CharacterAttributes();
                userId = Singleton.currentlyLoggedInUserId;
                this.level = level;
                this.loadoutPreset1 = loadoutPreset1;
            }

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level, Loadout loadoutPreset1)
            {
                slot = 0;
                this._id = _id;
                this.model = model;
                this.name = name;
                this.experience = experience;
                this.bodyColor = bodyColor;
                this.eyeColor = eyeColor;
                this.beard = beard;
                this.brows = brows;
                this.hair = hair;
                attributes = new CharacterAttributes();
                userId = Singleton.currentlyLoggedInUserId;
                this.level = level;
                this.loadoutPreset1 = loadoutPreset1;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _id);
                serializer.SerializeValue(ref name);
                serializer.SerializeValue(ref model);
                serializer.SerializeValue(ref bodyColor);
                serializer.SerializeValue(ref eyeColor);
                serializer.SerializeValue(ref beard);
                serializer.SerializeValue(ref brows);
                serializer.SerializeValue(ref hair);
                serializer.SerializeNetworkSerializable(ref attributes);
                serializer.SerializeNetworkSerializable(ref loadoutPreset1);
                serializer.SerializeValue(ref userId);
                serializer.SerializeValue(ref slot);
                serializer.SerializeValue(ref level);
                serializer.SerializeValue(ref experience);
            }
        }

        public struct Loadout : INetworkSerializable
        {
            public FixedString32Bytes loadoutSlot;
            public FixedString32Bytes headGearItemId;
            public FixedString32Bytes armorGearItemId;
            public FixedString32Bytes armsGearItemId;
            public FixedString32Bytes bootsGearItemId;
            public FixedString32Bytes weapon1ItemId;
            public FixedString32Bytes weapon2ItemId;
            public bool active;

            public Loadout(string loadoutSlot, string headGearItemId, string armorGearItemId, string armsGearItemId, string bootsGearItemId, string weapon1ItemId, string weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.headGearItemId = headGearItemId;
                this.armorGearItemId = armorGearItemId;
                this.armsGearItemId = armsGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref loadoutSlot);
                serializer.SerializeValue(ref headGearItemId);
                serializer.SerializeValue(ref armorGearItemId);
                serializer.SerializeValue(ref armsGearItemId);
                serializer.SerializeValue(ref bootsGearItemId);
                serializer.SerializeValue(ref weapon1ItemId);
                serializer.SerializeValue(ref weapon2ItemId);
                serializer.SerializeValue(ref active);
            }
        }

        private struct CharacterJson
        {
            public string _id;
            public int slot;
            public string name;
            public string model;
            public int experience;
            public string bodyColor;
            public string eyeColor;
            public string beard;
            public string brows;
            public string hair;
            public string dateCreated;
            public CharacterAttributes attributes;
            public List<LoadoutJson> loadOuts;
            public bool enabled;
            public string userId;
            public int level;
            public object attack;
            public object hp;
            public object stamina;
            public object critChance;
            public object crit;
            public string id;

            public Character ToCharacter()
            {
                return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level, Singleton.GetDefaultLoadout());
            }
        }

        private struct LoadoutJson
        {
            public string loadoutSlot;
            public string headGearItemId;
            public string armorGearItemId;
            public string armsGearItemId;
            public string bootsGearItemId;
            public string weapon1ItemId;
            public string weapon2ItemId;
            public bool active;

            public LoadoutJson(string loadoutSlot, string headGearItemId, string armorGearItemId, string armsGearItemId, string bootsGearItemId, string weapon1ItemId, string weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.headGearItemId = headGearItemId;
                this.armorGearItemId = armorGearItemId;
                this.armsGearItemId = armsGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }
        }

        public struct CharacterAttributes : INetworkSerializable
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref strength);
                serializer.SerializeValue(ref vitality);
                serializer.SerializeValue(ref agility);
                serializer.SerializeValue(ref dexterity);
                serializer.SerializeValue(ref intelligence);
            }
        }

        private struct CharacterPostPayload
        {
            public string userId;
            public NestedCharacter character;

            public CharacterPostPayload(string userId, int slot, string eyeColor, string hair, string bodyColor, string beard, string brows, string name, string model)
            {
                this.userId = userId;
                character = new NestedCharacter()
                {
                    slot = slot,
                    eyeColor = eyeColor,
                    hair = hair,
                    bodyColor = bodyColor,
                    beard = beard,
                    brows = brows,
                    name = name,
                    model = model
                };
            }

            public struct NestedCharacter
            {
                public int slot;
                public string eyeColor;
                public string hair;
                public string bodyColor;
                public string beard;
                public string brows;
                public string name;
                public string model;
            }
        }

        private struct CharacterCosmeticPutPayload
        {
            public string id;
            public int slot;
            public string eyeColor;
            public string hair;
            public string bodyColor;
            public string beard;
            public string brows;
            public string name;
            public string model;

            public CharacterCosmeticPutPayload(string id, int slot, string eyeColor, string hair, string bodyColor, string beard, string brows, string name, string model)
            {
                this.id = id;
                this.slot = slot;
                this.eyeColor = eyeColor;
                this.hair = hair;
                this.bodyColor = bodyColor;
                this.beard = beard;
                this.brows = brows;
                this.name = name;
                this.model = model;
            }
        }

        private struct CharacterLoadoutPutPayload
        {
            public string charId;
            public NestedCharacterLoadoutPutPayload loadout;

            public CharacterLoadoutPutPayload(string charId, string loadoutSlot, string headGearItemId, string armorGearItemId, string armsGearItemId, string bootsGearItemId, string weapon1ItemId, string weapon2ItemId)
            {
                this.charId = charId;
                loadout = new NestedCharacterLoadoutPutPayload()
                {
                    loadoutSlot = loadoutSlot,
                    headGearItemId = headGearItemId,
                    armorGearItemId = armorGearItemId,
                    armsGearItemId = armsGearItemId,
                    bootsGearItemId = bootsGearItemId,
                    weapon1ItemId = weapon1ItemId,
                    weapon2ItemId = weapon2ItemId
                };
            }
        }

        private struct NestedCharacterLoadoutPutPayload
        {
            public string loadoutSlot;
            public string headGearItemId;
            public string armorGearItemId;
            public string armsGearItemId;
            public string bootsGearItemId;
            public string weapon1ItemId;
            public string weapon2ItemId;
        }

        private struct CharacterDisablePayload
        {
            public string characterId;

            public CharacterDisablePayload(string characterId)
            {
                this.characterId = characterId;
            }
        }

        private void Start()
        {
            StartCoroutine(CreateItems());
        }

        private IEnumerator CreateItems()
        {
            if (!Application.isEditor) { Debug.LogError("Trying to create items from a non-editor instance!"); yield break; }

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

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            for (int i = 0; i < weaponOptions.Length; i++)
            {
                CharacterReference.WeaponOption weaponOption = weaponOptions[i];

                if (itemList.Exists(item => item._id == weaponOption.itemWebId)) { continue; }

                Debug.Log("Creating weapon item: " + (i+1) + " of " + weaponOptions.Length);

                CreateItemPayload payload = new CreateItemPayload(weaponOption.weapon.name, ItemClass.WEAPON, false, weaponOption.weapon.name, 1, 1, 1, 1, 1);

                string json = JsonConvert.SerializeObject(payload);
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

                UnityWebRequest postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Post request error in WebRequestManager.CreateItems()" + postRequest.error);
                }

                weaponOption.itemWebId = postRequest.downloadHandler.text;

                postRequest.Dispose();
            }

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetEquipmentOptions();

            for (int i = 0; i < wearableEquipmentOptions.Count; i++)
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = wearableEquipmentOptions[i];

                if (itemList.Exists(item => item._id == wearableEquipmentOption.itemWebId)) { continue; }

                Debug.Log("Creating armor item: " + (i + 1) + " of " + wearableEquipmentOptions.Count);

                CreateItemPayload payload = new CreateItemPayload(wearableEquipmentOption.wearableEquipmentPrefab.name, ItemClass.ARMOR, false, wearableEquipmentOption.wearableEquipmentPrefab.name, 1, 1, 1, 1, 1);

                string json = JsonConvert.SerializeObject(payload);
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

                UnityWebRequest postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Post request error in WebRequestManager.CreateItems()" + postRequest.error);
                }

                wearableEquipmentOption.itemWebId = postRequest.downloadHandler.text;

                postRequest.Dispose();
            }
        }

        private struct CreateItemPayload
        {
            public string name;
            public string @class;
            public bool isCraftOnly;
            public string modelName;
            public ItemAttributes attributes;

            public CreateItemPayload(string name, ItemClass @class, bool isCraftOnly, string modelName, int agi, int dex, int @int, int str, int vit)
            {
                this.name = name;
                this.@class = @class.ToString();
                this.isCraftOnly = isCraftOnly;
                this.modelName = modelName;
                attributes = new ItemAttributes() { agi = agi, dex = dex, @int = @int, str = str, vit = vit };
            }
        }

        private enum ItemClass
        {
            WEAPON,
            ARMOR,
            ETC
        }

        private struct ItemAttributes
        {
            public int str;
            public int agi;
            public int @int;
            public int vit;
            public int dex;
        }

        private struct Item
        {
            public string _id;
            public string @class;
            public string name;
            public int weight;
            public ItemAttributes attributes;
            public string isCraftOnly;
            public bool isCashExclusive;
            public bool isPassExclusive;
            public string modelName;
            public int __v;
            public string id;
        }

        public IEnumerator AddItemToCharacterInventory(string characterId, string itemId)
        {
            AddItemPayload payload = new AddItemPayload(characterId, itemId);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        private struct AddItemPayload
        {
            public string charId;
            public string itemId;

            public AddItemPayload(string characterId, string itemId)
            {
                charId = characterId;
                this.itemId = itemId;
            }
        }
    }
}