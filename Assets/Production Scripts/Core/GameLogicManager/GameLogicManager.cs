using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace Vi.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        public enum GameMode
        {
            Duel,
            TeamElimination,
            TeamDeathmatch
        }

        public enum Team
        {
            Environment,
            Spectator,
            Competitor,
            Red,
            Blue,
        }

        public static GameLogicManager Singleton { get { return _singleton; } }
        protected static GameLogicManager _singleton;

        public const char payloadParseString = '|';

        protected void Awake()
        {
            _singleton = this;
            playerDataList = new NetworkList<PlayerData>();
        }

        protected void Start()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        protected NetworkList<PlayerData> playerDataList;

        protected void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // The client identifier to be authenticated
            var clientId = request.ClientNetworkId;

            // Additional connection data defined by user code
            var connectionData = request.Payload;

            // Your approval logic determines the following values
            response.Approved = true;
            response.CreatePlayerObject = true;

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
            string[] payloadOptions = payload.Split(payloadParseString);

            string playerName = "Player Name";
            int characterIndex = 0;
            int skinIndex = 0;

            if (payloadOptions.Length > 0) { playerName = payloadOptions[0]; }
            if (payloadOptions.Length > 1) { int.TryParse(payloadOptions[1], out characterIndex); }
            if (payloadOptions.Length > 2) { int.TryParse(payloadOptions[2], out skinIndex); }

            if (clientId != NetworkManager.ServerClientId)
                playerDataList.Add(new PlayerData(clientId, playerName, characterIndex, skinIndex));
            else
                StartCoroutine(OnHostConnect(new PlayerData(clientId, playerName, characterIndex, skinIndex)));
        }

        protected IEnumerator OnHostConnect(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            playerDataList.Add(playerData);
        }

        public PlayerData GetPlayerData(ulong clientId)
        {
            foreach (PlayerData playerData in playerDataList)
            {
                if (playerData.clientId == clientId)
                {
                    return playerData;
                }
            }
            Debug.LogError("Could not find player data with ID: " + clientId);
            return new PlayerData();
        }

        public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
        {
            public ulong clientId;
            public FixedString32Bytes playerName;
            public int characterIndex;
            public int skinIndex;

            public PlayerData(ulong clientId, string playerName, int characterIndex, int skinIndex)
            {
                this.clientId = clientId;
                this.playerName = playerName;
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
            }

            public bool Equals(PlayerData other)
            {
                return clientId == other.clientId & playerName == other.playerName & characterIndex == other.characterIndex & skinIndex == other.skinIndex;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref clientId);
                serializer.SerializeValue(ref playerName);
                serializer.SerializeValue(ref characterIndex);
                serializer.SerializeValue(ref skinIndex);
            }
        }
    }
}