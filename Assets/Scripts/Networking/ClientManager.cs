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
        public NetworkVariable<ulong> lobbyLeaderId { get; private set; } = new NetworkVariable<ulong>();
        public NetworkVariable<GameMode> gameMode { get; private set; } = new NetworkVariable<GameMode>();
        private Dictionary<ulong, ClientData> clientDataDictionary = new Dictionary<ulong, ClientData>();
        private Queue<KeyValuePair<ulong, ClientData>> queuedClientData = new Queue<KeyValuePair<ulong, ClientData>>();

        private static ClientManager _singleton;

        public static ClientManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Debug.Log("Client Manager is Null");
                }

                return _singleton;
            }
        }

        public Dictionary<ulong, ClientData> GetClientDataDictionary()
        {
            return clientDataDictionary;
        }

        public ClientData GetClient(ulong clientId)
        {
            return clientDataDictionary[clientId];
        }

        public void QueueClient(ulong clientId, ClientData clientData)
        {
            queuedClientData.Enqueue(new KeyValuePair<ulong, ClientData>(clientId, clientData));
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateGameModeServerRpc(GameMode newGameMode)
        {
            gameMode.Value = newGameMode;
        }

        public override void OnNetworkSpawn()
        {
            lobbyLeaderId.OnValueChanged += OnLobbyLeaderChanged;
            if (IsServer) { RefreshLobbyLeader(); }
        }

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
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private IEnumerator SpawnServerCamera(string sceneName)
        {
            if (IsClient) { yield break; }
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            GameObject cameraObject = Instantiate(serverCameraPrefab);
            cameraObject.GetComponent<NetworkObject>().Spawn(true);
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            foreach (GameObject g in playerPrefabOptions)
            {
                NetworkManager.Singleton.AddNetworkPrefab(g);
            }

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => { StartCoroutine(ClientConnectCallback(id)); };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { ClientDisconnectCallback(id); };
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

            QueueClient(clientId, new ClientData(System.Text.Encoding.ASCII.GetString(connectionData)));
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

        [ServerRpc(RequireOwnership = false)]
        public void ChangeTeamServerRpc(ulong clientId, Team newTeam)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeTeam(newTeam);
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeColorsServerRpc(ulong clientId, Color[] newColors)
        {
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeColors(newColors);
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
        }

        public void AddDeaths(ulong clientId, int deathsToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDeaths(clientDataDictionary[clientId].deaths + deathsToAdd);
            SynchronizeClientDictionaries();
        }

        public void AddDamage(ulong clientId, float damageToAdd)
        {
            if (!IsServer) { Debug.LogError("This should only be modified on the server"); return; }
            if (!clientDataDictionary.ContainsKey(clientId)) { return; }
            clientDataDictionary[clientId] = clientDataDictionary[clientId].ChangeDamageDone(clientDataDictionary[clientId].damageDone + damageToAdd);
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSceneServerRpc(ulong clientId, string sceneName, bool spawnPlayers)
        {
            if (clientId != lobbyLeaderId.Value) { Debug.LogError("You can only change the scene if you are the lobby leader!"); return; }
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (spawnPlayers)
            {
                StartCoroutine(SpawnServerCamera(sceneName));
                SpawnAllPlayersOnSceneChangeClientRpc(sceneName);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnPlayerServerRpc(ulong clientId)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            foreach (TeamSpawnPoint teamSpawnPoint in FindObjectOfType<GameLogicManager>().spawnPoints)
            {
                if (teamSpawnPoint.team == clientDataDictionary[clientId].team)
                {
                    spawnPosition = teamSpawnPoint.spawnPosition;
                    spawnRotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }

            GameObject g = Instantiate(playerPrefabOptions[clientDataDictionary[clientId].playerPrefabOptionIndex], spawnPosition, spawnRotation);

            g.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    public struct ClientData : INetworkSerializable
    {
        public string clientName;
        public bool ready;
        public int playerPrefabOptionIndex;
        public Team team;
        public Color[] colors;
        public int[] spawnWeapons;
        public int kills;
        public int deaths;
        public float damageDone;

        public ClientData(string clientName)
        {
            this.clientName = clientName;
            ready = false;
            playerPrefabOptionIndex = 0;
            team = Team.Red;
            colors = new Color[1];
            spawnWeapons = new int[0];
            kills = 0;
            deaths = 0;
            damageDone = 0;
        }

        public ClientData ToggleReady()
        {
            ClientData copy = this;
            copy.ready = !copy.ready;
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

        public ClientData ChangeColors(Color[] newColorArray)
        {
            ClientData copy = this;
            copy.colors = newColorArray;
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

        public ClientData ChangeDamageDone(float newDamageDone)
        {
            ClientData copy = this;
            copy.damageDone = newDamageDone;
            return copy;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientName);
            serializer.SerializeValue(ref ready);
            serializer.SerializeValue(ref playerPrefabOptionIndex);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref colors);
            serializer.SerializeValue(ref spawnWeapons);
            serializer.SerializeValue(ref kills);
            serializer.SerializeValue(ref deaths);
            serializer.SerializeValue(ref damageDone);
        }
    }
}
