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

            if (FasterPlayerPrefs.Singleton.HasString("APIURL"))
            {
                SetAPIURL(FasterPlayerPrefs.Singleton.GetString("APIURL"));
            }
        }

        public static bool IsServerBuild()
        {
            RuntimePlatform[] includedRuntimePlatforms = new RuntimePlatform[] { RuntimePlatform.LinuxServer, RuntimePlatform.OSXServer, RuntimePlatform.WindowsServer };
            return includedRuntimePlatforms.Contains(Application.platform);
        }

        public const string ProdAPIURL = "http://38.60.246.146:80/";
        public const string DevAPIURL = "http://154.90.36.42:80/";

        private string APIURL = ProdAPIURL;

        public string GetAPIURL() { return APIURL[0..^1]; }

        public void SetAPIURL(string newAPIURL)
        {
            APIURL = newAPIURL + "/";
            FasterPlayerPrefs.Singleton.SetString("APIURL", newAPIURL);

            CheckGameVersion(true);
            Logout();
        }

        public string PublicIP { get; private set; }
        public IEnumerator GetPublicIP()
        {
            UnityWebRequest getRequest = UnityWebRequest.Get("http://icanhazip.com");
            yield return getRequest.SendWebRequest();

            servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetPublicIP() " + getRequest.error);
                getRequest.Dispose();
                IsRefreshingServers = false;
                yield break;
            }
            PublicIP = getRequest.downloadHandler.text.Replace("\\r\\n", "").Replace("\\n", "").Trim();
        }

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
                servers = new List<Server>() { new Server("1", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777", ""), new Server("2", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776", "") };
            }

            if (NetworkManager.Singleton.IsServer)
            {
                HubServers = servers.FindAll(item => item.type == 0).ToArray();
                LobbyServers = servers.FindAll(item => item.type == 1).ToArray();
            }
            else // Not the server
            {
                if (FasterPlayerPrefs.Singleton.GetBool("AllowLocalhostServers"))
                {
                    if (servers.Count(item => item.ip == "127.0.0.1") > 0)
                    {
                        servers.RemoveAll(item => item.ip != "127.0.0.1");
                    }
                }
                else
                {
                    servers.RemoveAll(item => item.ip == "127.0.0.1");
                }

                if (FasterPlayerPrefs.Singleton.GetBool("AllowLANServers"))
                {
                    if (servers.Count(item => item.ip.StartsWith("192.168.")) > 0)
                    {
                        servers.RemoveAll(item => !item.ip.StartsWith("192.168."));
                    }
                }
                else
                {
                    servers.RemoveAll(item => item.ip.StartsWith("192.168."));
                }

                HubServers = servers.FindAll(item => item.type == 0).ToArray();
                LobbyServers = servers.FindAll(item => item.type == 1).ToArray();

                if (HubServers.Length == 0)
                {
                    HubServers = LobbyServers.ToArray();
                }
            }

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
                Debug.LogError("Put request error in WebRequestManager.UpdateServerProgress()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator UpdateServerPopulation(int population, string label, string hostCharId)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }
            if (!thisServerCreated) { yield break; }

            ServerPopulationPayload payload = new ServerPopulationPayload(thisServer._id, population, thisServer.type == 0 ? "Hub" : label == "" ? "Lobby" : label, hostCharId);

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
                Debug.LogError("Put request error in WebRequestManager.UpdateServerPopulation()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        private Server thisServer;
        private bool thisServerCreated;
        public IEnumerator ServerPostRequest(ServerPostPayload payload)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server post request from a server!"); yield break; }

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
            form.AddField("hostCharId", payload.hostCharId);

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
            public string hostCharId;

            public Server(string _id, int type, int population, int progress, string ip, string label, string __v, string port, string hostCharId)
            {
                this._id = _id;
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.__v = __v;
                this.port = port;
                this.hostCharId = hostCharId;
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
            public string hostCharId;

            public ServerPostPayload(int type, int population, int progress, string ip, string label, string port, string hostCharId)
            {
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.port = port;
                this.hostCharId = hostCharId;
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
            public string hostCharId;

            public ServerPopulationPayload(string serverId, int population, string label, string hostCharId)
            {
                this.serverId = serverId;
                this.population = population;
                this.label = label;
                this.hostCharId = hostCharId;
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

            if (IsLoggedIn)
            {
                RefreshCharacters();
            }
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
                    LogInErrorText = "Login Failed. This is probably a bug on our end.";
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

            if (IsLoggedIn)
            {
                RefreshCharacters();
            }
        }

        public void Logout()
        {
            IsLoggedIn = false;
            currentlyLoggedInUserId = "";
            LogInErrorText = default;

            if (characterGetRequestRoutine != null)
            {
                StopCoroutine(characterGetRequestRoutine);
            }
            IsRefreshingCharacters = false;
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
        private Coroutine characterGetRequestRoutine;
        public void RefreshCharacters() { characterGetRequestRoutine = StartCoroutine(CharacterGetRequest()); }
        private IEnumerator CharacterGetRequest()
        {
            if (IsRefreshingCharacters) { yield break; }
            IsRefreshingCharacters = true;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Characters.Clear();
                yield return new WaitUntil(() => PlayerDataManager.DoesExist());
                yield return new WaitUntil(() => PlayerDataManager.IsCharacterReferenceLoaded());
                for (int i = 0; i < 5; i++)
                {
                    Character character = GetRandomizedCharacter(false);
                    character._id = i.ToString();
                    character.name = PlayerDataManager.Singleton.GetRandomPlayerName(character.raceAndGender);
                    Characters.Add(character);
                }
            }
            else
            {
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
                    try
                    {
                        foreach (CharacterJson jsonStruct in CharacterJson.DeserializeJsonList(json))
                        {
                            Characters.Add(jsonStruct.ToCharacter());
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e);
                        foreach (CharacterJson jsonStruct in JsonConvert.DeserializeObject<List<CharacterJson>>(json))
                        {
                            Characters.Add(jsonStruct.ToCharacter());
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }

                getRequest.Dispose();
            }

            foreach (Character character in Characters)
            {
                yield return GetCharacterInventory(character);
            }

            // This adds all weapons to the inventory if we're in the editor
//# if UNITY_EDITOR
//            var weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
//            foreach (Character character in Characters)
//            {
//                foreach (var weaponOption in weaponOptions)
//                {
//                    if (!inventoryItems[character._id.ToString()].Exists(item => item.itemId == weaponOption.itemWebId))
//                    {
//                        yield return AddItemToInventory(character._id.ToString(), weaponOption.itemWebId);
//                    }
//                }
//            }

//            foreach (Character character in Characters)
//            {
//                yield return GetCharacterInventory(character);
//            }
//# endif

            IsRefreshingCharacters = false;
        }

        public bool IsGettingCharacterById { get; private set; }
        public bool LastCharacterByIdWasSuccessful { get; private set; }
        public Character CharacterById { get; private set; }
        public void GetCharacterById(string characterId) { StartCoroutine(CharacterByIdGetRequest(characterId)); }

        private IEnumerator CharacterByIdGetRequest(string characterId)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                CharacterById = Characters.Find(item => item._id == characterId);
                LastCharacterByIdWasSuccessful = true;
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
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                string json = getRequest.downloadHandler.text;

                if (json == "[]")
                {
                    getRequest.Dispose();
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                try
                {
                    CharacterJson characterJson = JsonConvert.DeserializeObject<CharacterJson>(json);

                    if (!characterJson.enabled)
                    {
                        getRequest.Dispose();
                        LastCharacterByIdWasSuccessful = false;
                        IsGettingCharacterById = false;
                        yield break;
                    }

                    CharacterById = characterJson.ToCharacter();

                    CharacterJson.DeserializeJson(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Get Character by ID Exception " + e);
                    CharacterById = GetDefaultCharacter(CharacterReference.RaceAndGender.HumanMale);

                    getRequest.Dispose();
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                getRequest.Dispose();

                int index = Characters.FindIndex(item => item._id == CharacterById._id);
                if (index == -1)
                {
                    Characters.Add(CharacterById);
                }
                else
                {
                    Characters[index] = CharacterById;
                }

                yield return GetCharacterInventory(CharacterById._id.ToString());

                LastCharacterByIdWasSuccessful = true;
                IsGettingCharacterById = false;
            }
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

        //public List<InventoryItem> GetInventoryItems(string characterId)
        //{
        //    if (characterId == null) { characterId = ""; }

        //    if (InventoryItems.TryGetValue(characterId, out List<InventoryItem> result))
        //    {
        //        return result;
        //    }
        //    return new List<InventoryItem>();
        //}

        public static bool IsItemInInventory(string characterId, string itemWebId)
        {
            if (characterId == null) { characterId = ""; }
            if (itemWebId == null) { itemWebId = ""; }

            if (HasCharacterInventory(characterId))
            {
                List<InventoryItem> inventoryItems = GetInventory(characterId);
                return inventoryItems.Exists(item => item.itemId._id == itemWebId);
            }

            return false;
        }

        public static CharacterReference.WeaponOption GetWeaponOption(InventoryItem inventoryItem)
        {
            CharacterReference.WeaponOption[] options = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            int index = System.Array.FindIndex(options, item => item.itemWebId == inventoryItem.itemId._id);

            if (index == -1) { return null; }

            return options[index];
        }

        public static CharacterReference.WearableEquipmentOption GetEquipmentOption(InventoryItem inventoryItem, CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> options = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            int index = options.FindIndex(item => item.itemWebId == inventoryItem.itemId._id);

            if (index == -1) { return null; }

            return options[index];
        }

        public static bool TryGetInventoryItem(string characterId, string inventoryItemId, out InventoryItem resultingInventoryItem)
        {
            if (characterId == null) { characterId = ""; }
            if (inventoryItemId == null) { inventoryItemId = ""; }

            if (inventoryItems.TryGetValue(characterId, out List<InventoryItem> items))
            {
                int index = items.FindIndex(item => item.id == inventoryItemId);
                if (index != -1)
                {
                    resultingInventoryItem = items[index];
                    return true;
                }
            }

            resultingInventoryItem = InventoryItem.GetEmptyInventoryItem();
            return false;
        }

        public static bool HasCharacterInventory(string characterId)
        {
            if (characterId == null) { characterId = ""; }
            return inventoryItems.ContainsKey(characterId);
        }

        public static bool HasInventoryItem(string characterId, string inventoryItemId)
        {
            if (characterId == null) { characterId = ""; }
            if (inventoryItemId == null) { inventoryItemId = ""; }

            if (inventoryItems.TryGetValue(characterId, out List<InventoryItem> items))
            {
                int index = items.FindIndex(item => item.id == inventoryItemId);
                if (index != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<InventoryItem> GetInventory(string characterId)
        {
            if (characterId == null) { characterId = ""; }

            if (HasCharacterInventory(characterId))
            {
                return inventoryItems[characterId];
            }
            return new List<InventoryItem>();
        }

        private static Dictionary<string, List<InventoryItem>> inventoryItems = new Dictionary<string, List<InventoryItem>>();
        public IEnumerator GetCharacterInventory(Character character)
        {
            string characterId = character._id.ToString();
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(character.raceAndGender);
                CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

                List<InventoryItem> allItems = new List<InventoryItem>();
                foreach (var option in armorOptions)
                {
                    allItems.Add(new InventoryItem(characterId, new List<int>(), option.itemWebId, true, option.itemWebId));
                }

                foreach (var option in weaponOptions)
                {
                    allItems.Add(new InventoryItem(characterId, new List<int>(), option.itemWebId, true, option.itemWebId));
                }

                if (!inventoryItems.ContainsKey(characterId))
                    inventoryItems.Add(characterId, allItems);
                else
                    inventoryItems[characterId] = allItems;
            }
            else
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

                if (!inventoryItems.ContainsKey(characterId))
                    inventoryItems.Add(characterId, JsonConvert.DeserializeObject<List<InventoryItem>>(json));
                else
                    inventoryItems[characterId] = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

                getRequest.Dispose();
            }
        }

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

            if (!inventoryItems.ContainsKey(characterId))
                inventoryItems.Add(characterId, JsonConvert.DeserializeObject<List<InventoryItem>>(json));
            else
                inventoryItems[characterId] = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

            getRequest.Dispose();
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

        public struct ItemAttributes
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;
            public int? attack;
            public int? defense;
        }

        public struct ItemId
        {
            public string _id;
            public string @class;
            public string name;
            public string equipmentType;
            public int weight;
            public ItemAttributes attributes;
            public bool isCraftOnly;
            public bool isCashExclusive;
            public bool isPassExclusive;
            public bool isBasicGear;
            public ModelNames modelNames;
            public int __v;
            public string id;

            public ItemId(string itemId)
            {
                _id = itemId;
                @class = "";
                name = "";
                equipmentType = "";
                weight = 0;
                attributes = new ItemAttributes();
                isCraftOnly = false;
                isCashExclusive = false;
                isPassExclusive = false;
                isBasicGear = false;
                modelNames = new ModelNames();
                __v = 0;
                id = itemId;
            }
        }

        public struct InventoryItem
        {
            public string charId;
            public List<int> loadoutSlot;
            public ItemId itemId;
            public bool enabled;
            public string id;

            public InventoryItem(string charId, List<int> loadoutSlot, string itemId, bool enabled, string id)
            {
                this.charId = charId;
                this.loadoutSlot = loadoutSlot;
                this.itemId = new ItemId(itemId);
                this.enabled = enabled;
                this.id = id;
            }

            public static InventoryItem GetEmptyInventoryItem()
            {
                return new InventoryItem("", null, "", false, "");
            }
        }

        public bool InventoryAddWasSuccessful { get; private set; }
        public IEnumerator AddItemToInventory(string charId, string itemId)
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

            InventoryAddWasSuccessful = true;
            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.AddItemToInventory()" + postRequest.error);
                InventoryAddWasSuccessful = false;
            }

            if (postRequest.downloadHandler.text == "false")
            {
                InventoryAddWasSuccessful = false;
                yield break;
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
            if (!int.TryParse(newLoadout.loadoutSlot.ToString(), out int result))
            {
                Debug.LogWarning("Could not parse loadout slot from new loadout! " + newLoadout.loadoutSlot);
            }

            yield return GetCharacterInventory(characterId.ToString());

            newLoadout.helmGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.helmGearItemId | item.id == newLoadout.helmGearItemId).id ?? "";
            newLoadout.capeGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.capeGearItemId | item.id == newLoadout.capeGearItemId).id ?? "";
            newLoadout.pantsGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.pantsGearItemId | item.id == newLoadout.pantsGearItemId).id ?? "";
            newLoadout.shouldersGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.shouldersGearItemId | item.id == newLoadout.shouldersGearItemId).id ?? "";
            newLoadout.chestArmorGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.chestArmorGearItemId | item.id == newLoadout.chestArmorGearItemId).id ?? "";
            newLoadout.glovesGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.glovesGearItemId | item.id == newLoadout.glovesGearItemId).id ?? "";
            newLoadout.beltGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.beltGearItemId | item.id == newLoadout.beltGearItemId).id ?? "";
            newLoadout.robeGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.robeGearItemId | item.id == newLoadout.robeGearItemId).id ?? "";
            newLoadout.bootsGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.bootsGearItemId | item.id == newLoadout.bootsGearItemId).id ?? "";
            newLoadout.weapon1ItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.weapon1ItemId | item.id == newLoadout.weapon1ItemId).id ?? "";
            newLoadout.weapon2ItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.weapon2ItemId | item.id == newLoadout.weapon2ItemId).id ?? "";

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
            if (!int.TryParse(loadoutSlot.ToString(), out int result))
            {
                Debug.LogWarning("Could not parse loadout slot from new loadout! " + loadoutSlot);
            }

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

        public string CharacterCreationError { get; private set; } = "";
        public IEnumerator CharacterPostRequest(Character character)
        {
            CharacterCreationError = "";
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
                CharacterCreationError = "Server Error";
                yield break;
            }

            // TODO account for web request failure in UI
            if (postRequest.downloadHandler.text == "false")
            {
                CharacterCreationError = "Failed To Create Character";
                yield break;
            }

            character._id = postRequest.downloadHandler.text;

            yield return GetCharacterInventory(postRequest.downloadHandler.text);

            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, GetDefaultDisplayLoadout(character.raceAndGender));

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

        public Character GetDefaultCharacter(CharacterReference.RaceAndGender raceAndGender)
        {
            switch (raceAndGender)
            {
                case CharacterReference.RaceAndGender.HumanMale:
                    return new Character("", "Human_Male", "", 0,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).material.name,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).material.name,
                        "null", "null", "null", 1,
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        CharacterReference.RaceAndGender.HumanMale);
                case CharacterReference.RaceAndGender.HumanFemale:
                    return new Character("", "Human_Female", "", 0,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).material.name,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).material.name,
                        "null",
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows).material.name,
                        "null", 1,
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        CharacterReference.RaceAndGender.HumanFemale);
                default:
                    Debug.LogError("Unsure how to handle race and gennder " + raceAndGender);
                    return default;
            }
        }

        public Character GetRandomizedCharacter(bool useDefaultPrimaryWeapon)
        {
            List<CharacterReference.RaceAndGender> raceAndGenderList = new List<CharacterReference.RaceAndGender>()
            {
                CharacterReference.RaceAndGender.HumanMale,
                CharacterReference.RaceAndGender.HumanFemale
            };

            CharacterReference.RaceAndGender raceAndGender = raceAndGenderList[Random.Range(0, raceAndGenderList.Count)];
            var model = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(raceAndGender);

            var characterMaterialOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender);
            var equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender);

            return new Character("", model.model.name,
                "Name", 0,
                characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).Random().material.name,
                characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).Random().material.name,

                raceAndGender != CharacterReference.RaceAndGender.HumanMale
                    ? "null"
                    : equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Beard).Random().GetModel(raceAndGender, null).name,

                raceAndGender == CharacterReference.RaceAndGender.HumanFemale
                    ? characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows).Random().material.name
                    : equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Brows).Random().GetModel(raceAndGender, null).name,

                equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Hair).Random().GetModel(raceAndGender, null).name,
                1,
                GetRandomizedLoadout(raceAndGender, useDefaultPrimaryWeapon),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                raceAndGender);
        }

        public static CharacterReference.WeaponOption GetDefaultPrimaryWeapon()
        {
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            return System.Array.Find(weaponOptions, item => item.name == "Flintblade");
        }

        public static CharacterReference.WeaponOption GetDefaultSecondaryWeapon()
        {
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            return System.Array.Find(weaponOptions, item => item.name == "Sylvan Sentinel");
        }

        public static Loadout GetDefaultDisplayLoadout(CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            
            return new Loadout("1",
                "",
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Chest & item.groupName == "Winterstalk").itemWebId,
                "",
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Boots & item.groupName == "Winterstalk").itemWebId,
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Pants & item.groupName == "Winterstalk").itemWebId,
                "",
                "",
                "",
                "",
                GetDefaultPrimaryWeapon().itemWebId,
                GetDefaultSecondaryWeapon().itemWebId,
                true);
        }

        public static readonly List<CharacterReference.EquipmentType> NullableEquipmentTypes = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Belt,
            CharacterReference.EquipmentType.Cape,
            CharacterReference.EquipmentType.Gloves,
            CharacterReference.EquipmentType.Helm,
            CharacterReference.EquipmentType.Shoulders,
            CharacterReference.EquipmentType.Robe
        };

        public Loadout GetRandomizedLoadout(CharacterReference.RaceAndGender raceAndGender, bool useDefaultPrimaryWeapon = false)
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

            int weapon1Index = useDefaultPrimaryWeapon ? 1 : Random.Range(0, weaponOptions.Length);

            var weaponOptionsOfDifferentClass = System.Array.FindAll(weaponOptions, item => item.weapon.GetWeaponClass() != weaponOptions[weapon1Index].weapon.GetWeaponClass());
            int weapon2Index = Random.Range(0, weaponOptionsOfDifferentClass.Length);
            weapon2Index = System.Array.FindIndex(weaponOptions, item => item.itemWebId == weaponOptionsOfDifferentClass[weapon2Index].itemWebId);

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
            public FixedString64Bytes _id;
            public FixedString64Bytes name;
            public FixedString64Bytes model;
            public FixedString64Bytes bodyColor;
            public FixedString64Bytes eyeColor;
            public FixedString64Bytes beard;
            public FixedString64Bytes brows;
            public FixedString64Bytes hair;
            public CharacterAttributes attributes;
            public Loadout loadoutPreset1;
            public Loadout loadoutPreset2;
            public Loadout loadoutPreset3;
            public Loadout loadoutPreset4;
            public FixedString64Bytes userId;
            public int slot;
            public int level;
            public int experience;
            public CharacterReference.RaceAndGender raceAndGender;

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level, Loadout loadoutPreset1, Loadout loadoutPreset2, Loadout loadoutPreset3, Loadout loadoutPreset4, CharacterReference.RaceAndGender raceAndGender)
            {
                loadoutPreset1.loadoutSlot = "1";
                loadoutPreset2.loadoutSlot = "2";
                loadoutPreset3.loadoutSlot = "3";
                loadoutPreset4.loadoutSlot = "4";

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
                        newLoadout.loadoutSlot = "1";
                        copy.loadoutPreset1 = newLoadout;
                        break;
                    case 1:
                        newLoadout.loadoutSlot = "2";
                        copy.loadoutPreset2 = newLoadout;
                        break;
                    case 2:
                        newLoadout.loadoutSlot = "3";
                        copy.loadoutPreset3 = newLoadout;
                        break;
                    case 3:
                        newLoadout.loadoutSlot = "4";
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

        public struct Loadout : INetworkSerializable, System.IEquatable<Loadout>
        {
            public FixedString64Bytes loadoutSlot;
            public FixedString64Bytes helmGearItemId;
            public FixedString64Bytes chestArmorGearItemId;
            public FixedString64Bytes shouldersGearItemId;
            public FixedString64Bytes bootsGearItemId;
            public FixedString64Bytes pantsGearItemId;
            public FixedString64Bytes beltGearItemId;
            public FixedString64Bytes glovesGearItemId;
            public FixedString64Bytes capeGearItemId;
            public FixedString64Bytes robeGearItemId;
            public FixedString64Bytes weapon1ItemId;
            public FixedString64Bytes weapon2ItemId;
            public bool active;

            public Loadout(FixedString64Bytes loadoutSlot, FixedString64Bytes helmGearItemId, FixedString64Bytes chestArmorGearItemId, FixedString64Bytes shouldersGearItemId, FixedString64Bytes bootsGearItemId, FixedString64Bytes pantsGearItemId, FixedString64Bytes beltGearItemId, FixedString64Bytes glovesGearItemId, FixedString64Bytes capeGearItemId, FixedString64Bytes robeGearItemId, FixedString64Bytes weapon1ItemId, FixedString64Bytes weapon2ItemId, bool active)
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

            public static Loadout GetEmptyLoadout()
            {
                return new Loadout();
            }

            public Loadout Copy()
            {
                return new Loadout(loadoutSlot, helmGearItemId, chestArmorGearItemId, shouldersGearItemId, bootsGearItemId, pantsGearItemId,
                    beltGearItemId, glovesGearItemId, capeGearItemId, robeGearItemId, weapon1ItemId, weapon2ItemId, active);
            }

            public List<FixedString64Bytes> GetLoadoutAsList()
            {
                return new List<FixedString64Bytes>()
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

            public Dictionary<CharacterReference.EquipmentType, FixedString64Bytes> GetLoadoutArmorPiecesAsDictionary()
            {
                return new Dictionary<CharacterReference.EquipmentType, FixedString64Bytes>()
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

            public string[] GetLoadoutItemIDsAsArray()
            {
                return new string[]
                {
                    helmGearItemId.ToString(),
                    chestArmorGearItemId.ToString(),
                    shouldersGearItemId.ToString(),
                    bootsGearItemId.ToString(),
                    pantsGearItemId.ToString(),
                    beltGearItemId.ToString(),
                    glovesGearItemId.ToString(),
                    capeGearItemId.ToString(),
                    robeGearItemId.ToString(),
                    weapon1ItemId.ToString(),
                    weapon2ItemId.ToString()
                };
            }

            public bool IsValid()
            {
                if (string.IsNullOrWhiteSpace(weapon1ItemId.ToString())) { return false; }
                if (string.IsNullOrWhiteSpace(weapon2ItemId.ToString())) { return false; }

                foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString64Bytes> kvp in GetLoadoutArmorPiecesAsDictionary())
                {
                    if (!NullableEquipmentTypes.Contains(kvp.Key))
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Value.ToString()))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public Loadout GetValidCopy(CharacterReference.RaceAndGender raceAndGender)
            {
                if (IsValid()) { return this; }

                Loadout validCopy = Copy();
                Loadout defaultLoadout = GetDefaultDisplayLoadout(raceAndGender);

                if (string.IsNullOrWhiteSpace(validCopy.weapon1ItemId.ToString())) { validCopy.weapon1ItemId = defaultLoadout.weapon1ItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.weapon2ItemId.ToString())) { validCopy.weapon2ItemId = defaultLoadout.weapon2ItemId; }

                if (string.IsNullOrWhiteSpace(validCopy.helmGearItemId.ToString())) { validCopy.helmGearItemId = defaultLoadout.helmGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.chestArmorGearItemId.ToString())) { validCopy.chestArmorGearItemId = defaultLoadout.chestArmorGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.shouldersGearItemId.ToString())) { validCopy.shouldersGearItemId = defaultLoadout.shouldersGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.bootsGearItemId.ToString())) { validCopy.bootsGearItemId = defaultLoadout.bootsGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.pantsGearItemId.ToString())) { validCopy.pantsGearItemId = defaultLoadout.pantsGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.beltGearItemId.ToString())) { validCopy.beltGearItemId = defaultLoadout.beltGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.glovesGearItemId.ToString())) { validCopy.glovesGearItemId = defaultLoadout.glovesGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.capeGearItemId.ToString())) { validCopy.capeGearItemId = defaultLoadout.capeGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.robeGearItemId.ToString())) { validCopy.robeGearItemId = defaultLoadout.robeGearItemId; }

                return validCopy;
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

            public bool Equals(Loadout other)
            {
                return loadoutSlot == other.loadoutSlot & helmGearItemId == other.helmGearItemId & chestArmorGearItemId == other.chestArmorGearItemId
                    & shouldersGearItemId == other.shouldersGearItemId & bootsGearItemId == other.bootsGearItemId & pantsGearItemId == other.pantsGearItemId
                    & beltGearItemId == other.beltGearItemId & glovesGearItemId == other.glovesGearItemId & capeGearItemId == other.capeGearItemId
                    & robeGearItemId == other.robeGearItemId & weapon1ItemId == other.weapon1ItemId & weapon2ItemId == other.weapon2ItemId;
            }

            public bool EqualsIgnoringSlot(Loadout other)
            {
                return helmGearItemId == other.helmGearItemId & chestArmorGearItemId == other.chestArmorGearItemId
                    & shouldersGearItemId == other.shouldersGearItemId & bootsGearItemId == other.bootsGearItemId & pantsGearItemId == other.pantsGearItemId
                    & beltGearItemId == other.beltGearItemId & glovesGearItemId == other.glovesGearItemId & capeGearItemId == other.capeGearItemId
                    & robeGearItemId == other.robeGearItemId & weapon1ItemId == other.weapon1ItemId & weapon2ItemId == other.weapon2ItemId;
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
            public double defense;
            public double hp;
            public double stamina;
            public double critChance;
            public double crit;
            public string id;

            public static CharacterJson DeserializeJson(string json)
            {
                List<CharacterJson> list = DeserializeJsonList('[' + json + ']');
                if (list.Count == 0)
                {
                    Debug.LogError("Unable to deserialize json body " + json);
                    return new CharacterJson();
                }
                else
                {
                    if (list.Count > 1) { Debug.LogError("There should only be one character in this body! " + json); }
                    return list[0];
                }
            }

            public static List<CharacterJson> DeserializeJsonList(string json)
            {
                List<CharacterJson> parsedElements = new List<CharacterJson>();

                List<string> splitStrings = new List<string>();
                string splitToAppend = "";
                char charToSplitOn = '"';
                for (int i = 0; i < json.Length; i++)
                {
                    if (json[i] == charToSplitOn)
                    {
                        if (splitToAppend.Length > 0)
                        {
                            if (splitToAppend[^1] == '\\')
                            {
                                splitToAppend = splitToAppend[0..^1] + json[i];
                                continue;
                            }
                        }

                        splitStrings.Add(splitToAppend);
                        splitToAppend = "";
                    }
                    else
                    {
                        splitToAppend += json[i];
                    }
                }

                CharacterJson element = new CharacterJson();
                element.loadOuts = new List<LoadoutJson>();
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    switch (splitStrings[i])
                    {
                        case "_id":
                            element = new CharacterJson();
                            element.loadOuts = new List<LoadoutJson>();
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("_id can't find a : in between the value!");
                                }
                                element._id = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property _id!");
                            }
                            break;
                        case "userId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("userId can't find a : in between the value!");
                                }
                                element.userId = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property userId!");
                            }
                            break;
                        case "slot":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.slot = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing slot property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property _id!");
                            }
                            break;
                        case "name":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("name can't find a : in between the value!");
                                }
                                element.name = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property name!");
                            }
                            break;
                        case "model":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("model can't find a : in between the value!");
                                }
                                element.model = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property model!");
                            }
                            break;
                        case "experience":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.experience = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing experience property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property experience!");
                            }
                            break;
                        case "bodyColor":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("bodyColor can't find a : in between the value!");
                                }
                                element.bodyColor = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property bodyColor!");
                            }
                            break;
                        case "eyeColor":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("eyeColor can't find a : in between the value!");
                                }
                                element.eyeColor = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property eyeColor!");
                            }
                            break;
                        case "beard":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("beard can't find a : in between the value!");
                                }
                                element.beard = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property beard!");
                            }
                            break;
                        case "brows":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("brows can't find a : in between the value!");
                                }
                                element.brows = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property brows!");
                            }
                            break;
                        case "hair":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("hair can't find a : in between the value!");
                                }
                                element.hair = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hair!");
                            }
                            break;
                        case "gender":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("gender can't find a : in between the value!");
                                }
                                element.gender = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property gender!");
                            }
                            break;
                        case "race":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("race can't find a : in between the value!");
                                }
                                element.race = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property race!");
                            }
                            break;
                        case "dateCreated":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("dateCreated can't find a : in between the value!");
                                }
                                element.dateCreated = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property dateCreated!");
                            }
                            break;
                        case "attributes":
                            List<string> splitAttributesStrings = new List<string>();
                            for (int j = i; j < splitStrings.Count; j++)
                            {
                                splitAttributesStrings.Add(splitStrings[j]);
                                if (splitStrings[j].Contains("},")) { break; }
                            }
                            element.attributes = CharacterAttributes.FromCharacterJsonExtract(splitAttributesStrings);
                            break;
                        case "loadoutSlot":
                            List<string> splitLoadoutStrings = new List<string>();
                            for (int j = i; j < splitStrings.Count; j++)
                            {
                                splitLoadoutStrings.Add(splitStrings[j]);
                                if (splitStrings[j].Contains("}")) { break; }
                            }
                            element.loadOuts.Add(LoadoutJson.FromCharacterJsonExtract(splitLoadoutStrings));
                            break;
                        case "level":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.level = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing level property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property level!");
                            }
                            break;
                        case "attack":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.attack = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing attack property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property attack!");
                            }
                            break;
                        case "defense":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.defense = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing defense property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property defense!");
                            }
                            break;
                        case "hp":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.hp = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing hp property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hp!");
                            }
                            break;
                        case "stamina":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.stamina = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing hp property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hp!");
                            }
                            break;
                        case "critChance":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.critChance = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing critChance property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property critChance!");
                            }
                            break;
                        case "crit":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.crit = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing crit property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property crit!");
                            }
                            break;
                        case "id":
                            parsedElements.Add(element);
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("id can't find a : in between the value!");
                                }
                                element.id = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property id!");
                            }
                            break;
                    }
                }
                return parsedElements;
            }

            public Character ToCharacter()
            {
                CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(char.ToUpper(race[0]) + race[1..].ToLower() + char.ToUpper(gender[0]) + gender[1..].ToLower());
                int loadout1Index = loadOuts.FindIndex(item => item.loadoutSlot == "1");
                int loadout2Index = loadOuts.FindIndex(item => item.loadoutSlot == "2");
                int loadout3Index = loadOuts.FindIndex(item => item.loadoutSlot == "3");
                int loadout4Index = loadOuts.FindIndex(item => item.loadoutSlot == "4");

                // Index values of -1 mean that this loadout doesn't exist in the API yet

                //return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level,
                //    loadout1Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout1Index].ToLoadout(),
                //    loadout2Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout2Index].ToLoadout(),
                //    loadout3Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout3Index].ToLoadout(),
                //    loadout4Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout4Index].ToLoadout(),
                //    raceAndGender);

                return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level,
                    loadout1Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout1Index].ToLoadout(),
                    loadout2Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout2Index].ToLoadout(),
                    loadout3Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout3Index].ToLoadout(),
                    loadout4Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout4Index].ToLoadout(),
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

            public static LoadoutJson FromCharacterJsonExtract(List<string> splitStrings)
            {
                LoadoutJson loadoutJson = new LoadoutJson();
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    string split = splitStrings[i].Replace("},", ",");

                    switch (split)
                    {
                        case "loadoutSlot":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    Debug.LogWarning("Loadout slot should not be null!");
                                }

                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("loadoutSlot can't find a : in between the value!");
                                }
                                loadoutJson.loadoutSlot = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property loadoutSlot!");
                            }
                            break;
                        case "helmGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.helmGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("helmGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.helmGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property helmGearItemId!");
                            }
                            break;
                        case "chestArmorGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.chestArmorGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("chestArmorGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.chestArmorGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property chestArmorGearItemId!");
                            }
                            break;
                        case "shouldersGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.shouldersGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("shouldersGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.shouldersGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property shouldersGearItemId!");
                            }
                            break;
                        case "bootsGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.bootsGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("bootsGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.bootsGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property bootsGearItemId!");
                            }
                            break;
                        case "pantsGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.pantsGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("pantsGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.pantsGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property pantsGearItemId!");
                            }
                            break;
                        case "beltGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.beltGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("beltGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.beltGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property beltGearItemId!");
                            }
                            break;
                        case "glovesGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.glovesGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("glovesGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.glovesGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property glovesGearItemId!");
                            }
                            break;
                        case "capeGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.capeGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("capeGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.capeGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property capeGearItemId!");
                            }
                            break;
                        case "robeGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.robeGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("robeGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.robeGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property robeGearItemId!");
                            }
                            break;
                        case "weapon1ItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.weapon1ItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("weapon1ItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.weapon1ItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon1ItemId!");
                            }
                            break;
                        case "weapon2ItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.weapon2ItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("weapon2ItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.weapon2ItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon2ItemId!");
                            }
                            break;
                        case "active":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (bool.TryParse(splitStrings[i + 1][1..^3], out bool result))
                                {
                                    loadoutJson.active = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing " + splitStrings[i + 1 ][1..^3]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon2ItemId!");
                            }
                            break;
                    }
                }
                return loadoutJson;
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

            public static CharacterAttributes FromCharacterJsonExtract(List<string> splitStrings)
            {
                CharacterAttributes attributes = new CharacterAttributes();
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    string split = splitStrings[i].Replace("},", ",");

                    switch (split)
                    {
                        case "strength":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.strength = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing strength property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property strength!");
                            }
                            break;
                        case "vitality":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.vitality = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing vitality property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property vitality!");
                            }
                            break;
                        case "agility":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.agility = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing agility property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property agility!");
                            }
                            break;
                        case "dexterity":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.strength = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing dexterity property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property dexterity!");
                            }
                            break;
                        case "intelligence":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^2], out int result))
                                {
                                    attributes.intelligence = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing intelligence property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property intelligence!");
                            }
                            break;
                    }
                }
                return attributes;
            }

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

        public IEnumerator SendHordeModeLeaderboardResult(string charId, string playerName, PlayerDataManager.GameMode gameMode, float clearTime, int wave, float damageDealt)
        {
            HordeModeLeaderboardResultPayload payload = new HordeModeLeaderboardResultPayload(charId, playerName, gameMode, clearTime, wave, damageDealt);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.SendHordeModeLeaderboardResult()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct HordeModeRecord
        {
            public string playerName;
            public string gameMode;
            public int wave;
            public float clearTime;
            public float damageDealt;
        }

        private struct HordeModeLeaderboardResultPayload
        {
            public string charId;
            public HordeModeRecord record;
            public string boardType;

            public HordeModeLeaderboardResultPayload(string charId, string playerName, PlayerDataManager.GameMode gameMode, float clearTime, int wave, float damageDealt)
            {
                this.charId = charId;
                record = new HordeModeRecord()
                {
                    playerName = playerName,
                    gameMode = PlayerDataManager.GetGameModeString(gameMode),
                    wave = wave,
                    clearTime = clearTime,
                    damageDealt = damageDealt
                };
                boardType = "horde";
            }
        }

        public IEnumerator SendKillsLeaderboardResult(string charId, string playerName, PlayerDataManager.GameMode gameMode, int kills, int deaths, int assists)
        {
            KillsLeaderboardResultPayload payload = new KillsLeaderboardResultPayload(charId, playerName, gameMode, kills, deaths, assists);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(APIURL + "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(APIURL + "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.SendKillsLeaderboardResult()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct KillsRecord
        {
            public string playerName;
            public string gameMode;
            public int kills;
            public int deaths;
            public int assists;
            public float KDA;
        }

        private struct KillsLeaderboardResultPayload
        {
            public string charId;
            public KillsRecord record;
            public string boardType;

            public KillsLeaderboardResultPayload(string charId, string playerName, PlayerDataManager.GameMode gameMode, int kills, int deaths, int assists)
            {
                this.charId = charId;
                record = new KillsRecord()
                {
                    playerName = playerName,
                    gameMode = PlayerDataManager.GetGameModeString(gameMode),
                    kills = kills,
                    deaths = deaths,
                    assists = assists,
                    KDA = deaths == 0 ? kills + assists : (kills + assists) / (float)deaths
                };
                boardType = "kills";
            }
        }

        public List<KillsLeaderboardEntry> killsLeaderboardEntries { get; private set; } = new List<KillsLeaderboardEntry>();
        public List<HordeLeaderboardEntry> hordeLeaderboardEntries { get; private set; } = new List<HordeLeaderboardEntry>();

        public IEnumerator GetLeaderboard()
        {
            // Kills leaderboard
            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "characters/getLeaderBoardSummary/kills");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetLeaderboard()");
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;

            killsLeaderboardEntries = JsonConvert.DeserializeObject<List<KillsLeaderboardEntry>>(json);

            getRequest.Dispose();

            // Horde mode leaderboard
            getRequest = UnityWebRequest.Get(APIURL + "characters/getLeaderboard/horde");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetLeaderboard()");
                getRequest.Dispose();
                yield break;
            }
            json = getRequest.downloadHandler.text;

            hordeLeaderboardEntries = JsonConvert.DeserializeObject<List<HordeLeaderboardEntry>>(json);

            getRequest.Dispose();
        }

        public struct KillsLeaderboardEntry
        {
            public string boardType;
            public string charId;
            public KillsRecord record;
        }

        public struct HordeLeaderboardEntry
        {
            public string boardType;
            public string charId;
            public HordeModeRecord record;
            public string dateCreated;
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
                    loadoutSlot = StringUtility.EvaluateFixedString(loadout.loadoutSlot),
                    helmGearItemId = StringUtility.EvaluateFixedString(loadout.helmGearItemId),
                    chestArmorGearItemId = StringUtility.EvaluateFixedString(loadout.chestArmorGearItemId),
                    shouldersGearItemId = StringUtility.EvaluateFixedString(loadout.shouldersGearItemId),
                    bootsGearItemId = StringUtility.EvaluateFixedString(loadout.bootsGearItemId),
                    pantsGearItemId = StringUtility.EvaluateFixedString(loadout.pantsGearItemId),
                    beltGearItemId = StringUtility.EvaluateFixedString(loadout.beltGearItemId),
                    glovesGearItemId = StringUtility.EvaluateFixedString(loadout.glovesGearItemId),
                    capeGearItemId = StringUtility.EvaluateFixedString(loadout.capeGearItemId),
                    robeGearItemId = StringUtility.EvaluateFixedString(loadout.robeGearItemId),
                    weapon1ItemId = StringUtility.EvaluateFixedString(loadout.weapon1ItemId),
                    weapon2ItemId = StringUtility.EvaluateFixedString(loadout.weapon2ItemId)
                };
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
            StartCoroutine(Initialize());
#if UNITY_EDITOR
            //StartCoroutine(CreateItems());
#endif
        }

        private IEnumerator Initialize()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            yield return GetPublicIP();
            APIURL = "http://" + PublicIP + ":80/";
#endif
            CheckGameVersion(false);
            yield return null;
        }

        private void Update()
        {
            if (thisServerCreated)
            {
                if (!IsRefreshingServers)
                {
                    RefreshServers();

                    if (thisServer.type == 0) // Hub
                    {
                        if (!System.Array.Exists(HubServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            FasterPlayerPrefs.QuitGame();
                        }
                    }
                    else if (thisServer.type == 1) // Lobby
                    {
                        if (!System.Array.Exists(LobbyServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            FasterPlayerPrefs.QuitGame();
                        }
                    }
                    else
                    {
                        Debug.LogError("Not sure how to handle server type: " + thisServer.type);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private IEnumerator CreateItems()
        {
            if (!Application.isEditor) { Debug.LogError("Trying to create items from a non-editor instance!"); yield break; }

            if (Application.internetReachability == NetworkReachability.NotReachable) { Debug.LogWarning("No internet connection, can't create items"); yield break; }

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
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield return new WaitUntil(() => SceneManager.GetActiveScene() == gameObject.scene);
                Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Error while checking game version, are you connected to the internet?";

                IsLoggedIn = true;
                currentlyLoggedInUserId = "";

                if (IsLoggedIn)
                {
                    RefreshCharacters();
                }
                yield break;
            }

            IsCheckingGameVersion = true;

            UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/version");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.VersionGetRequest() " + getRequest.error + APIURL + "game/version");
                getRequest.Dispose();
                if (Application.internetReachability != NetworkReachability.NotReachable)
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