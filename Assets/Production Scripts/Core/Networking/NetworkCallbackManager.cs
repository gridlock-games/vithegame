using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

namespace Vi.Core
{
    public class NetworkCallbackManager : MonoBehaviour
    {
        [SerializeField] private PlayerDataManager playerDataManagerPrefab;
        [SerializeField] private NetSceneManager networkSceneManagerPrefab;
        [Header("Must be less than 60 seconds")]
        [SerializeField] private float clientConnectTimeoutThreshold = 30;
        [SerializeField] private GameObject alertBoxPrefab;

        private void Start()
        {
            if (clientConnectTimeoutThreshold >= 60) { Debug.LogWarning("Client connect timeout is greater than 60 seconds! The network manager will turn off before then!"); }

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            DontDestroyOnLoad(Instantiate(networkSceneManagerPrefab.gameObject));
            DontDestroyOnLoad(Instantiate(playerDataManagerPrefab.gameObject));
            NetSceneManager.Singleton.LoadScene("Main Menu");

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnServerStopped += OnClientStopped;
            NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
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

            playerDataQueue.Enqueue(new PlayerDataInput(payload, (int)clientId));
        }

        private struct PlayerDataInput
        {
            public string characterId;
            public int clientId;

            public PlayerDataInput(string characterId, int clientId)
            {
                this.characterId = characterId;
                this.clientId = clientId;
            }
        }

        private Queue<PlayerDataInput> playerDataQueue = new Queue<PlayerDataInput>();
        private void Update()
        {
            if (playerDataQueue.Count > 0)
            {
                if (!addPlayerDataRunning & !NetSceneManager.Singleton.IsBusyLoadingScenes())
                {
                    PlayerDataManager.Team clientTeam = NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub") ? PlayerDataManager.Team.Peaceful : PlayerDataManager.Team.Competitor;

                    if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
                    {
                        clientTeam = PlayerDataManager.Team.Peaceful;
                    }
                    else if (NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby") | NetSceneManager.Singleton.IsSceneGroupLoaded("Training Room"))
                    {
                        clientTeam = PlayerDataManager.Team.Competitor;
                    }
                    else // Game in progress
                    {
                        clientTeam = PlayerDataManager.Team.Spectator;
                    }

                    PlayerDataInput data = playerDataQueue.Dequeue();
                    StartCoroutine(AddPlayerData(data.characterId, data.clientId, clientTeam));
                }
            }
        }

        private bool addPlayerDataRunning;
        private IEnumerator AddPlayerData(string characterId, int clientId, PlayerDataManager.Team team)
        {
            addPlayerDataRunning = true;

            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            WebRequestManager.Singleton.GetCharacterById(characterId);
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsGettingCharacterById);
            PlayerDataManager.Singleton.AddPlayerData(new PlayerDataManager.PlayerData(clientId, WebRequestManager.Singleton.CharacterById, team));
            
            addPlayerDataRunning = false;
        }

        private void OnServerStarted()
        {
            StartCoroutine(CreateServerInAPI());
        }

        private IEnumerator CreateServerInAPI()
        {
            if (NetworkManager.Singleton.IsClient) { yield break; }
            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            Debug.Log("Started Server at " + networkTransport.ConnectionData.Address + ". Make sure you opened port " + networkTransport.ConnectionData.Port + " for UDP traffic!");

            yield return new WaitUntil(() => NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub") | NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby"));

            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
            {
                yield return WebRequestManager.Singleton.ServerPostRequest(new WebRequestManager.ServerPostPayload(0, PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count,
                    1, networkTransport.ConnectionData.Address, "Hub", networkTransport.ConnectionData.Port.ToString()));
            }
            else if (NetSceneManager.Singleton.IsSceneGroupLoaded("Lobby"))
            {
                yield return WebRequestManager.Singleton.ServerPostRequest(new WebRequestManager.ServerPostPayload(1, PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count,
                    0, networkTransport.ConnectionData.Address, "Lobby", networkTransport.ConnectionData.Port.ToString()));
            }
            else
            {
                Debug.LogError("Not sure what scene group is loaded to create server");
            }

            Debug.Log("Finished Creating Server in API");
        }

        private void OnServerStopped(bool test)
        {
            Debug.Log("Stopped Server " + test);
        }

        private void OnClientStarted()
        {
            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            Debug.Log("Started Client at IP Address: " + networkTransport.ConnectionData.Address + " - Port: " + networkTransport.ConnectionData.Port + " - Payload: " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            StartCoroutine(ClientConnectTimeout());
        }

        private IEnumerator ClientConnectTimeout()
        {
            yield return new WaitForSeconds(clientConnectTimeoutThreshold);
            if (NetSceneManager.Singleton.IsSpawned) { yield break; }
            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(); }
            if (!NetSceneManager.Singleton.IsSceneGroupLoaded("Character Select")) { NetSceneManager.Singleton.LoadScene("Character Select"); }
            Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Could not connect to server.";
        }

        private void OnClientStopped(bool test)
        {
            Debug.Log("Stopped Client " + test);
        }

        private void OnTransportFailure()
        {
            Debug.Log("Transport failure at time: " + Time.time);
        }
    }
}