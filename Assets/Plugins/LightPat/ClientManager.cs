using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace LightPat.Core
{
    public class ClientManager : NetworkBehaviour
    {
        [SerializeField] private PlayerModelOption[] playerModelOptions;

        [System.Serializable]
        public class PlayerModelOption
        {
            public string name;
            public GameObject playerPrefab;
            public GameObject[] skinOptions;
        }

        [SerializeField] private GameObject spectatorPrefab;

        [HideInInspector] public NetworkVariable<ulong> gameLogicManagerNetObjId = new NetworkVariable<ulong>();
        // [HideInInspector] public const string serverAPIEndPointURL = "https://us-central1-vithegame.cloudfunctions.net/api/servers/duels";
        [HideInInspector] public string serverAPIEndPointURL;

        public NetworkVariable<ulong> lobbyLeaderId { get; private set; } = new NetworkVariable<ulong>();
        public NetworkVariable<GameMode> gameMode { get; private set; } = new NetworkVariable<GameMode>();

        public Dictionary<ulong, GameObject> localNetworkPlayers = new Dictionary<ulong, GameObject>();

        private NetworkVariable<int> randomSeed = new NetworkVariable<int>();
        private Dictionary<ulong, ClientData> clientDataDictionary = new Dictionary<ulong, ClientData>();
        private Queue<KeyValuePair<ulong, ClientData>> queuedClientData = new Queue<KeyValuePair<ulong, ClientData>>();

        private static ClientManager _singleton;
        IPManager iPManager = new IPManager();
        private static readonly string payloadParseString = "|";

        public static ClientManager Singleton { get { return _singleton; } }

        public static string GetPayLoadParseString() { return payloadParseString; }

        public Dictionary<ulong, ClientData> GetClientDataDictionary() { return clientDataDictionary; }

        public ClientData GetClient(ulong clientId) { return clientDataDictionary[clientId]; }

        public PlayerModelOption[] GetPlayerModelOptions() { return playerModelOptions; }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(iPManager.CheckAPI());
        }

        public void ResetAllClientData()
        {
            //if (!IsServer) { Debug.LogError("ResetAllClientData() should only be called on the server"); return; }

            //foreach (ulong clientId in clientDataDictionary.Keys)
            //{
            //    clientDataDictionary[clientId] = new ClientData(clientDataDictionary[clientId].clientName,
            //                                                    clientDataDictionary[clientId].playerPrefabOptionIndex,
            //                                                    clientDataDictionary[clientId].team);
            //}
        }

        public void QueueClient(ulong clientId, ClientData clientData) { queuedClientData.Enqueue(new KeyValuePair<ulong, ClientData>(clientId, clientData)); }

        [ServerRpc(RequireOwnership = false)] public void UpdateGameModeServerRpc(GameMode newGameMode) { gameMode.Value = newGameMode; }

        public override void OnNetworkSpawn()
        {
            NetworkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;

            lobbyLeaderId.OnValueChanged += OnLobbyLeaderChanged;
            randomSeed.OnValueChanged += OnRandomSeedChange;

            if (IsServer)
            {
                RefreshLobbyLeader();
                randomSeed.Value = Random.Range(0, 100);
            }
        }

        public override void OnNetworkDespawn()
        {
            lobbyLeaderId.OnValueChanged -= OnLobbyLeaderChanged;
            randomSeed.OnValueChanged -= OnRandomSeedChange;

            clientDataDictionary = new Dictionary<ulong, ClientData>();
        }

        private void OnRandomSeedChange(int prev, int current) { Random.InitState(current); }

        private void RefreshLobbyLeader()
        {
            if (clientDataDictionary.Count > 0)
                lobbyLeaderId.Value = clientDataDictionary.Keys.Min();
            else
                lobbyLeaderId.Value = 0;
        }

        private void OnLobbyLeaderChanged(ulong previous, ulong current)
        {
            if (current > 0)
                Debug.Log(clientDataDictionary[current].clientName + " is the new lobby leader.");
        }

        private void SynchronizeClientDictionaries()
        {
            foreach (ulong clientId in clientDataDictionary.Keys)
            {
                SynchronizeClientRpc(clientId, clientDataDictionary[clientId]);
            }
        }

        private IEnumerator SpawnLocalPlayerOnSceneChange(string sceneName, string additiveSceneName = null)
        {
            // Wait for the active scene to load
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            // Wait for an additive scene to load
            if (additiveSceneName != null) { yield return new WaitUntil(() => SceneManager.GetSceneByName(additiveSceneName).isLoaded); }
            SpawnPlayerServerRpc(NetworkManager.LocalClientId);
        }

        private IEnumerator WaitForServerSceneChange(string sceneName, string additiveSceneName = null)
        {
            if (IsClient) { yield break; }

            // Wait for the active scene to load
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

            GameLogicManager glm = FindObjectOfType<GameLogicManager>();
            if (glm)
            {
                gameLogicManagerNetObjId.Value = glm.NetworkObjectId;
            }
            else
            {
                gameLogicManagerNetObjId.Value = 0;
            }

            // Wait for an additive scene to load
            if (additiveSceneName != null) { yield return new WaitUntil(() => SceneManager.GetSceneByName(additiveSceneName).isLoaded); }

            // If we are not the editor and not a headless build
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                //Instantiate(spectatorPrefab);
            }
        }

        private void Start()
        {
            foreach (PlayerModelOption option in playerModelOptions)
            {
                NetworkManager.Singleton.AddNetworkPrefab(option.playerPrefab);
            }
            NetworkManager.Singleton.AddNetworkPrefab(spectatorPrefab);

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => { StartCoroutine(ClientConnectCallback(id)); Random.InitState(randomSeed.Value); };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { ClientDisconnectCallback(id); Random.InitState(randomSeed.Value); };

            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;

            this.serverAPIEndPointURL = iPManager.ServerAPIURL;
        }

        [SerializeField] private GameObject sceneLoadingScreenPrefab;

        public float SceneLoadingProgress { get; private set; }

        private Queue<string> additiveSceneQueue = new Queue<string>();
        private GameObject sceneLoadingScreenInstance;

        void OnNetworkSceneEvent(SceneEvent sceneEvent)
        {
            Debug.Log("Network Scene Event " + sceneEvent.SceneEventType + " " + sceneEvent.SceneName);

            currentSceneLoadingOperation = sceneEvent.AsyncOperation;

            if (IsServer)
            {
                // If we have finished loading a scene
                if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
                {
                    // Load all additive scenes in queue
                    if (additiveSceneQueue.Count > 0)
                    {
                        string additiveSceneName = additiveSceneQueue.Dequeue();
                        NetworkManager.SceneManager.LoadScene(additiveSceneName, LoadSceneMode.Additive);
                    }
                }
            }
        }

        void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            //Debug.Log("Loaded scene: " + scene.name + " - Mode: " + mode);
        }

        void OnSceneUnload(Scene scene)
        {
            //Debug.Log("Unloaded scene: " + scene.name);
        }

        private AsyncOperation currentSceneLoadingOperation;

        private void Update()
        {
            // Loading Screen
            // Async operation is null
            if (currentSceneLoadingOperation == null)
            {
                if (sceneLoadingScreenInstance)
                {
                    Destroy(sceneLoadingScreenInstance);
                }
            }
            else // Async operation is not null
            {
                SceneLoadingProgress = currentSceneLoadingOperation.progress;
                if (!sceneLoadingScreenInstance)
                {
                    sceneLoadingScreenInstance = Instantiate(sceneLoadingScreenPrefab);
                    DontDestroyOnLoad(sceneLoadingScreenInstance);
                }
            }

            // Remove empty player object references from local player object references
            foreach (var item in localNetworkPlayers.Where(kvp => kvp.Value == null).ToList())
            {
                localNetworkPlayers.Remove(item.Key);
            }

            if (IsServer)
            {
                if (SceneManager.GetActiveScene().name != "Login" & !updateServerStatusRunning) { StartCoroutine(UpdateServerStatus()); }

                foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
                {
                    if (clientPair.Value.PlayerObject)
                    {
                        if (clientPair.Value.PlayerObject.TryGetComponent(out Player.NetworkPlayer networkPlayer))
                        {
                            networkPlayer.roundTripTime.Value = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(clientPair.Key);
                        }
                        else if (clientPair.Value.PlayerObject.TryGetComponent(out SpectatorCamera spectatorCamera))
                        {
                            spectatorCamera.roundTripTime.Value = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(clientPair.Key);
                        }
                    }
                }
            }
        }

        [SerializeField] private string[] sceneNamesToSpawnPlayerOnConnect;
        private IEnumerator ClientConnectCallback(ulong clientId)
        {
            yield return null;
            if (!IsServer) { yield break; }
            KeyValuePair<ulong, ClientData> valuePair = queuedClientData.Dequeue();
            clientDataDictionary.Add(valuePair.Key, valuePair.Value);
            Debug.Log(valuePair.Value.clientName + " has connected. ID: " + clientId);
            AddClientRpc(valuePair.Key, valuePair.Value);
            SynchronizeClientDictionaries();
            if (lobbyLeaderId.Value == 0) { RefreshLobbyLeader(); }

            if (sceneNamesToSpawnPlayerOnConnect.Contains(SceneManager.GetActiveScene().name)) { SpawnPlayer(clientId); }
            else if (clientDataDictionary[clientId].team == Team.Spectator) { SpawnPlayer(clientId); }
        }

        private bool updateServerStatusRunning;
        private IEnumerator UpdateServerStatus()
        {
            updateServerStatusRunning = true;
            // Get list of servers in the API
            UnityWebRequest getRequest = UnityWebRequest.Get(serverAPIEndPointURL);

            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Endpoint: " + serverAPIEndPointURL);
                Debug.LogError("ClientManager Get Request Error in ClientManager.UpdateServerPopulation() " + serverAPIEndPointURL);
                updateServerStatusRunning = false;
                yield break;
            }

            List<Server> serverList = new List<Server>();

            string json = getRequest.downloadHandler.text;

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

                    serverList.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                }
            }

            string[] gameplayScenes = new string[] { "Hub", "Duel", "TeamElimination" };
            // PUT request to update duel server API
            bool thisServerIsInAPI = false;
            var networkTransport = NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            foreach (Server server in serverList)
            {
                if (server.ip == networkTransport.ConnectionData.Address & ushort.Parse(server.port) == networkTransport.ConnectionData.Port)
                {
                    thisServerIsInAPI = true;
                    yield return PutRequest(new ServerPutPayload(server._id, clientDataDictionary.Count, gameplayScenes.Contains(SceneManager.GetActiveScene().name) ? 1 : 0, server.port));
                    break;
                }
            }

            if (!thisServerIsInAPI)
            {
                if ((postRequestCalled | putRequestCalled) & clientDataDictionary.Count == 0)
                {
                    Application.Quit();
                }
                else
                {
                    ServerPostPayload payload = new ServerPostPayload(SceneManager.GetActiveScene().name == "Hub" ? 1 : 0,
                                                                      clientDataDictionary.Count,
                                                                      gameplayScenes.Contains(SceneManager.GetActiveScene().name) ? 1 : 0,
                                                                      networkTransport.ConnectionData.Address,
                                                                      SceneManager.GetActiveScene().name,
                                                                      networkTransport.ConnectionData.Port.ToString());
                    yield return PostRequest(payload);
                }
            }
            updateServerStatusRunning = false;
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
        }

        private bool putRequestCalled;
        private IEnumerator PutRequest(ServerPutPayload payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(serverAPIEndPointURL, jsonData);

            putRequest.SetRequestHeader("Content-Type", "application/json");

            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in ClientManager.PutRequest " + putRequest.error);
            }
            putRequestCalled = true;
        }

        private struct ServerPutPayload
        {
            public string serverId;
            public int population;
            public int progress;
            public string port;

            public ServerPutPayload(string serverId, int population, int progress, string port)
            {
                this.serverId = serverId;
                this.population = population;
                this.progress = progress;
                this.port = port;
            }
        }

        private bool postRequestCalled;
        private IEnumerator PostRequest(ServerPostPayload payload)
        {
            WWWForm form = new WWWForm();
            form.AddField("type", payload.type);
            form.AddField("population", payload.population);
            form.AddField("progress", payload.progress);
            form.AddField("ip", payload.ip);
            form.AddField("label", payload.label);
            form.AddField("port", payload.port);

            UnityWebRequest postRequest = UnityWebRequest.Post(serverAPIEndPointURL, form);

            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in ClientManager.PostRequest " + postRequest.error);
            }
            postRequestCalled = true;
        }

        private struct ServerPostPayload
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

        void ClientDisconnectCallback(ulong clientId)
        {
            Debug.Log(clientDataDictionary[clientId].clientName + " has disconnected. ID: " + clientId);
            if (!IsServer) { return; }
            clientDataDictionary.Remove(clientId);
            if (clientId == lobbyLeaderId.Value) { RefreshLobbyLeader(); }
            RemoveClientRpc(clientId);
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // The client identifier to be authenticated
            var clientId = request.ClientNetworkId;

            // Additional connection data defined by user code
            var connectionData = request.Payload;

            // Your approval logic determines the following values
            response.Approved = true;
            response.CreatePlayerObject = false;

            // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
            response.PlayerPrefabHash = null;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            response.Position = Vector3.zero;

            // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
            response.Rotation = Quaternion.identity;

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;

            // Only allow clients to connect if the server is at the lobby scene or the hub scene

            if (response.Approved)
            {
                string payload = System.Text.Encoding.ASCII.GetString(connectionData);
                string[] payloadOptions = payload.Split(payloadParseString);

                //string[] spawnPlayerSceneNames = new string[] { "Lobby", "Hub", "Prototype" };
                //Team clientTeam = spawnPlayerSceneNames.Contains(SceneManager.GetActiveScene().name) ? Team.Red : Team.Spectator;
                Team clientTeam = SceneManager.GetActiveScene().name == "Lobby" | SceneManager.GetActiveScene().name == "Hub" ? Team.Competitor : Team.Spectator;
                clientTeam = Team.Competitor;
                if (payloadOptions.Length == 2)
                {
                    QueueClient(clientId, new ClientData(payloadOptions[0], int.Parse(payloadOptions[1]), clientTeam));
                }
                else
                {
                    QueueClient(clientId, new ClientData(payloadOptions[0], 0, clientTeam));
                }
            }
        }

        [ClientRpc] void SynchronizeClientRpc(ulong clientId, ClientData clientData) { if (IsHost) { return; } clientDataDictionary[clientId] = clientData; }
        [ClientRpc] void AddClientRpc(ulong clientId, ClientData clientData) { if (IsHost) { return; } Debug.Log(clientData.clientName + " has connected. ID: " + clientId); clientDataDictionary.Add(clientId, clientData); }
        [ClientRpc] void RemoveClientRpc(ulong clientId) { clientDataDictionary.Remove(clientId); }
        [ClientRpc] void SpawnAllPlayersOnSceneChangeClientRpc(string sceneName, string additiveSceneName = null) { StartCoroutine(SpawnLocalPlayerOnSceneChange(sceneName, additiveSceneName)); }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyServerRpc(ulong clientId)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ToggleReady();
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerPrefabOptionServerRpc(ulong clientId, int newPlayerPrefabIndex)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangePlayerPrefabOption(newPlayerPrefabIndex);
            SynchronizeClientDictionaries();
        }

        public void ChangeTeamOnServer(ulong clientId, Team newTeam)
        {
            if (!IsServer) { Debug.LogError("ChangeTeamOnServer() should only be called on the server."); return; }
            ChangeTeam(clientId, newTeam);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeTeamServerRpc(ulong clientId, Team newTeam)
        {
            ChangeTeam(clientId, newTeam);
        }

        private void ChangeTeam(ulong clientId, Team newTeam)
        {
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            if (clientDataDictionary[clientId].team == newTeam) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeTeam(newTeam);
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSpawnWeaponsServerRpc(ulong clientId, int[] newSpawnWeaponIndexes)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeSpawnWeapons(newSpawnWeaponIndexes);
            SynchronizeClientDictionaries();
        }

        public void AddKills(ulong clientId, int killsToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeKills(clientDataDictionary[clientId].kills + killsToAdd);
            SynchronizeClientDictionaries();

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(gameLogicManagerNetObjId.Value))
                NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>().OnPlayerKill(clientId);
        }

        public void AddDeaths(ulong clientId, int deathsToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDeaths(clientDataDictionary[clientId].deaths + deathsToAdd);
            SynchronizeClientDictionaries();

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(gameLogicManagerNetObjId.Value))
                NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>().OnPlayerDeath(clientId);
        }

        public void AddDamage(ulong clientId, int damageToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDamageDone(clientDataDictionary[clientId].damageDealt + damageToAdd);
            SynchronizeClientDictionaries();
        }

        public void ChangeScene(string sceneName, bool spawnPlayers, string additiveScene = null)
        {
            if (IsServer)
            {
                ApplyNetworkSceneChange(NetworkManager.LocalClientId, sceneName, spawnPlayers, additiveScene);
            }
            else
            {
                ChangeSceneServerRpc(NetworkManager.LocalClientId, sceneName, spawnPlayers, additiveScene);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangeSceneServerRpc(ulong clientId, string sceneName, bool spawnPlayers, string additiveScene)
        {
            ApplyNetworkSceneChange(clientId, sceneName, spawnPlayers, additiveScene);
        }

        private void ApplyNetworkSceneChange(ulong clientId, string sceneName, bool spawnPlayers, string additiveScene)
        {
            if (clientId != lobbyLeaderId.Value) { Debug.LogError("You can only change the scene if you are the lobby leader!"); return; }
            if (!IsServer) { Debug.LogError("You should only change the scene on the server!"); return; }

            if (additiveScene != null)
                additiveSceneQueue.Enqueue(additiveScene);

            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            if (spawnPlayers)
            {
                StartCoroutine(WaitForServerSceneChange(sceneName, additiveScene));
                SpawnAllPlayersOnSceneChangeClientRpc(sceneName, additiveScene);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong clientId)
        {
            SpawnPlayer(clientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (!IsServer) { Debug.LogError("SpawnPlayer() should only be called from the server!"); return; }

            clientDataDictionary[clientId] = clientDataDictionary[clientId].SetReady(false);
            SynchronizeClientDictionaries();

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(gameLogicManagerNetObjId.Value))
            {
                GameLogicManager glm = NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>();
                KeyValuePair<Vector3, Quaternion> spawnOrientation = glm.GetSpawnOrientation(clientDataDictionary[clientId].team);
                spawnPosition = spawnOrientation.Key;
                spawnRotation = spawnOrientation.Value;
            }
            else
            {
                Debug.LogError("No game logic manager found in scene. This means that players will not have a set spawn point");
            }

            GameObject g;
            if (clientDataDictionary[clientId].team == Team.Spectator)
            {
                g = Instantiate(spectatorPrefab, spawnPosition, spawnRotation);
            }
            else // If the team is not spectator
            {
                g = Instantiate(playerModelOptions[clientDataDictionary[clientId].playerPrefabOptionIndex].playerPrefab, spawnPosition, spawnRotation);
            }
            g.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    public struct ClientData : INetworkSerializable
    {
        public string clientName;
        public bool ready;
        public int playerPrefabOptionIndex;
        public Team team;
        public int[] spawnWeapons;
        public int kills;
        public int deaths;
        public int damageDealt;

        public ClientData(string clientName, int playerPrefabOptionIndex, Team team)
        {
            this.clientName = clientName;
            ready = false;
            this.playerPrefabOptionIndex = playerPrefabOptionIndex;
            this.team = team;
            spawnWeapons = new int[0];
            kills = 0;
            deaths = 0;
            damageDealt = 0;
        }

        public ClientData ToggleReady()
        {
            ClientData copy = this;
            copy.ready = !copy.ready;
            return copy;
        }

        public ClientData SetReady(bool status)
        {
            ClientData copy = this;
            copy.ready = status;
            return copy;
        }

        public ClientData ChangePlayerPrefabOption(int newOption)
        {
            ClientData copy = this;
            copy.playerPrefabOptionIndex = newOption;
            return copy;
        }

        public ClientData ChangeTeam(Team newTeam)
        {
            ClientData copy = this;
            copy.team = newTeam;
            return copy;
        }

        public ClientData ChangeSpawnWeapons(int[] newWeapons)
        {
            ClientData copy = this;
            copy.spawnWeapons = newWeapons;
            return copy;
        }

        public ClientData ChangeKills(int newKills)
        {
            ClientData copy = this;
            copy.kills = newKills;
            return copy;
        }

        public ClientData ChangeDeaths(int newDeaths)
        {
            ClientData copy = this;
            copy.deaths = newDeaths;
            return copy;
        }

        public ClientData ChangeDamageDone(int newDamageDealt)
        {
            ClientData copy = this;
            copy.damageDealt = newDamageDealt;
            return copy;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientName);
            serializer.SerializeValue(ref ready);
            serializer.SerializeValue(ref playerPrefabOptionIndex);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref spawnWeapons);
            serializer.SerializeValue(ref kills);
            serializer.SerializeValue(ref deaths);
            serializer.SerializeValue(ref damageDealt);
        }
    }
}
