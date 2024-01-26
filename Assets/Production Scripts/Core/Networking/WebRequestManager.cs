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
using UnityEngine.SceneManagement;

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

        public bool PlayingOffine { get; private set; }

        public void SetPlayingOffline(bool shouldBeOffline)
        {
            if (!SceneManager.GetSceneByName("Main Menu").isLoaded) { Debug.LogError("You are calling WebRequestManager.SetPlayingOffline() when the main menu scene isn't loaded!"); return; }
            PlayingOffine = shouldBeOffline;
        }

        private const string APIURL = "154.90.35.191/";

        public bool IsRefreshingServers { get; private set; }
        public Server[] LobbyServers { get; private set; } = new Server[0];
        public Server[] HubServers { get; private set; } = new Server[0];

        private List<Server> servers = new List<Server>();
        public void RefreshServers() { StartCoroutine(ServerGetRequest()); }
        private IEnumerator ServerGetRequest()
        {
            if (IsRefreshingServers) { yield break; }
            IsRefreshingServers = true;

            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "servers/duels");
            yield return getRequest.SendWebRequest();

            servers.Clear();
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
                        servers.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                    }
                }
            }
            catch
            {
                servers = new List<Server>() { new Server("1", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777"), new Server("2", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776") };
            }

            HubServers = servers.FindAll(item => item.type == 0).ToArray();
            LobbyServers = servers.FindAll(item => item.type == 1).ToArray();

            getRequest.Dispose();
            IsRefreshingServers = false;
        }
        
        public IEnumerator UpdateServerProgress(int progress)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }
            if (!thisServerCreated) { yield break; }

            ServerProgressPayload payload = new ServerProgressPayload(thisServer._id, progress);

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

        public IEnumerator UpdateServerPopulation(int population, string label)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }
            if (!thisServerCreated) { yield break; }

            ServerPopulationPayload payload = new ServerPopulationPayload(thisServer._id, population, thisServer.type == 0 ? "Hub" : label == "" ? "Lobby" : label);

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

        private Server thisServer;
        private bool thisServerCreated;
        public IEnumerator ServerPostRequest(ServerPostPayload payload)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }

            if (payload.type == 0)
            {
                foreach (Server server in servers)
                {
                    if (server.ip == payload.ip)
                    {
                        yield return new WaitUntil(() => !IsDeletingServer);
                        DeleteServer(server._id);
                        yield return new WaitUntil(() => !IsDeletingServer);
                    }
                }
            }

            yield return ServerGetRequest();

            foreach (Server server in servers)
            {
                if (payload.ip == server.ip & payload.port == server.port)
                {
                    thisServer = server;
                    thisServerCreated = true;
                    Debug.LogWarning("Server already exists in API!");
                    yield break;
                }
            }

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
                yield break;
            }

            thisServer = JsonConvert.DeserializeObject<Server>(postRequest.downloadHandler.text);

            postRequest.Dispose();

            yield return ServerGetRequest();

            thisServerCreated = true;
        }

        public bool IsDeletingServer { get; private set; }
        public void DeleteServer(string serverId) { StartCoroutine(DeleteServerCoroutine(serverId)); }
        private IEnumerator DeleteServerCoroutine(string serverId)
        {
            if (IsDeletingServer) { yield break; }
            IsDeletingServer = true;
            ServerDeletePayload payload = new ServerDeletePayload(serverId);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest deleteRequest = UnityWebRequest.Delete(APIURL + "servers/duels");
            deleteRequest.method = UnityWebRequest.kHttpVerbDELETE;

            deleteRequest.SetRequestHeader("Content-Type", "application/json");

            deleteRequest.uploadHandler = new UploadHandlerRaw(jsonData);

            yield return deleteRequest.SendWebRequest();

            if (deleteRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Delete request error in LobbyManagerNPC.DeleteLobby() " + deleteRequest.error);
            }
            deleteRequest.Dispose();
            IsDeletingServer = false;
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

        private struct ServerProgressPayload
        {
            public string serverId;
            public int progress;

            public ServerProgressPayload(string serverId, int progress)
            {
                this.serverId = serverId;
                this.progress = progress;
            }
        }

        private struct ServerPopulationPayload
        {
            public string serverId;
            public int population;
            public string label;

            public ServerPopulationPayload(string serverId, int population, string label)
            {
                this.serverId = serverId;
                this.population = population;
                this.label = label;
            }
        }

        private struct ServerDeletePayload
        {
            public string serverId;

            public ServerDeletePayload(string serverId)
            {
                this.serverId = serverId;
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

            if (postRequest.result != UnityWebRequest.Result.Success) { yield return postRequest.SendWebRequest(); }
            if (postRequest.result != UnityWebRequest.Result.Success) { yield return postRequest.SendWebRequest(); }
            if (postRequest.result != UnityWebRequest.Result.Success) { yield return postRequest.SendWebRequest(); }
            if (postRequest.result != UnityWebRequest.Result.Success) { yield return postRequest.SendWebRequest(); }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login() " + postRequest.error);

                IsLoggedIn = false;
                currentlyLoggedInUserId = default;
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                currentlyLoggedInUserId = loginResultPayload.userId;
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
                    break;
                case UnityWebRequest.Result.Success:
                    LogInErrorText = IsLoggedIn ? "" : "Invalid Username or Password";
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    LogInErrorText = "Server Offline";
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    LogInErrorText = "Protocol Error";
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    LogInErrorText = "Data Processing Error";
                    break;
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
            if (PlayingOffine)
            {
                Characters.Clear();
                if (PlayerPrefs.HasKey("OfflineCharacter"))
                {
                    Characters.Add(JsonConvert.DeserializeObject<CharacterJson>(PlayerPrefs.GetString("OfflineCharacter")).ToCharacter());
                }
            }
            else
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
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }

                getRequest.Dispose();
                IsRefreshingCharacters = false;
            }
        }

        public bool IsGettingCharacterById { get; private set; }
        public Character CharacterById { get; private set; }
        public void GetCharacterById(string characterId) { StartCoroutine(CharacterByIdGetRequest(characterId)); }

        private IEnumerator CharacterByIdGetRequest(string characterId)
        {
            if (PlayingOffine)
            {
                CharacterById = JsonConvert.DeserializeObject<CharacterJson>(PlayerPrefs.GetString("OfflineCharacter")).ToCharacter();
            }
            else
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
        }

        public IEnumerator UpdateCharacterCosmetics(Character character)
        {
            if (PlayingOffine)
            {
                character._id = "OfflineCharacter";
                PlayerPrefs.SetString("OfflineCharacter", JsonConvert.SerializeObject(ToCharacterJson(character)));
            }
            else
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
        }

        public IEnumerator UpdateCharacterLoadout(Character character)
        {
            Debug.Log("TODO: Update character loadout");
            yield return null;
            //CharacterLoadoutPutPayload payload = new CharacterLoadoutPutPayload(character._id.ToString(), character.loadoutPreset1.loadoutSlot.ToString(),
            //    character.loadoutPreset1.headGearItemId.ToString(), character.loadoutPreset1.armorGearItemId.ToString(), character.loadoutPreset1.armsGearItemId.ToString(),
            //    character.loadoutPreset1.bootsGearItemId.ToString(), character.loadoutPreset1.weapon1ItemId.ToString(), character.loadoutPreset1.weapon2ItemId.ToString());

            //string json = JsonConvert.SerializeObject(payload);
            //byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            //UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "saveLoadOut", jsonData);
            //putRequest.SetRequestHeader("Content-Type", "application/json");
            //yield return putRequest.SendWebRequest();

            //if (putRequest.result != UnityWebRequest.Result.Success)
            //{
            //    Debug.LogError("Put request error in WebRequestManager.UpdateCharacterLoadout()" + putRequest.error);
            //}
            //putRequest.Dispose();
        }

        public IEnumerator CharacterPostRequest(Character character)
        {
            if (PlayingOffine)
            {
                character._id = "OfflineCharacter";
                PlayerPrefs.SetString("OfflineCharacter", JsonConvert.SerializeObject(ToCharacterJson(character)));
            }
            else
            {
                CharacterPostPayload payload = new CharacterPostPayload(character);

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
        }

        public IEnumerator CharacterDisableRequest(string characterId)
        {
            if (PlayingOffine)
            {
                PlayerPrefs.DeleteKey("OfflineCharacter");
            }
            else
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
        }

        public Character GetDefaultCharacter() { return new Character("", "Human_Male", "", 0, 1, GetDefaultLoadout(CharacterReference.RaceAndGender.HumanMale), CharacterReference.RaceAndGender.HumanMale); }

        public Loadout GetDefaultLoadout(CharacterReference.RaceAndGender raceAndGender)
        {
            switch (raceAndGender)
            {
                case CharacterReference.RaceAndGender.HumanMale:
                    return new Loadout("1", "Hu_M_Helm_SMage_03_Bl", "Hu_M_Shoulders_SMage_Bl", "Hu_M_Chest_SMage_Bl", "Hu_M_Gloves_SMage_Bl",
                        "Hu_M_Belt_SMage_Bl", "Hu_M_Robe_SMage_Bl", "Hu_M_Boots_SMage_Bl", "HammerWeapon", "CrossbowWeapon", true);
                case CharacterReference.RaceAndGender.HumanFemale:
                    return new Loadout("1", "Hu_F_Helm_SMage_03_Bl", "Hu_F_Shoulders_SMage_Bl", "Hu_F_Chest_SMage_Bl", "Hu_F_Gloves_SMage_Bl",
                        "Hu_F_Belt_SMage_Bl", "Hu_F_Robe_SMage_Bl", "Hu_F_Boots_SMage_Bl", "HammerWeapon", "BrawlerWeapon", true);
                case CharacterReference.RaceAndGender.OrcMale:
                    return new Loadout("1", "Or_M_Helm_SMage_03_Bl", "Or_M_Shoulders_SMage_Bl", "Or_M_Chest_SMage_Bl", "Or_M_Gloves_SMage_Bl",
                        "Or_M_Belt_SMage_Bl", "Or_M_Robe_SMage_Bl", "Or_M_Boots_SMage_Bl", "HammerWeapon", "BrawlerWeapon", true);
                case CharacterReference.RaceAndGender.OrcFemale:
                    return new Loadout("1", "Or_F_Helm_SMage_03_Bl", "Or_F_Shoulders_SMage_Bl", "Or_F_Chest_SMage_Bl", "Or_F_Gloves_SMage_Bl",
                        "Or_F_Belt_SMage_Bl", "Or_F_Robe_SMage_Bl", "Or_F_Boots_SMage_Bl", "HammerWeapon", "CrossbowWeapon", true);
                default:
                    Debug.LogError("Not sure how to handle " + raceAndGender);
                    break;
            }

            return new Loadout("1", "65a2b5077fd3af802c750f7f", "65a2b5247fd3af802c751047", "65a2b4e27fd3af802c750e7f", "65a2b4f37fd3af802c750ef7",
                "65a2b4987fd3af802c750c83", "65a2b5177fd3af802c750fef", "65a2b4b27fd3af802c750d33", "GreatSwordWeapon", "CrossbowWeapon", true);
        }

        private CharacterJson ToCharacterJson(Character character)
        {
            string[] raceAndGenderStrings = Regex.Matches(character.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            string race = raceAndGenderStrings[0];
            string gender = raceAndGenderStrings[1];

            return new CharacterJson()
            {
                _id = character._id.ToString(),
                name = character.name.ToString(),
                model = character.model.ToString(),
                bodyColor = character.bodyColor.ToString(),
                eyeColor = character.eyeColor.ToString(),
                beard = character.beard.ToString(),
                brows = character.brows.ToString(),
                hair = character.hair.ToString(),
                attributes = new CharacterAttributes(),
                loadOuts = new List<object>(),
                userId = character.userId.ToString(),
                slot = character.slot,
                level = character.level,
                experience = character.experience,
                race = race,
                gender = gender
            };
        }

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
            public CharacterReference.RaceAndGender raceAndGender;

            public Character(string _id, string model, string name, int experience, int level, Loadout loadoutPreset1, CharacterReference.RaceAndGender raceAndGender)
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
                this.raceAndGender = raceAndGender;
            }

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level, Loadout loadoutPreset1, CharacterReference.RaceAndGender raceAndGender)
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
                this.raceAndGender = raceAndGender;
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
                serializer.SerializeValue(ref raceAndGender);
            }
        }

        public struct Loadout : INetworkSerializable
        {
            public FixedString32Bytes loadoutSlot;
            public FixedString32Bytes helmGearItemId;
            public FixedString32Bytes shouldersGearItemId;
            public FixedString32Bytes chestArmorGearItemId;
            public FixedString32Bytes glovesGearItemId;
            public FixedString32Bytes beltGearItemId;
            public FixedString32Bytes robeGearItemId;
            public FixedString32Bytes bootsGearItemId;
            public FixedString32Bytes weapon1ItemId;
            public FixedString32Bytes weapon2ItemId;
            public bool active;

            public Loadout(FixedString32Bytes loadoutSlot, FixedString32Bytes helmGearItemId, FixedString32Bytes shouldersGearItemId, FixedString32Bytes chestArmorGearItemId, FixedString32Bytes glovesGearItemId, FixedString32Bytes beltGearItemId, FixedString32Bytes robeGearItemId, FixedString32Bytes bootsGearItemId, FixedString32Bytes weapon1ItemId, FixedString32Bytes weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.helmGearItemId = helmGearItemId;
                this.shouldersGearItemId = shouldersGearItemId;
                this.chestArmorGearItemId = chestArmorGearItemId;
                this.glovesGearItemId = glovesGearItemId;
                this.beltGearItemId = beltGearItemId;
                this.robeGearItemId = robeGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref loadoutSlot);
                serializer.SerializeValue(ref helmGearItemId);
                serializer.SerializeValue(ref shouldersGearItemId);
                serializer.SerializeValue(ref chestArmorGearItemId);
                serializer.SerializeValue(ref glovesGearItemId);
                serializer.SerializeValue(ref beltGearItemId);
                serializer.SerializeValue(ref robeGearItemId);
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
            public string gender;
            public string race;
            public string dateCreated;
            public CharacterAttributes attributes;
            public List<object> loadOuts;
            public bool enabled;
            public string userId;
            public int level;
            public double attack;
            public int defense;
            public double hp;
            public int stamina;
            public double critChance;
            public double crit;
            public string id;

            public Character ToCharacter()
            {
                CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(char.ToUpper(race[0]) + race[1..].ToLower() + char.ToUpper(gender[0]) + gender[1..].ToLower());
                return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level, Singleton.GetDefaultLoadout(raceAndGender), raceAndGender);
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

            public CharacterPostPayload(Character character)
            {
                userId = character.userId.ToString();

                string[] raceAndGenderStrings = Regex.Matches(character.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();

                this.character = new NestedCharacter()
                {
                    slot = character.slot,
                    eyeColor = character.eyeColor.ToString(),
                    hair = string.IsNullOrEmpty(character.hair.ToString()) ? "null" : character.hair.ToString(),
                    bodyColor = string.IsNullOrEmpty(character.bodyColor.ToString()) ? "null" : character.bodyColor.ToString(),
                    beard = string.IsNullOrEmpty(character.beard.ToString()) ? "null" : character.beard.ToString(),
                    brows = string.IsNullOrEmpty(character.brows.ToString()) ? "null" : character.brows.ToString(),
                    name = string.IsNullOrEmpty(character.name.ToString()) ? "null" : character.name.ToString(),
                    model = string.IsNullOrEmpty(character.model.ToString()) ? "null" : character.model.ToString(),
                    race = string.IsNullOrEmpty(raceAndGenderStrings[0].ToUpper()) ? "null" : raceAndGenderStrings[0].ToUpper(),
                    gender = string.IsNullOrEmpty(raceAndGenderStrings[1].ToUpper()) ? "null" : raceAndGenderStrings[1].ToUpper()
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
                public string race;
                public string gender;
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
            private NestedCharacterLoadoutPutPayload loadout;

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
            if (Application.isEditor) { StartCoroutine(CreateItems()); }
        }

        private void Update()
        {
            if (thisServerCreated)
            {
                if (!IsRefreshingServers)
                {
                    RefreshServers();

                    if (thisServer.type == 0)
                    {
                        if (!System.Array.Exists(HubServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            Application.Quit();
                        }
                    }
                    else if (thisServer.type == 1)
                    {
                        if (!System.Array.Exists(LobbyServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            Application.Quit();
                        }
                    }
                    else
                    {
                        Debug.LogError("Not sure how to handle server type: " + thisServer.type);
                    }
                }
            }
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

                Debug.Log("Creating weapon item: " + (i + 1) + " of " + weaponOptions.Length + " " + weaponOption.weapon.name);

                CreateItemPayload payload = new CreateItemPayload(ItemClass.WEAPON, weaponOption.name, 1, 1, 1, 1, 1, 1, false, false, false, true,
                    weaponOption.weapon.name, weaponOption.weapon.name, weaponOption.weapon.name, weaponOption.weapon.name);

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

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions();

            for (int i = 0; i < wearableEquipmentOptions.Count; i++)
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = wearableEquipmentOptions[i];

                if (CharacterReference.equipmentTypesThatAreForCharacterCustomization.Contains(wearableEquipmentOption.equipmentType)) { continue; }
                if (itemList.Exists(item => item._id == wearableEquipmentOption.itemWebId)) { continue; }

                Debug.Log("Creating armor item: " + (i + 1) + " of " + wearableEquipmentOptions.Count + " " + wearableEquipmentOption.name);

                CreateItemPayload payload = new CreateItemPayload(ItemClass.ARMOR, wearableEquipmentOption.name, 1, 1, 1, 1, 1, 1, false, false, false, true,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanMale).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanFemale).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcMale).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcFemale).name);

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
            public ModelNames modelNames;
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
                this.modelNames = new ModelNames(humanMaleModelName, humanFemaleModelName, orcMaleModelName, orcFemaleModelName);
                this.isBasicGear = isBasicGear;
            }

            public struct ModelNames
            {
                public Human human;
                public Orc orc;

                public ModelNames(string humanMaleModelName, string humanFemaleModelName, string orcMaleModelName, string orcFemaleModelName)
                {
                    human = new Human()
                    {
                        m = humanMaleModelName,
                        f = humanFemaleModelName
                    };
                    orc = new Orc()
                    {
                        m = orcMaleModelName,
                        f = orcFemaleModelName
                    };
                }
            }

            public struct Human
            {
                public string m;
                public string f;
            }

            public struct Orc
            {
                public string m;
                public string f;
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
                Debug.LogError("Post request error in WebRequestManager.AddItemToCharacterInventory()" + postRequest.error);
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