using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LightPat.Core
{
    public class ClientManager : NetworkBehaviour
    {
        public GameObject[] playerPrefabOptions;
        public GameObject serverCameraPrefab;
        
        [HideInInspector] public NetworkVariable<ulong> gameLogicManagerNetObjId = new NetworkVariable<ulong>();
        [HideInInspector] public string playerHubIP;

        public NetworkVariable<ulong> lobbyLeaderId { get; private set; } = new NetworkVariable<ulong>();
        public NetworkVariable<GameMode> gameMode { get; private set; } = new NetworkVariable<GameMode>();
        private NetworkVariable<int> randomSeed = new NetworkVariable<int>();
        private Dictionary<ulong, ClientData> clientDataDictionary = new Dictionary<ulong, ClientData>();
        private Queue<KeyValuePair<ulong, ClientData>> queuedClientData = new Queue<KeyValuePair<ulong, ClientData>>();

        private static ClientManager _singleton;
        private static string payloadParseString = "|";

        public static ClientManager Singleton { get { return _singleton; } }

        public static string GetPayLoadParseString() { return payloadParseString; }

        public Dictionary<ulong, ClientData> GetClientDataDictionary() { return clientDataDictionary; }

        public ClientData GetClient(ulong clientId) { return clientDataDictionary[clientId]; }

        public void QueueClient(ulong clientId, ClientData clientData) { queuedClientData.Enqueue(new KeyValuePair<ulong, ClientData>(clientId, clientData)); }

        [ServerRpc(RequireOwnership = false)] public void UpdateGameModeServerRpc(GameMode newGameMode) { gameMode.Value = newGameMode; }

        public override void OnNetworkSpawn()
        {
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

        private IEnumerator SpawnLocalPlayerOnSceneChange(string sceneName)
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            SpawnPlayerServerRpc(NetworkManager.LocalClientId);
        }

        private IEnumerator WaitForServerSceneChange(string sceneName)
        {
            if (IsClient) { yield break; }
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            gameLogicManagerNetObjId.Value = FindObjectOfType<GameLogicManager>().NetworkObjectId;
            GameObject cameraObject = Instantiate(serverCameraPrefab);
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            foreach (GameObject g in playerPrefabOptions)
            {
                NetworkManager.Singleton.AddNetworkPrefab(g);
            }

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => { StartCoroutine(ClientConnectCallback(id)); Random.InitState(randomSeed.Value); };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { ClientDisconnectCallback(id); Random.InitState(randomSeed.Value); };

            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        void OnSceneLoad(Scene scene, LoadSceneMode mode) { Debug.Log("Loaded scene: " + scene.name + " - Mode: " + mode); }

        void OnSceneUnload(Scene scene) { Debug.Log("Unloaded scene: " + scene.name); }

        private void Update()
        {
            if (IsServer)
            {
                foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
                {
                    if (clientPair.Value.PlayerObject)
                        clientPair.Value.PlayerObject.GetComponent<Player.NetworkPlayer>().roundTripTime.Value = NetworkManager.Singleton.GetComponent<NetworkTransport>().GetCurrentRtt(clientPair.Key);
                }
            }
        }

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

            if (SceneManager.GetActiveScene().name == "Hub")
            {
                GameObject g = Instantiate(playerPrefabOptions[clientDataDictionary[clientId].playerPrefabOptionIndex], Vector3.zero, Quaternion.identity);
                g.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
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
            response.Approved = false;
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
            if (SceneManager.GetActiveScene().name == "Lobby" | SceneManager.GetActiveScene().name == "Hub")
            {
                response.Approved = true;
            }

            if (response.Approved)
            {
                string payload = System.Text.Encoding.ASCII.GetString(connectionData);
                string[] payloadOptions = payload.Split(payloadParseString);

                if (payloadOptions.Length == 2)
                {
                    QueueClient(clientId, new ClientData(payloadOptions[0], int.Parse(payloadOptions[1])));
                }
                else
                {
                    QueueClient(clientId, new ClientData(payloadOptions[0], 0));
                }
            }
        }

        [ClientRpc] void SynchronizeClientRpc(ulong clientId, ClientData clientData) { if (IsHost) { return; } clientDataDictionary[clientId] = clientData; }
        [ClientRpc] void AddClientRpc(ulong clientId, ClientData clientData) { if (IsHost) { return; } Debug.Log(clientData.clientName + " has connected. ID: " + clientId); clientDataDictionary.Add(clientId, clientData); }
        [ClientRpc] void RemoveClientRpc(ulong clientId) { clientDataDictionary.Remove(clientId); }
        [ClientRpc] void SpawnAllPlayersOnSceneChangeClientRpc(string sceneName) { StartCoroutine(SpawnLocalPlayerOnSceneChange(sceneName)); }

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

            NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>().OnPlayerKill(clientId);
        }

        public void AddDeaths(ulong clientId, int deathsToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDeaths(clientDataDictionary[clientId].deaths + deathsToAdd);
            SynchronizeClientDictionaries();

            NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>().OnPlayerDeath(clientId);
        }

        public void AddDamage(ulong clientId, int damageToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDamageDone(clientDataDictionary[clientId].damageDealt + damageToAdd);
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSceneServerRpc(ulong clientId, string sceneName, bool spawnPlayers)
        {
            if (clientId != lobbyLeaderId.Value) { Debug.LogError("You can only change the scene if you are the lobby leader!"); return; }
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            if (spawnPlayers)
            {
                StartCoroutine(WaitForServerSceneChange(sceneName));
                SpawnAllPlayersOnSceneChangeClientRpc(sceneName);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong clientId)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].SetReady(false);
            SynchronizeClientDictionaries();

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(gameLogicManagerNetObjId.Value))
            {
                GameLogicManager glm = NetworkManager.SpawnManager.SpawnedObjects[gameLogicManagerNetObjId.Value].GetComponent<GameLogicManager>();
                foreach (TeamSpawnPoint teamSpawnPoint in glm.spawnPoints)
                {
                    if (teamSpawnPoint.team == clientDataDictionary[clientId].team)
                    {
                        spawnPosition = teamSpawnPoint.spawnPosition;
                        spawnRotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                        break;
                    }
                }
            }
            
            GameObject g = Instantiate(playerPrefabOptions[clientDataDictionary[clientId].playerPrefabOptionIndex], spawnPosition, spawnRotation);
            g.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }

        public AsyncOperation ChangeLocalSceneThenStartClient(string sceneName)
        {
            //if (IsSpawned) { Debug.LogError("ChangeLocalSceneThenStartClient() should only be called when the network manager is turned off"); yield break; }
            Debug.Log("Loading " + sceneName + " scene");
            StartCoroutine(ChangeLocalSceneThenStartClientCoroutine(sceneName));
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private IEnumerator ChangeLocalSceneThenStartClientCoroutine(string sceneName)
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Started Client, looking for address: " + NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address);
            }
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

        public ClientData(string clientName, int playerPrefabOptionIndex)
        {
            this.clientName = clientName;
            ready = false;
            this.playerPrefabOptionIndex = playerPrefabOptionIndex;
            team = Team.Red;
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
