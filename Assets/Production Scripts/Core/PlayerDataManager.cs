using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Vi.Core
{
    public class PlayerDataManager : NetworkBehaviour
    {
        [SerializeField] private GameMode gameModeValue;
        [SerializeField] private GameObject spectatorPrefab;
        [SerializeField] private CharacterReference characterReference;

        [SerializeField] private List<GameModeInfo> gameModeInfos;

        [System.Serializable]
        public struct GameModeInfo
        {
            public GameMode gameMode;
            public Team[] possibleTeams;
            //public string[] possibleMaps;
        }

        public CharacterReference GetCharacterReference() { return characterReference; }

        public GameModeInfo GetGameModeInfo() { return gameModeInfos.Find(item => item.gameMode == gameMode.Value); }

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

        public bool CanHit(Attributes attacker, Attributes victim) { return CanHit(GetPlayerData(attacker.GetPlayerDataId()).team, GetPlayerData(victim.GetPlayerDataId()).team); }

        public static Color GetTeamColor(Team team)
        {
            ColorUtility.TryParseHtmlString(team.ToString(), out Color color);
            return color;
        }

        public enum GameMode
        {
            None,
            FreeForAll,
            TeamElimination,
            EssenceWar,
            OutputRush
        }

        public enum Team
        {
            Environment,
            Spectator,
            Competitor,
            Red,
            Orange,
            Yellow,
            Green,
            Blue,
            Purple
        }

        private Dictionary<int, Attributes> localPlayers = new Dictionary<int, Attributes>();
        public void AddPlayerObject(int clientId, Attributes playerObject)
        {
            localPlayers.Add(clientId, playerObject);

            //// Remove empty player object references from local player object references
            //foreach (var item in localPlayers.Where(kvp => kvp.Value == null).ToList())
            //{
            //    localPlayers.Remove(item.Key);
            //}
        }

        public void RemovePlayerObject(int clientId)
        {
            localPlayers.Remove(clientId);
        }

        public List<Attributes> GetPlayersOnTeam(Team team, Attributes attributesToExclude = null)
        {
            List<Attributes> attributesList = new List<Attributes>();
            if (team == Team.Competitor) { return attributesList; }
            foreach (var kvp in localPlayers.Where(kvp => GetPlayerData(kvp.Value.GetPlayerDataId()).team == team))
            {
                if (kvp.Value == attributesToExclude) { continue; }
                attributesList.Add(kvp.Value);
            }
            return attributesList;
        }

        public List<Attributes> GetActivePlayers(Attributes attributesToExclude = null)
        {
            List<Attributes> attributesList = new List<Attributes>();
            foreach (var kvp in localPlayers.Where(kvp => GetPlayerData(kvp.Value.GetPlayerDataId()).team != Team.Spectator))
            {
                if (kvp.Value == attributesToExclude) { continue; }
                attributesList.Add(kvp.Value);
            }
            return attributesList;
        }

        public KeyValuePair<int, Attributes> GetLocalPlayer()
        {
            return localPlayers.First(kvp => kvp.Value.IsLocalPlayer);
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
                if (playerData.id == clientId)
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
                if (playerData.id == (int)clientId)
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

        public static PlayerDataManager Singleton { get { return _singleton; } }
        private static PlayerDataManager _singleton;

        public const char payloadParseString = '|';

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            playerDataList = new NetworkList<PlayerData>();
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private PlayerSpawnPoints playerSpawnPoints;
        void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            foreach (GameObject g in scene.GetRootGameObjects())
            {
                if (g.TryGetComponent(out playerSpawnPoints)) { break; }
            }
        }

        void OnSceneUnload(Scene scene)
        {
            playerSpawnPoints = null;
        }

        private void Start()
        {
            NetworkManager.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.OnClientConnectedCallback += OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        public override void OnNetworkSpawn()
        {
            playerDataList.OnListChanged += OnPlayerDataListChange;
            if (IsServer) { gameMode.Value = gameModeValue; }
        }

        public override void OnNetworkDespawn()
        {
            playerDataList.OnListChanged -= OnPlayerDataListChange;
        }

        private void OnPlayerDataListChange(NetworkListEvent<PlayerData> networkListEvent)
        {
            if (!IsServer) { return; }
            if (networkListEvent.Type == NetworkListEvent<PlayerData>.EventType.Add)
            {
                StartCoroutine(SpawnPlayer(networkListEvent.Value));
            }
        }

        private void OnClientConnectCallback(ulong clientId)
        {

        }

        public void RespawnPlayers()
        {
            playerSpawnPoints.ResetSpawnTracker();
            foreach (KeyValuePair<int, Attributes> kvp in localPlayers)
            {
                PlayerSpawnPoints.TransformData transformData = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, kvp.Value.GetTeam());
                Vector3 spawnPosition = transformData.position;
                Quaternion spawnRotation = transformData.rotation;

                kvp.Value.ResetStats(false);
                kvp.Value.GetComponent<AnimationHandler>().CancelAllActions();
                kvp.Value.GetComponent<MovementHandler>().SetOrientation(spawnPosition, spawnRotation);
            }
        }

        private IEnumerator SpawnPlayer(PlayerData playerData)
        {
            if (playerData.id >= 0) { yield return new WaitUntil(() => NetworkManager.ConnectedClientsIds.Contains((ulong)playerData.id)); }
            if (localPlayers.ContainsKey(playerData.id)) { yield break; }

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (playerSpawnPoints)
            {
                PlayerSpawnPoints.TransformData transformData = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team);
                spawnPosition = transformData.position;
                spawnRotation = transformData.rotation;
            }

            GameObject playerObject;
            if (GetPlayerData(playerData.id).team == Team.Spectator)
            {
                playerObject = Instantiate(spectatorPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                if (playerData.id >= 0)
                    playerObject = Instantiate(characterReference.GetPlayerModelOptions()[GetPlayerData(playerData.id).characterIndex].playerPrefab, spawnPosition, spawnRotation);
                else
                    playerObject = Instantiate(characterReference.GetPlayerModelOptions()[GetPlayerData(playerData.id).characterIndex].botPrefab, spawnPosition, spawnRotation);
            }

            playerObject.GetComponent<AnimationHandler>().SetCharacterSkin(playerData.characterIndex, playerData.skinIndex);
            playerObject.GetComponent<Attributes>().SetPlayerDataId(playerData.id);

            if (playerData.id >= 0)
                playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject((ulong)GetPlayerData(playerData.id).id, true);
            else
                playerObject.GetComponent<NetworkObject>().Spawn(true);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            RemovePlayerData((int)clientId);
        }

        public NetworkList<PlayerData> playerDataList;

        [System.Serializable]
        private struct TeamDefinition
        {
            public Team team;
            public ulong clientId;
        }

        [SerializeField] private TeamDefinition[] teamDefinitions;

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
            string[] payloadOptions = payload.Split(payloadParseString);

            string playerName = "Player Name";
            int characterIndex = 0;
            int skinIndex = 0;

            if (payloadOptions.Length > 0) { playerName = payloadOptions[0]; }
            if (payloadOptions.Length > 1) { int.TryParse(payloadOptions[1], out characterIndex); }
            if (payloadOptions.Length > 2) { int.TryParse(payloadOptions[2], out skinIndex); }

            Team clientTeam = Team.Competitor;

            foreach (TeamDefinition teamDefinition in teamDefinitions)
            {
                if (clientId == teamDefinition.clientId) { clientTeam = teamDefinition.team; }
            }

            if (clientId != NetworkManager.ServerClientId)
                playerDataList.Add(new PlayerData((int)clientId, playerName, characterIndex, skinIndex, clientTeam));
            else
                StartCoroutine(OnHostConnect(new PlayerData((int)clientId, playerName, characterIndex, skinIndex, clientTeam)));
        }

        private IEnumerator OnHostConnect(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            playerDataList.Add(playerData);
        }

        public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
        {
            public int id;
            public FixedString32Bytes playerName;
            public int characterIndex;
            public int skinIndex;
            public Team team;

            public PlayerData(int id)
            {
                this.id = id;
                playerName = "Player Name";
                characterIndex = 0;
                skinIndex = 0;
                team = Team.Environment;
            }

            public PlayerData(int id, string playerName, int characterIndex, int skinIndex, Team team)
            {
                this.id = id;
                this.playerName = playerName;
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
                this.team = team;
            }

            public PlayerData(ulong clientId, string playerName, int characterIndex, int skinIndex, Team team)
            {
                id = (int)clientId;
                this.playerName = playerName;
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
                this.team = team;
            }

            public bool Equals(PlayerData other)
            {
                return id == other.id;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeValue(ref playerName);
                serializer.SerializeValue(ref characterIndex);
                serializer.SerializeValue(ref skinIndex);
                serializer.SerializeValue(ref team);
            }
        }
    }
}