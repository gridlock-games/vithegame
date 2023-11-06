using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        [SerializeField] protected CharacterReference characterReference;

        public CharacterReference GetCharacterReference() { return characterReference; }

        private NetworkVariable<GameMode> gameMode = new NetworkVariable<GameMode>();
        public GameMode GetGameMode() { return gameMode.Value; }

        public static bool CanHit(Team attackerTeam, Team victimTeam)
        {
            if (attackerTeam != Team.Competitor & victimTeam != Team.Competitor)
            {
                if (attackerTeam == victimTeam) { return false; }
            }
            return true;
        }

        public static Color GetTeamColor(Team team)
        {
            if (team == Team.Red)
            {
                return Color.red;
            }
            else if (team == Team.Blue)
            {
                return Color.blue;
            }
            else
            {
                return Color.black;
            }
        }

        public enum GameMode
        {
            Duel,
            TeamElimination
        }

        public enum Team
        {
            Environment,
            Spectator,
            Competitor,
            Red,
            Blue,
        }

        private Dictionary<ulong, GameObject> localPlayers = new Dictionary<ulong, GameObject>();
        public void AddPlayerObject(ulong clientId, GameObject playerObject)
        {
            localPlayers.Add(clientId, playerObject);

            //// Remove empty player object references from local player object references
            //foreach (var item in localPlayers.Where(kvp => kvp.Value == null).ToList())
            //{
            //    localPlayers.Remove(item.Key);
            //}
        }

        public bool ContainsId(int clientId) { return playerDataList.Contains(new PlayerData(clientId)); }

        private int botClientId = 0;
        public int AddBotData(int characterIndex, int skinIndex, Team team)
        {
            if (IsSpawned) { if (!IsServer) { Debug.LogError("GameLogicManager.AddBotData() should only be called on the server!"); return 0; } }

            botClientId--;
            PlayerData botData = new PlayerData(botClientId, "Bot " + (botClientId*-1).ToString(), characterIndex, skinIndex, team);

            if (IsSpawned)
                playerDataList.Add(botData);
            else
                StartCoroutine(WaitForSpawnToAddPlayerData(botData));

            return botClientId;
        }

        private IEnumerator WaitForSpawnToAddPlayerData(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            playerDataList.Add(playerData);
        }

        public PlayerData GetPlayerData(int clientId)
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

        public PlayerData GetPlayerData(ulong clientId)
        {
            foreach (PlayerData playerData in playerDataList)
            {
                if (playerData.clientId == (int)clientId)
                {
                    return playerData;
                }
            }
            Debug.LogError("Could not find player data with ID: " + clientId);
            return new PlayerData();
        }

        public void SetPlayerData(PlayerData playerData)
        {
            if (IsServer)
            {
                int index = playerDataList.IndexOf(playerData);
                if (index == -1) { return; }
                playerDataList[index] = playerData;
            }
            else
            {
                SetPlayerDataServerRpc(playerData);
            }
        }

        public void RemovePlayerData(int clientId)
        {
            playerDataList.Remove(new PlayerData(clientId));
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerDataServerRpc(PlayerData playerData) { SetPlayerData(playerData); }

        public static GameLogicManager Singleton { get { return _singleton; } }
        protected static GameLogicManager _singleton;

        public const char payloadParseString = '|';

        protected void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            playerDataList = new NetworkList<PlayerData>();
        }

        protected void Start()
        {
            NetworkManager.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.OnClientConnectedCallback += OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        public override void OnNetworkSpawn()
        {
            playerDataList.OnListChanged += OnPlayerDataListChange;
        }

        public override void OnNetworkDespawn()
        {
            playerDataList.OnListChanged -= OnPlayerDataListChange;
        }

        protected void OnPlayerDataListChange(NetworkListEvent<PlayerData> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<PlayerData>.EventType.Add)
            {
                //localPlayers[networkListEvent.Value.clientId].GetComponent<AnimationHandler>().SetCharacterSkin(networkListEvent.Value.characterIndex, networkListEvent.Value.skinIndex);
            }
        }

        protected void OnClientConnectCallback(ulong clientId)
        {
            if (IsServer) { StartCoroutine(SpawnPlayer(clientId)); }
        }

        private IEnumerator SpawnPlayer(ulong clientId)
        {
            yield return new WaitUntil(() => playerDataList.Contains(new PlayerData((int)clientId)));

            GameObject playerObject = Instantiate(characterReference.GetPlayerModelOptions()[GetPlayerData((int)clientId).characterIndex].playerPrefab);
            playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject((ulong)GetPlayerData((int)clientId).clientId, true);
        }

        protected void OnClientDisconnectCallback(ulong clientId)
        {
            RemovePlayerData((int)clientId);
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
            string[] payloadOptions = payload.Split(payloadParseString);

            string playerName = "Player Name";
            int characterIndex = 0;
            int skinIndex = 0;

            if (payloadOptions.Length > 0) { playerName = payloadOptions[0]; }
            if (payloadOptions.Length > 1) { int.TryParse(payloadOptions[1], out characterIndex); }
            if (payloadOptions.Length > 2) { int.TryParse(payloadOptions[2], out skinIndex); }

            if (clientId != NetworkManager.ServerClientId)
                playerDataList.Add(new PlayerData((int)clientId, playerName, characterIndex, skinIndex, Team.Competitor));
            else
                StartCoroutine(OnHostConnect(new PlayerData((int)clientId, playerName, characterIndex, skinIndex, Team.Competitor)));
        }

        protected IEnumerator OnHostConnect(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            playerDataList.Add(playerData);
        }

        public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
        {
            public int clientId;
            public FixedString32Bytes playerName;
            public int characterIndex;
            public int skinIndex;
            public Team team;

            public PlayerData(int clientId)
            {
                this.clientId = clientId;
                playerName = "Player Name";
                characterIndex = 0;
                skinIndex = 0;
                team = Team.Environment;
            }

            public PlayerData(int clientId, string playerName, int characterIndex, int skinIndex, Team team)
            {
                this.clientId = clientId;
                this.playerName = playerName;
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
                this.team = team;
            }

            public PlayerData(ulong clientId, string playerName, int characterIndex, int skinIndex, Team team)
            {
                this.clientId = (int)clientId;
                this.playerName = playerName;
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
                this.team = team;
            }

            public bool Equals(PlayerData other)
            {
                return clientId == other.clientId;
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