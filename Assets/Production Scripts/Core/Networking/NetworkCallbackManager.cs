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

        private void Start()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Instantiate(networkSceneManagerPrefab.gameObject);
            Instantiate(playerDataManagerPrefab.gameObject);
            NetSceneManager.Singleton.LoadScene("Main Menu");
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

            PlayerDataManager.ParsedConnectionData parsedConnectionData = PlayerDataManager.ParseConnectionData(connectionData);

            string payload = System.Text.Encoding.ASCII.GetString(connectionData);

            PlayerDataManager.Team clientTeam = PlayerDataManager.Team.Competitor;

            StartCoroutine(AddPlayerData(new PlayerDataManager.PlayerData((int)clientId, parsedConnectionData.playerName,
                parsedConnectionData.characterIndex,
                parsedConnectionData.skinIndex,
                clientTeam,
                0,
                1)));
        }

        private IEnumerator AddPlayerData(PlayerDataManager.PlayerData playerData)
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            PlayerDataManager.Singleton.AddPlayerData(playerData);
        }
    }
}