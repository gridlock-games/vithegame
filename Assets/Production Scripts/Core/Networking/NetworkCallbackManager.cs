using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Vi.Core.SceneManagement
{
    public class NetworkCallbackManager : MonoBehaviour
    {
        [SerializeField] private PlayerDataManager playerDataManagerPrefab;
        [SerializeField] private NetSceneManager networkSceneManagerPrefab;

        private void Awake()
        {
            //Screen.SetResolution(1920, 1080, Screen.fullScreenMode, Screen.currentResolution.refreshRate);
            //Application.targetFrameRate = Screen.currentResolution.refreshRate;
        }

        private void Start()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Instantiate(networkSceneManagerPrefab.gameObject);
            Instantiate(playerDataManagerPrefab.gameObject);
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
            Debug.Log("ClientId: " + clientId + " has been approved. Payload: " + payload);

            PlayerDataManager.Team clientTeam = SceneManager.GetSceneByName("Player Hub").isLoaded ? PlayerDataManager.Team.Peaceful : PlayerDataManager.Team.Competitor;

            StartCoroutine(AddPlayerData(payload, (int)clientId, clientTeam));
        }

        private IEnumerator AddPlayerData(string characterId, int clientId, PlayerDataManager.Team team)
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            WebRequestManager.Singleton.GetCharacterById(characterId);
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsGettingCharacterById);
            PlayerDataManager.Singleton.AddPlayerData(new PlayerDataManager.PlayerData(clientId, WebRequestManager.Singleton.CharacterById, team, 1, 2));
        }

        private void OnServerStarted()
        {
            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            Debug.Log("Started Server at " + networkTransport.ConnectionData.Address + ". Make sure you opened port " + networkTransport.ConnectionData.Port + " for UDP traffic!");
        }

        private void OnServerStopped(bool test)
        {
            Debug.Log("Stopped Server " + test);
        }

        private void OnClientStarted()
        {
            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            Debug.Log("Started Client at IP Address: " + networkTransport.ConnectionData.Address + " - Port: " + networkTransport.ConnectionData.Port + " - Payload: " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
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