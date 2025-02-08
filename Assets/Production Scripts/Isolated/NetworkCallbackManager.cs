using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Vi.Utility;
using System.Linq;
using Vi.Core;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.Isolated
{
    public class NetworkCallbackManager : MonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject networkManagerPrefab;
        [SerializeField] private AssetReferenceGameObject playerDataManagerPrefab;
        [SerializeField] private AssetReferenceGameObject networkSceneManagerPrefab;
        [SerializeField] private AssetReferenceGameObject[] networkPrefabsToLoadAsynchronously;
        [SerializeField] private NetworkPrefabHashOverride[] networkPrefabOverrides;
        [Header("Must be less than 60 seconds")]
        [SerializeField] private float clientConnectTimeoutThreshold = 30;
        [SerializeField] private GameObject alertBoxPrefab;

        [System.Serializable]
        private struct NetworkPrefabHashOverride
        {
            public int index;
            public uint hashOverride;
        }

        public AsyncOperationHandle<GameObject>[] NetworkPrefabsLoading { get; private set; } = new AsyncOperationHandle<GameObject>[0];

        private void Awake()
        {
            NetworkPrefabsLoading = new AsyncOperationHandle<GameObject>[networkPrefabsToLoadAsynchronously.Length];
        }

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private void Start()
        {
            if (clientConnectTimeoutThreshold >= 60) { Debug.LogError("Client connect timeout is greater than 60 seconds! The network manager will turn off before then!"); }

            StartCoroutine(LoadMainMenu());
        }

        public AsyncOperationHandle<GameObject> NetworkManagerLoadingOperation { get; private set; }

        private IEnumerator LoadMainMenu()
        {
            yield return new WaitUntil(() => AudioManager.AudioConfigurationApplied);
            NetworkManagerLoadingOperation = networkManagerPrefab.InstantiateAsync();
            yield return NetworkManagerLoadingOperation;
            yield return new WaitUntil(() => NetworkManager.Singleton);
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnServerStopped += OnClientStopped;
            NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

            LoadedNetSceneManagerPrefab = networkSceneManagerPrefab.LoadAssetAsync();
            yield return new WaitUntil(() => LoadedNetSceneManagerPrefab.IsDone);
            NetworkManager.Singleton.AddNetworkPrefab(LoadedNetSceneManagerPrefab.Result);
            CreateNetSceneManager();

            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Base");
            NetSceneManager.Singleton.LoadScene("Main Menu");
            yield return new WaitUntil(() => NetSceneManager.Singleton.IsSceneGroupLoaded("Main Menu"));

            loadedPlayerDataManagerPrefab = playerDataManagerPrefab.LoadAssetAsync();
            yield return new WaitUntil(() => loadedPlayerDataManagerPrefab.IsDone);
            NetworkManager.Singleton.AddNetworkPrefab(loadedPlayerDataManagerPrefab.Result);
            CreatePlayerDataManager(false);

            for (int i = 0; i < networkPrefabsToLoadAsynchronously.Length; i++)
            {
                NetworkPrefabsLoading[i] = networkPrefabsToLoadAsynchronously[i].LoadAssetAsync();
                yield return NetworkPrefabsLoading[i];
                NetworkManager.Singleton.AddNetworkPrefab(NetworkPrefabsLoading[i].Result);
            }

            foreach (NetworkPrefabHashOverride overide in networkPrefabOverrides)
            {
                NetworkPrefab netPref = new NetworkPrefab()
                {
                    Override = NetworkPrefabOverride.Hash,
                    Prefab = NetworkPrefabsLoading[overide.index].Result,
                    OverridingTargetPrefab = NetworkPrefabsLoading[overide.index].Result,
                    SourceHashToOverride = overide.hashOverride
                };
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(netPref);
            }

            ObjectPoolingManager.CanPool = true;
        }

        private AsyncOperationHandle<GameObject> loadedPlayerDataManagerPrefab;
        private void CreatePlayerDataManager(bool forceRefresh)
        {
            if (forceRefresh & PlayerDataManager.DoesExist()) { Destroy(PlayerDataManager.Singleton.gameObject); }
            if (!PlayerDataManager.DoesExist() | forceRefresh)
            {
                DontDestroyOnLoad(Instantiate(loadedPlayerDataManagerPrefab.Result));
            }
        }

        public AsyncOperationHandle<GameObject> LoadedNetSceneManagerPrefab { get; private set; }
        private void CreateNetSceneManager()
        {
            if (!NetSceneManager.DoesExist())
            {
                DontDestroyOnLoad(Instantiate(LoadedNetSceneManagerPrefab.Result));
            }
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

            // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
            response.PlayerPrefabHash = null;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            //response.Position = Vector3.zero;

            // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
            //response.Rotation = Quaternion.identity;

            // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
            // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
            //response.Reason = "Some reason for not approving the client";

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;

            string payload = System.Text.Encoding.ASCII.GetString(connectionData);
            //Debug.Log("ClientId: " + clientId + " has been approved. Payload: " + payload);

            playerDataQueue.Enqueue(new PlayerDataInput(payload, clientId, PlayerDataManager.Singleton.GetBestChannel()));
        }

        private struct PlayerDataInput
        {
            public string characterId;
            public ulong clientId;
            public int channel;

            public PlayerDataInput(string characterId, ulong clientId, int channel)
            {
                this.characterId = characterId;
                this.clientId = clientId;
                this.channel = channel;
            }
        }

        private Queue<PlayerDataInput> playerDataQueue = new Queue<PlayerDataInput>();
        private void Update()
        {
            if (playerDataQueue.Count > 0)
            {
                if (!addPlayerDataRunning & !NetSceneManager.IsBusyLoadingScenes())
                {
                    PlayerDataManager.Team clientTeam;
                    if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
                    {
                        clientTeam = PlayerDataManager.Team.Peaceful;
                    }
                    else if (NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby")
                        | NetSceneManager.Singleton.IsSceneGroupLoaded("Training Room")
                        | NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room"))
                    {
                        List<PlayerDataManager.PlayerData> playerDataListWithoutSpectators = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators();
                        if (playerDataListWithoutSpectators.Count >= PlayerDataManager.Singleton.GetMaxPlayersForMap())
                        {
                            clientTeam = PlayerDataManager.Team.Spectator;
                        }
                        else // The lobby isn't full yet
                        {
                            PlayerDataManager.GameModeInfo gameModeInfo = PlayerDataManager.Singleton.GetGameModeInfo();
                            Dictionary<PlayerDataManager.Team, int> teamCounts = new Dictionary<PlayerDataManager.Team, int>();
                            foreach (PlayerDataManager.Team possibleTeam in gameModeInfo.possibleTeams)
                            {
                                teamCounts.Add(possibleTeam, playerDataListWithoutSpectators.Where(item => item.team == possibleTeam).ToArray().Length);
                            }
                            // Get the team with the lowest player count
                            clientTeam = teamCounts.Aggregate((l, r) => l.Value <= r.Value ? l : r).Key;
                        }
                    }
                    else // Game in progress
                    {
                        clientTeam = PlayerDataManager.Team.Spectator;
                    }

                    PlayerDataInput data = playerDataQueue.Dequeue();
                    StartCoroutine(AddPlayerData(data.characterId, data.clientId, data.channel, clientTeam));
                }
            }

            if (NetworkManager.Singleton)
            {
                if (!NetworkManager.Singleton.IsConnectedClient & lastConnectedClientState)
                {
                    CreatePlayerDataManager(true);
                }
                lastConnectedClientState = NetworkManager.Singleton.IsConnectedClient;
            }
        }
        private bool lastConnectedClientState;

        private bool addPlayerDataRunning;
        private IEnumerator AddPlayerData(string characterId, ulong clientId, int channel, PlayerDataManager.Team team)
        {
            addPlayerDataRunning = true;

            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            WebRequestManager.Singleton.CharacterManager.GetCharacterById(characterId);
            yield return new WaitUntil(() => !WebRequestManager.Singleton.CharacterManager.IsGettingCharacterById);
            // If the game crashed, or the player disconnected for some reason, don't add their data
            if (NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
            {
                if (WebRequestManager.Singleton.CharacterManager.LastCharacterByIdWasSuccessful)
                {
                    PlayerDataManager.Singleton.AddPlayerData(new PlayerDataManager.PlayerData((int)clientId,
                    channel,
                    WebRequestManager.Singleton.CharacterManager.CharacterById,
                    team));
                }
                else
                {
                    NetworkManager.Singleton.DisconnectClient(clientId, "Invalid Character Id.");
                }
            }

            addPlayerDataRunning = false;
        }

        private void OnServerStarted()
        {
            StartCoroutine(CreateServerInAPI());
        }

        private IEnumerator CreateServerInAPI()
        {
            if (NetworkManager.Singleton.IsClient) { yield break; }
            Debug.Log("Started Server at " + networkTransport.ConnectionData.Address + ". Make sure you opened port " + networkTransport.ConnectionData.Port + " for UDP traffic!");

            yield return new WaitUntil(() => NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub") | NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby"));

            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
            {
                yield return WebRequestManager.Singleton.SetGameVersion();

                yield return WebRequestManager.Singleton.ServerManager.ServerPostRequest(new ServerManager.ServerPostPayload(0, PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count,
                    1, networkTransport.ConnectionData.Address, "Hub", networkTransport.ConnectionData.Port.ToString(), ""));
            }
            else if (NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby"))
            {
                yield return WebRequestManager.Singleton.ServerManager.ServerPostRequest(new ServerManager.ServerPostPayload(1, PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count,
                    0, networkTransport.ConnectionData.Address, "Lobby", networkTransport.ConnectionData.Port.ToString(), ""));
            }
            else
            {
                Debug.LogError("Not sure what scene group is loaded to create server");
            }

            Debug.Log("Finished Creating Server in API");
        }

        private void OnServerStopped(bool wasHost)
        {
            Debug.Log("Stopped Server " + wasHost);
        }

        private void OnClientStarted()
        {
            Debug.Log("Started Client at IP Address: " + networkTransport.ConnectionData.Address + " - Port: " + networkTransport.ConnectionData.Port + " - Payload: " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            if (createInstancesCoroutine != null) { StopCoroutine(createInstancesCoroutine); }
            StartCoroutine(ClientConnectTimeout());
        }

        private IEnumerator ClientConnectTimeout()
        {
            float startTime = Time.time;
            while (Time.time - startTime < clientConnectTimeoutThreshold)
            {
                if (NetworkManager.Singleton.IsConnectedClient) { yield break; }
                yield return null;
            }

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
                if (!NetSceneManager.Singleton.IsSceneGroupLoaded("Character Select")) { NetSceneManager.Singleton.LoadScene("Character Select"); }
                Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Could not connect to server.";
            }

            CreatePlayerDataManager(false);
            CreateNetSceneManager();
        }

        private void OnClientStopped(bool wasHost)
        {
            Debug.Log("Stopped Client " + wasHost);

            if (createInstancesCoroutine != null) { StopCoroutine(createInstancesCoroutine); }
            StartCoroutine(CreateInstances());
        }

        private Coroutine createInstancesCoroutine;
        private IEnumerator CreateInstances()
        {
            yield return new WaitUntil(() => !NetSceneManager.DoesExist() | !PlayerDataManager.DoesExist());
            CreatePlayerDataManager(false);
            CreateNetSceneManager();
        }

        private void OnTransportFailure()
        {
            Debug.Log("Transport failure at time: " + Time.time);
        }
    }
}