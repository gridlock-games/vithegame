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

        public static bool IsServerBuild()
        {
            RuntimePlatform[] excludedRuntimePlatforms = new RuntimePlatform[] { RuntimePlatform.LinuxServer, RuntimePlatform.OSXServer, RuntimePlatform.WindowsServer };
            return excludedRuntimePlatforms.Contains(Application.platform);
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
                IsRefreshingServers = false;
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
                putRequest = UnityWebRequest.Put(APIURL + "servers/duels", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

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
                putRequest = UnityWebRequest.Put(APIURL + "servers/duels", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

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
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server post request from a server!"); yield break; }

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
                deleteRequest = UnityWebRequest.Delete(APIURL + "servers/duels");
                deleteRequest.method = UnityWebRequest.kHttpVerbDELETE;
                deleteRequest.SetRequestHeader("Content-Type", "application/json");
                deleteRequest.uploadHandler = new UploadHandlerRaw(jsonData);
                yield return deleteRequest.SendWebRequest();
            }

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

        public IEnumerator CreateAccount(string username, string email, string password)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            CreateAccountPayload payload = new CreateAccountPayload(username, email, password, true);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "auth/users/create", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "auth/users/create", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CreateAccount() " + postRequest.error);
            }
            else
            {
                CreateAccountResultPayload badResultPayload = JsonConvert.DeserializeObject<CreateAccountResultPayload>(postRequest.downloadHandler.text);

                if (badResultPayload.mes == null)
                {
                    CreateAccountSuccessPayload goodResultPayload = JsonConvert.DeserializeObject<CreateAccountSuccessPayload>(postRequest.downloadHandler.text);
                }
                else
                {
                    LogInErrorText = badResultPayload.mes;
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
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

        private struct CreateAccountPayload
        {
            public string username;
            public string email;
            public string password;
            public bool isPlayer;

            public CreateAccountPayload(string username, string email, string password, bool isPlayer)
            {
                this.username = username;
                this.email = email;
                this.password = password;
                this.isPlayer = isPlayer;
            }
        }

        private struct CreateAccountResultPayload
        {
            public string mes;
        }

        private struct CreateAccountSuccessPayload
        {
            public string username;
            public bool isPlayer;
            public List<string> dateCreated;
            public string id;
        }

        // TODO Change the string at the end to be the account ID of whoever we sign in under
        //private string currentlyLoggedInUserId = "652b4e237527296665a5059b";
        public bool IsLoggedIn { get; private set; }
        public bool IsLoggingIn { get; private set; }
        public string LogInErrorText { get; private set; }
        private string currentlyLoggedInUserId = "";

        public void ResetLogInErrorText() { LogInErrorText = ""; }

        public IEnumerator Login(string username, string password)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            LoginPayload payload = new LoginPayload(username, password);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "auth/users/login", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "auth/users/login", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login() " + postRequest.error);

                IsLoggedIn = false;
                currentlyLoggedInUserId = "";
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                currentlyLoggedInUserId = loginResultPayload.userId;

                if (!IsLoggedIn)
                {
                    LogInErrorText = "Invalid Username or Password";
                    if (postRequest.downloadHandler.text.Contains("isVerified"))
                    {
                        LogInErrorText = "Verify Your Email";
                    }
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
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

        public IEnumerator LoginWithFirebaseUserId(string email, string firebaseUserId)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            LoginWithFirebaseUserIdPayload payload = new LoginWithFirebaseUserIdPayload(email, firebaseUserId);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "auth/users/firebaseAuth", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "auth/users/firebaseAuth", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login() " + postRequest.error);

                IsLoggedIn = false;
                currentlyLoggedInUserId = "";
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                currentlyLoggedInUserId = loginResultPayload.userId;

                if (!IsLoggedIn)
                {
                    LogInErrorText = "Login Failed";
                    if (postRequest.downloadHandler.text.Contains("isVerified"))
                    {
                        LogInErrorText = "Verify Your Email";
                    }
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
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
            currentlyLoggedInUserId = "";
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

        private struct LoginWithFirebaseUserIdPayload
        {
            public string email;
            public string firebaseUserId;

            public LoginWithFirebaseUserIdPayload(string email, string firebaseUserId)
            {
                this.email = email;
                this.firebaseUserId = firebaseUserId;
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
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            getRequest.Dispose();

            foreach (Character character in Characters)
            {
                yield return GetCharacterInventory(character._id.ToString());
            }

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

            yield return GetCharacterInventory(CharacterById._id.ToString());

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
                putRequest = UnityWebRequest.Put(APIURL + "characters/" + "updateCharacterCosmetic", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterCosmetics()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public Dictionary<string, List<InventoryItem>> InventoryItems { get; private set; } = new Dictionary<string, List<InventoryItem>>();
        public IEnumerator GetCharacterInventory(string characterId)
        {
            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "characters/" + "getInventory/" + characterId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetCharacterInventory()");
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;

            if (!InventoryItems.ContainsKey(characterId))
                InventoryItems.Add(characterId, JsonConvert.DeserializeObject<List<InventoryItem>>(json));
            else
                InventoryItems[characterId] = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

            getRequest.Dispose();
        }

        public struct InventoryItem
        {
            public string charId;
            public List<int> loadoutSlot;
            public string itemId;
            public bool enabled;
            public string id;
        }

        private IEnumerator AddItemToInventory(string charId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) { Debug.LogWarning("You are trying to add an item to a character's inventory that has an id of null or whitespace"); yield break; }

            AddCharacterInventoryPayload payload = new AddCharacterInventoryPayload(charId, itemId);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/" + "setInventory", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "characters/" + "setInventory", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.AddItemToInventory()" + postRequest.error);
            }

            postRequest.Dispose();
        }

        private struct AddCharacterInventoryPayload
        {
            public string charId;
            public string itemId;

            public AddCharacterInventoryPayload(string charId, string itemId)
            {
                this.charId = charId;
                this.itemId = itemId;
            }
        }

        public IEnumerator UpdateCharacterLoadout(string characterId, Loadout newLoadout)
        {
            yield return GetCharacterInventory(characterId.ToString());

            newLoadout.helmGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.helmGearItemId | item.id == newLoadout.helmGearItemId).id ?? "";
            newLoadout.capeGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.capeGearItemId | item.id == newLoadout.capeGearItemId).id ?? "";
            newLoadout.pantsGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.pantsGearItemId | item.id == newLoadout.pantsGearItemId).id ?? "";
            newLoadout.shouldersGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.shouldersGearItemId | item.id == newLoadout.shouldersGearItemId).id ?? "";
            newLoadout.chestArmorGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.chestArmorGearItemId | item.id == newLoadout.chestArmorGearItemId).id ?? "";
            newLoadout.glovesGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.glovesGearItemId | item.id == newLoadout.glovesGearItemId).id ?? "";
            newLoadout.beltGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.beltGearItemId | item.id == newLoadout.beltGearItemId).id ?? "";
            newLoadout.robeGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.robeGearItemId | item.id == newLoadout.robeGearItemId).id ?? "";
            newLoadout.bootsGearItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.bootsGearItemId | item.id == newLoadout.bootsGearItemId).id ?? "";
            newLoadout.weapon1ItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.weapon1ItemId | item.id == newLoadout.weapon1ItemId).id ?? "";
            newLoadout.weapon2ItemId = InventoryItems[characterId].Find(item => item.itemId == newLoadout.weapon2ItemId | item.id == newLoadout.weapon2ItemId).id ?? "";

            CharacterLoadoutPutPayload payload = new CharacterLoadoutPutPayload(characterId, newLoadout);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "saveLoadOut", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(APIURL + "characters/" + "saveLoadOut", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterLoadout()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator UseCharacterLoadout(string characterId, string loadoutSlot)
        {
            UseCharacterLoadoutPayload payload = new UseCharacterLoadoutPayload(characterId, loadoutSlot);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(APIURL + "characters/" + "useLoadOut", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(APIURL + "characters/" + "useLoadOut", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UseCharacterLoadout()" + putRequest.error);
            }

            putRequest.Dispose();
        }

        private struct UseCharacterLoadoutPayload
        {
            public string charId;
            public string loadoutSlot;

            public UseCharacterLoadoutPayload(string charId, string loadoutSlot)
            {
                this.charId = charId;
                this.loadoutSlot = loadoutSlot;
            }
        }

        public IEnumerator CharacterPostRequest(Character character)
        {
            CharacterPostPayload payload = new CharacterPostPayload(character);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
            }

            yield return GetCharacterInventory(postRequest.downloadHandler.text);

            Loadout loadout1 = GetRandomizedLoadout(character.raceAndGender);
            Loadout loadout2 = GetRandomizedLoadout(character.raceAndGender);
            Loadout loadout3 = GetRandomizedLoadout(character.raceAndGender);
            Loadout loadout4 = GetRandomizedLoadout(character.raceAndGender);

            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(character.raceAndGender);
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            
            // Add all items into character inventory
            foreach (var option in armorOptions)
            {
                if (!InventoryItems[postRequest.downloadHandler.text].Exists(item => item.itemId == option.itemWebId))
                {
                    Debug.LogWarning("Item not in inventory but you're putting it in a loadout");
                    yield return AddItemToInventory(postRequest.downloadHandler.text, option.itemWebId);
                }
            }

            foreach (var option in weaponOptions)
            {
                if (!InventoryItems[postRequest.downloadHandler.text].Exists(item => item.itemId == option.itemWebId))
                {
                    Debug.LogWarning("Item not in inventory but you're putting it in a loadout");
                    yield return AddItemToInventory(postRequest.downloadHandler.text, option.itemWebId);
                }
            }

            yield return GetCharacterInventory(postRequest.downloadHandler.text);

            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, loadout1);
            loadout2.loadoutSlot = "2";
            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, loadout2);
            loadout3.loadoutSlot = "3";
            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, loadout3);
            loadout4.loadoutSlot = "4";
            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, loadout4);

            yield return UseCharacterLoadout(postRequest.downloadHandler.text, "1");

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
                putRequest = UnityWebRequest.Put(APIURL + "characters/" + "disableCharacter", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.CharacterDisableRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public Character GetDefaultCharacter()
        {
            return new Character("", "Human_Male", "", 0, 1,
                GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                CharacterReference.RaceAndGender.HumanMale);
        }

        public Character GetRandomizedCharacter()
        {
            List<CharacterReference.RaceAndGender> raceAndGenderList = new List<CharacterReference.RaceAndGender>()
            {
                CharacterReference.RaceAndGender.HumanMale,
                CharacterReference.RaceAndGender.HumanFemale
            };

            CharacterReference.RaceAndGender raceAndGender = raceAndGenderList[Random.Range(0, raceAndGenderList.Count)];

            string[] raceAndGenderStrings = Regex.Matches(raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            string race = raceAndGenderStrings[0];
            string gender = raceAndGenderStrings[1];

            return new Character("", race + "_" + gender, "", 0, 1,
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                raceAndGender);
        }

        public Loadout GetDefaultDisplayLoadout(CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            var beltOption = armorOptions.Find(item => item.name == "Runic Belt");
            var capeOption = armorOptions.Find(item => item.name == "Runic Cape");
            return new Loadout("1",
                "",
                armorOptions.Find(item => item.name == "Runic Chest").itemWebId,
                armorOptions.Find(item => item.name == "Runic Shoulders").itemWebId,
                armorOptions.Find(item => item.name == "Runic Boots").itemWebId,
                armorOptions.Find(item => item.name == "Runic Pants").itemWebId,
                beltOption == null ? "" : beltOption.itemWebId,
                armorOptions.Find(item => item.name == "Runic Gloves").itemWebId,
                capeOption == null ? "" : capeOption.itemWebId,
                "",
                System.Array.Find(weaponOptions, item => item.weapon.name == "GreatSwordWeapon").itemWebId,
                System.Array.Find(weaponOptions, item => item.weapon.name == "CrossbowWeapon").itemWebId,
                true);
        }

        public static readonly List<CharacterReference.EquipmentType> NullableEquipmentTypes = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Belt,
            CharacterReference.EquipmentType.Cape,
            CharacterReference.EquipmentType.Gloves,
            CharacterReference.EquipmentType.Helm,
            CharacterReference.EquipmentType.Shoulders,
        };

        public Loadout GetRandomizedLoadout(CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            var helmOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Helm);
            var chestOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Chest);
            var shoulderOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Shoulders);
            var bootsOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Boots);
            var pantsOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Pants);
            var beltOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Belt);
            var gloveOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Gloves);
            var capeOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Cape);
            var robeOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Robe);

            int helmIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Helm) ? -1 : 0, helmOptions.Count);
            int chestIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Chest) ? -1 : 0, chestOptions.Count);
            int shoulderIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Shoulders) ? -1 : 0, shoulderOptions.Count);
            int bootsIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Boots) ? -1 : 0, bootsOptions.Count);
            int pantsIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Pants) ? -1 : 0, pantsOptions.Count);
            int beltIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Belt) ? -1 : 0, beltOptions.Count);
            int gloveIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Gloves) ? -1 : 0, gloveOptions.Count);
            int capeIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Cape) ? -1 : 0, capeOptions.Count);
            int robeIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Robe) ? -1 : 0, robeOptions.Count);

            int weapon1Index = Random.Range(0, weaponOptions.Length);
            int weapon2Index = Random.Range(0, weaponOptions.Length);

            if (weapon1Index == weapon2Index)
            {
                weapon2Index++;
                // If weapon 2 index is out of the weapon options range, set it to 0
                if (weapon2Index >= weaponOptions.Length) { weapon2Index = 0; }
            }

            return new Loadout("1",
                helmOptions.Count == 0 ? "" : (helmIndex == -1 ? "" : helmOptions[helmIndex].itemWebId),
                chestOptions.Count == 0 ? "" : (chestIndex == -1 ? "" : chestOptions[chestIndex].itemWebId),
                shoulderOptions.Count == 0 ? "" : (shoulderIndex == -1 ? "" : shoulderOptions[shoulderIndex].itemWebId),
                bootsOptions.Count == 0 ? "" : (bootsIndex == -1 ? "" : bootsOptions[bootsIndex].itemWebId),
                pantsOptions.Count == 0 ? "" : (pantsIndex == -1 ? "" : pantsOptions[pantsIndex].itemWebId),
                beltOptions.Count == 0 ? "" : (beltIndex == -1 ? "" : beltOptions[beltIndex].itemWebId),
                gloveOptions.Count == 0 ? "" : (gloveIndex == -1 ? "" : gloveOptions[gloveIndex].itemWebId),
                capeOptions.Count == 0 ? "" : (capeIndex == -1 ? "" : capeOptions[capeIndex].itemWebId),
                robeOptions.Count == 0 ? "" : (robeIndex == -1 ? "" : robeOptions[robeIndex].itemWebId),
                weaponOptions[weapon1Index].itemWebId,
                weaponOptions[weapon2Index].itemWebId,
                true);
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
                loadOuts = new List<LoadoutJson>(),
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
            public NetworkString64Bytes _id;
            public NetworkString64Bytes name;
            public NetworkString64Bytes model;
            public NetworkString64Bytes bodyColor;
            public NetworkString64Bytes eyeColor;
            public NetworkString64Bytes beard;
            public NetworkString64Bytes brows;
            public NetworkString64Bytes hair;
            public CharacterAttributes attributes;
            public Loadout loadoutPreset1;
            public Loadout loadoutPreset2;
            public Loadout loadoutPreset3;
            public Loadout loadoutPreset4;
            public NetworkString64Bytes userId;
            public int slot;
            public int level;
            public int experience;
            public CharacterReference.RaceAndGender raceAndGender;

            public Character(string _id, string model, string name, int experience, int level, Loadout loadoutPreset1, Loadout loadoutPreset2, Loadout loadoutPreset3, Loadout loadoutPreset4, CharacterReference.RaceAndGender raceAndGender)
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
                this.loadoutPreset2 = loadoutPreset2;
                this.loadoutPreset3 = loadoutPreset3;
                this.loadoutPreset4 = loadoutPreset4;
                this.raceAndGender = raceAndGender;
            }

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level, Loadout loadoutPreset1, Loadout loadoutPreset2, Loadout loadoutPreset3, Loadout loadoutPreset4, CharacterReference.RaceAndGender raceAndGender)
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
                this.loadoutPreset2 = loadoutPreset2;
                this.loadoutPreset3 = loadoutPreset3;
                this.loadoutPreset4 = loadoutPreset4;
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
                serializer.SerializeNetworkSerializable(ref loadoutPreset2);
                serializer.SerializeNetworkSerializable(ref loadoutPreset3);
                serializer.SerializeNetworkSerializable(ref loadoutPreset4);
                serializer.SerializeValue(ref userId);
                serializer.SerializeValue(ref slot);
                serializer.SerializeValue(ref level);
                serializer.SerializeValue(ref experience);
                serializer.SerializeValue(ref raceAndGender);
            }

            public Loadout GetLoadoutFromSlot(int loadoutSlot)
            {
                switch (loadoutSlot)
                {
                    case 0:
                        return loadoutPreset1;
                    case 1:
                        return loadoutPreset2;
                    case 2:
                        return loadoutPreset3;
                    case 3:
                        return loadoutPreset4;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        return Singleton.GetRandomizedLoadout(raceAndGender);
                }
            }

            public bool IsSlotActive(int loadoutSlot)
            {
                switch (loadoutSlot)
                {
                    case 0:
                        return loadoutPreset1.active;
                    case 1:
                        return loadoutPreset2.active;
                    case 2:
                        return loadoutPreset3.active;
                    case 3:
                        return loadoutPreset4.active;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        return false;
                }
            }

            public Loadout GetActiveLoadout()
            {
                if (loadoutPreset1.active) { return loadoutPreset1; }
                if (loadoutPreset2.active) { return loadoutPreset2; }
                if (loadoutPreset3.active) { return loadoutPreset3; }
                if (loadoutPreset4.active) { return loadoutPreset4; }
                return loadoutPreset1;
            }

            public Character ChangeLoadoutFromSlot(int loadoutSlot, Loadout newLoadout)
            {
                Character copy = this;
                switch (loadoutSlot)
                {
                    case 0:
                        copy.loadoutPreset1 = newLoadout;
                        break;
                    case 1:
                        copy.loadoutPreset2 = newLoadout;
                        break;
                    case 2:
                        copy.loadoutPreset3 = newLoadout;
                        break;
                    case 3:
                        copy.loadoutPreset4 = newLoadout;
                        break;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        break;
                }
                return copy;
            }

            public Character ChangeActiveLoadoutFromSlot(int loadoutSlot)
            {
                Character copy = this;

                copy.loadoutPreset1 = new Loadout(loadoutPreset1.loadoutSlot, loadoutPreset1.helmGearItemId,
                    loadoutPreset1.chestArmorGearItemId, loadoutPreset1.shouldersGearItemId, loadoutPreset1.bootsGearItemId,
                    loadoutPreset1.pantsGearItemId, loadoutPreset1.beltGearItemId, loadoutPreset1.glovesGearItemId,
                    loadoutPreset1.capeGearItemId, loadoutPreset1.robeGearItemId, loadoutPreset1.weapon1ItemId,
                    loadoutPreset1.weapon2ItemId, loadoutSlot == 0);

                copy.loadoutPreset2 = new Loadout(loadoutPreset2.loadoutSlot, loadoutPreset2.helmGearItemId,
                    loadoutPreset2.chestArmorGearItemId, loadoutPreset2.shouldersGearItemId, loadoutPreset2.bootsGearItemId,
                    loadoutPreset2.pantsGearItemId, loadoutPreset2.beltGearItemId, loadoutPreset2.glovesGearItemId,
                    loadoutPreset2.capeGearItemId, loadoutPreset2.robeGearItemId, loadoutPreset2.weapon1ItemId,
                    loadoutPreset2.weapon2ItemId, loadoutSlot == 1);

                copy.loadoutPreset3 = new Loadout(loadoutPreset3.loadoutSlot, loadoutPreset3.helmGearItemId,
                    loadoutPreset3.chestArmorGearItemId, loadoutPreset3.shouldersGearItemId, loadoutPreset3.bootsGearItemId,
                    loadoutPreset3.pantsGearItemId, loadoutPreset3.beltGearItemId, loadoutPreset3.glovesGearItemId,
                    loadoutPreset3.capeGearItemId, loadoutPreset3.robeGearItemId, loadoutPreset3.weapon1ItemId,
                    loadoutPreset3.weapon2ItemId, loadoutSlot == 2);

                copy.loadoutPreset4 = new Loadout(loadoutPreset4.loadoutSlot, loadoutPreset4.helmGearItemId,
                    loadoutPreset4.chestArmorGearItemId, loadoutPreset4.shouldersGearItemId, loadoutPreset4.bootsGearItemId,
                    loadoutPreset4.pantsGearItemId, loadoutPreset4.beltGearItemId, loadoutPreset4.glovesGearItemId,
                    loadoutPreset4.capeGearItemId, loadoutPreset4.robeGearItemId, loadoutPreset4.weapon1ItemId,
                    loadoutPreset4.weapon2ItemId, loadoutSlot == 3);

                return copy;
            }
        }

        public struct Loadout : INetworkSerializable
        {
            public NetworkString64Bytes loadoutSlot;
            public NetworkString64Bytes helmGearItemId;
            public NetworkString64Bytes chestArmorGearItemId;
            public NetworkString64Bytes shouldersGearItemId;
            public NetworkString64Bytes bootsGearItemId;
            public NetworkString64Bytes pantsGearItemId;
            public NetworkString64Bytes beltGearItemId;
            public NetworkString64Bytes glovesGearItemId;
            public NetworkString64Bytes capeGearItemId;
            public NetworkString64Bytes robeGearItemId;
            public NetworkString64Bytes weapon1ItemId;
            public NetworkString64Bytes weapon2ItemId;
            public bool active;

            public Loadout(NetworkString64Bytes loadoutSlot, NetworkString64Bytes helmGearItemId, NetworkString64Bytes chestArmorGearItemId, NetworkString64Bytes shouldersGearItemId, NetworkString64Bytes bootsGearItemId, NetworkString64Bytes pantsGearItemId, NetworkString64Bytes beltGearItemId, NetworkString64Bytes glovesGearItemId, NetworkString64Bytes capeGearItemId, NetworkString64Bytes robeGearItemId, NetworkString64Bytes weapon1ItemId, NetworkString64Bytes weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.helmGearItemId = helmGearItemId;
                this.chestArmorGearItemId = chestArmorGearItemId;
                this.shouldersGearItemId = shouldersGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.pantsGearItemId = pantsGearItemId;
                this.beltGearItemId = beltGearItemId;
                this.glovesGearItemId = glovesGearItemId;
                this.capeGearItemId = capeGearItemId;
                this.robeGearItemId = robeGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public List<NetworkString64Bytes> GetLoadoutAsList()
            {
                return new List<NetworkString64Bytes>()
                {
                    helmGearItemId,
                    chestArmorGearItemId,
                    shouldersGearItemId,
                    bootsGearItemId,
                    pantsGearItemId,
                    beltGearItemId,
                    glovesGearItemId,
                    capeGearItemId,
                    robeGearItemId,
                    weapon1ItemId,
                    weapon2ItemId
                };
            }

            public Dictionary<CharacterReference.EquipmentType, NetworkString64Bytes> GetLoadoutArmorPiecesAsDictionary()
            {
                return new Dictionary<CharacterReference.EquipmentType, NetworkString64Bytes>()
                {
                    { CharacterReference.EquipmentType.Helm, helmGearItemId },
                    { CharacterReference.EquipmentType.Chest, chestArmorGearItemId },
                    { CharacterReference.EquipmentType.Shoulders, shouldersGearItemId },
                    { CharacterReference.EquipmentType.Boots, bootsGearItemId },
                    { CharacterReference.EquipmentType.Pants, pantsGearItemId },
                    { CharacterReference.EquipmentType.Belt, beltGearItemId },
                    { CharacterReference.EquipmentType.Gloves, glovesGearItemId },
                    { CharacterReference.EquipmentType.Cape, capeGearItemId },
                    { CharacterReference.EquipmentType.Robe, robeGearItemId }
                };
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref loadoutSlot);
                serializer.SerializeValue(ref helmGearItemId);
                serializer.SerializeValue(ref chestArmorGearItemId);
                serializer.SerializeValue(ref shouldersGearItemId);
                serializer.SerializeValue(ref bootsGearItemId);
                serializer.SerializeValue(ref pantsGearItemId);
                serializer.SerializeValue(ref beltGearItemId);
                serializer.SerializeValue(ref glovesGearItemId);
                serializer.SerializeValue(ref capeGearItemId);
                serializer.SerializeValue(ref robeGearItemId);
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
            public List<LoadoutJson> loadOuts;
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
                int loadout1Index = loadOuts.FindIndex(item => item.loadoutSlot == "1");
                int loadout2Index = loadOuts.FindIndex(item => item.loadoutSlot == "2");
                int loadout3Index = loadOuts.FindIndex(item => item.loadoutSlot == "3");
                int loadout4Index = loadOuts.FindIndex(item => item.loadoutSlot == "4");

                return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level,
                    loadout1Index == -1 ? Singleton.GetRandomizedLoadout(raceAndGender) : loadOuts[loadout1Index].ToLoadout(),
                    loadout2Index == -1 ? Singleton.GetRandomizedLoadout(raceAndGender) : loadOuts[loadout2Index].ToLoadout(),
                    loadout3Index == -1 ? Singleton.GetRandomizedLoadout(raceAndGender) : loadOuts[loadout3Index].ToLoadout(),
                    loadout4Index == -1 ? Singleton.GetRandomizedLoadout(raceAndGender) : loadOuts[loadout4Index].ToLoadout(),
                    raceAndGender);
            }
        }

        private struct LoadoutJson
        {
            public string loadoutSlot;
            public string helmGearItemId;
            public string chestArmorGearItemId;
            public string shouldersGearItemId;
            public string bootsGearItemId;
            public string pantsGearItemId;
            public string beltGearItemId;
            public string glovesGearItemId;
            public string capeGearItemId;
            public string robeGearItemId;
            public string weapon1ItemId;
            public string weapon2ItemId;
            public bool active;

            public LoadoutJson(string loadoutSlot, string helmGearItemId, string chestArmorGearItemId, string shouldersGearItemId, string bootsGearItemId, string pantsGearItemId, string beltGearItemId, string glovesGearItemId, string capeGearItemId, string robeGearItemId, string weapon1ItemId, string weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.helmGearItemId = helmGearItemId;
                this.chestArmorGearItemId = chestArmorGearItemId;
                this.shouldersGearItemId = shouldersGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.pantsGearItemId = pantsGearItemId;
                this.beltGearItemId = beltGearItemId;
                this.glovesGearItemId = glovesGearItemId;
                this.capeGearItemId = capeGearItemId;
                this.robeGearItemId = robeGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public Loadout ToLoadout()
            {
                return new Loadout(loadoutSlot, helmGearItemId ?? "", chestArmorGearItemId ?? "", shouldersGearItemId ?? "", bootsGearItemId ?? "",
                    pantsGearItemId ?? "", beltGearItemId ?? "", glovesGearItemId ?? "", capeGearItemId ?? "", robeGearItemId ?? "", weapon1ItemId ?? "",
                    weapon2ItemId ?? "", active);
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
            public NestedCharacterLoadoutPutPayload loadout;

            public CharacterLoadoutPutPayload(string charId, Loadout loadout)
            {
                this.charId = charId;
                this.loadout = new NestedCharacterLoadoutPutPayload()
                {
                    loadoutSlot = EvaluateFixedString(loadout.loadoutSlot),
                    helmGearItemId = EvaluateFixedString(loadout.helmGearItemId),
                    chestArmorGearItemId = EvaluateFixedString(loadout.chestArmorGearItemId),
                    shouldersGearItemId = EvaluateFixedString(loadout.shouldersGearItemId),
                    bootsGearItemId = EvaluateFixedString(loadout.bootsGearItemId),
                    pantsGearItemId = EvaluateFixedString(loadout.pantsGearItemId),
                    beltGearItemId = EvaluateFixedString(loadout.beltGearItemId),
                    glovesGearItemId = EvaluateFixedString(loadout.glovesGearItemId),
                    capeGearItemId = EvaluateFixedString(loadout.capeGearItemId),
                    robeGearItemId = EvaluateFixedString(loadout.robeGearItemId),
                    weapon1ItemId = EvaluateFixedString(loadout.weapon1ItemId),
                    weapon2ItemId = EvaluateFixedString(loadout.weapon2ItemId)
                };
            }

            private static string EvaluateFixedString(NetworkString64Bytes input)
            {
                if (input == "")
                {
                    return null;
                }
                else
                {
                    return input.ToString();
                }
            }

            public struct NestedCharacterLoadoutPutPayload
            {
                public string loadoutSlot;
                public string helmGearItemId;
                public string chestArmorGearItemId;
                public string shouldersGearItemId;
                public string bootsGearItemId;
                public string pantsGearItemId;
                public string beltGearItemId;
                public string glovesGearItemId;
                public string capeGearItemId;
                public string robeGearItemId;
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
            CheckGameVersion();
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
                    postRequest = new UnityWebRequest(APIURL + "items/createItem", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                    postRequest.SetRequestHeader("Content-Type", "application/json");
                    yield return postRequest.SendWebRequest();
                }

                if (postRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Post request error in WebRequestManager.CreateItems()" + postRequest.error);
                }

                weaponOption.itemWebId = postRequest.downloadHandler.text;

                postRequest.Dispose();
            }

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetAllArmorEquipmentOptions();

            for (int i = 0; i < wearableEquipmentOptions.Count; i++)
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = wearableEquipmentOptions[i];

                if (CharacterReference.equipmentTypesThatAreForCharacterCustomization.Contains(wearableEquipmentOption.equipmentType)) { continue; }
                if (itemList.Exists(item => item._id == wearableEquipmentOption.itemWebId)) { continue; }

                Debug.Log("Creating armor item: " + (i + 1) + " of " + wearableEquipmentOptions.Count + " " + wearableEquipmentOption.name);

                CreateItemPayload payload = new CreateItemPayload(ItemClass.ARMOR, wearableEquipmentOption.name, 1, 1, 1, 1, 1, 1, false, false, false, true,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanMale, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.HumanFemale, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcMale, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).name,
                    wearableEquipmentOption.GetModel(CharacterReference.RaceAndGender.OrcFemale, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).name);

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
                postRequest = new UnityWebRequest(APIURL + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

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

        public void CheckGameVersion()
        {
            if (checkingGameVersion) { return; }
            StartCoroutine(CheckGameVersionRequest());
        }

        public bool GameIsUpToDate { get; private set; }

        public string GetGameVersion() { return gameVersion.Version; }

        [SerializeField] private GameObject alertBoxPrefab;
        private GameVersion gameVersion;
        private bool checkingGameVersion;
        private IEnumerator CheckGameVersionRequest()
        {
            checkingGameVersion = true;

            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/version");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.VersionGetRequest() " + getRequest.error + APIURL + "servers/duels");
                getRequest.Dispose();
                yield break;
            }

            Version version = JsonConvert.DeserializeObject<Version>(getRequest.downloadHandler.text);
            gameVersion = version.gameversion;

            getRequest.Dispose();

            GameIsUpToDate = Application.version == gameVersion.Version;
            if (!GameIsUpToDate) { Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Game is out of date, please update."; }

            checkingGameVersion = false;
        }

        private class GameVersion
        {
            public string Version;
            public string Type;
        }

        private class Version
        {
            public GameVersion gameversion;
        }
    }
}