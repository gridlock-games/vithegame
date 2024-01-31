using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.SceneManagement;
using Vi.Core.GameModeManagers;

namespace Vi.Core
{
    public class PlayerDataManager : NetworkBehaviour
    {
        [SerializeField] private GameObject spectatorPrefab;
        [SerializeField] private CharacterReference characterReference;

        [SerializeField] private List<GameModeInfo> gameModeInfos;

        [System.Serializable]
        public struct GameModeInfo
        {
            public GameMode gameMode;
            public Team[] possibleTeams;
            public string[] possibleMapSceneGroupNames;
        }

        public CharacterReference GetCharacterReference() { return characterReference; }

        public GameModeInfo GetGameModeInfo() { return gameModeInfos.Find(item => item.gameMode == gameMode.Value); }

        private NetworkVariable<GameMode> gameMode = new NetworkVariable<GameMode>();
        public GameMode GetGameMode() { return gameMode.Value; }

        public void SetGameMode(GameMode newGameMode)
        {
            if (IsServer)
            {
                gameMode.Value = newGameMode;
            }
            else
            {
                SetGameModeServerRpc(newGameMode);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetGameModeServerRpc(GameMode newGameMode)
        {
            SetGameMode(newGameMode);
        }

        private void OnGameModeChange(GameMode prev, GameMode current)
        {
            mapIndex.Value = 0;
        }

        private NetworkVariable<int> mapIndex = new NetworkVariable<int>();
        public string GetMapName()
        {
            return GetGameModeInfo().possibleMapSceneGroupNames[mapIndex.Value];
        }

        public void SetMap(string map)
        {
            if (IsServer)
            {
                mapIndex.Value = System.Array.IndexOf(GetGameModeInfo().possibleMapSceneGroupNames, map);
            }
            else
            {
                SetMapServerRpc(map);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetMapServerRpc(string map)
        {
            SetMap(map);
        }

        public bool IsLobbyLeader()
        {
            if (IsServer) { return true; }

            List<PlayerData> playerDataList = GetPlayerDataList();
            playerDataList.RemoveAll(item => item.id < 0);
            playerDataList = playerDataList.OrderBy(item => item.id).ToList();

            if (playerDataList.Count > 0)
                return playerDataList[0].id == (int)NetworkManager.LocalClientId;
            else
                return false;
        }

        public PlayerData GetLobbyLeader()
        {
            List<PlayerData> playerDataList = GetPlayerDataList();
            playerDataList.RemoveAll(item => item.id < 0);
            playerDataList = playerDataList.OrderBy(item => item.id).ToList();

            if (playerDataList.Count > 0)
                return playerDataList[0];
            else
                return new PlayerData();
        }

        public static bool CanHit(Team attackerTeam, Team victimTeam)
        {
            if (attackerTeam == Team.Peaceful) { return false; }

            if (attackerTeam != Team.Competitor & victimTeam != Team.Competitor)
            {
                if (attackerTeam == victimTeam) { return false; }
            }
            return true;
        }

        public bool CanHit(Attributes attacker, Attributes victim) { return CanHit(GetPlayerData(attacker.GetPlayerDataId()).team, GetPlayerData(victim.GetPlayerDataId()).team) & attacker != victim; }

        public static Color GetTeamColor(Team team)
        {
            if (ColorUtility.TryParseHtmlString(team.ToString(), out Color color))
            {
                return color;
            }
            else
            {
                return Color.black;
            }
        }

        public static string GetTeamText(Team team)
        {
            switch (team)
            {
                case Team.Environment:
                case Team.Peaceful:
                    return team.ToString();
                case Team.Competitor:
                    return "Competitors";
                default:
                    return team.ToString() + " Team";
            }
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
            Purple,
            Peaceful
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

        public List<Attributes> GetPlayerObjectsOnTeam(Team team, Attributes attributesToExclude = null)
        {
            List<Attributes> attributesList = new List<Attributes>();
            if (team == Team.Competitor | team == Team.Peaceful) { return attributesList; }
            foreach (var kvp in localPlayers.Where(kvp => GetPlayerData(kvp.Value.GetPlayerDataId()).team == team))
            {
                if (kvp.Value == attributesToExclude) { continue; }
                attributesList.Add(kvp.Value);
            }
            return attributesList;
        }

        public List<Attributes> GetActivePlayerObjects(Attributes attributesToExclude = null)
        {
            List<Attributes> attributesList = new List<Attributes>();
            foreach (var kvp in localPlayers.Where(kvp => GetPlayerData(kvp.Value.GetPlayerDataId()).team != Team.Spectator))
            {
                if (kvp.Value == attributesToExclude) { continue; }
                attributesList.Add(kvp.Value);
            }
            return attributesList;
        }

        public KeyValuePair<int, Attributes> GetLocalPlayerObject()
        {
            try
            {
                return localPlayers.First(kvp => kvp.Value.IsLocalPlayer);
            }
            catch
            {
                return new KeyValuePair<int, Attributes>((int)NetworkManager.LocalClientId, null);
            }
        }

        public bool ContainsId(int clientId) { return playerDataList.Contains(new PlayerData(clientId)); }

        private int botClientId = 0;
        public void AddBotData(Team team)
        {
            if (IsServer)
            {
                botClientId--;

                WebRequestManager.Character botCharacter = WebRequestManager.Singleton.GetDefaultCharacter();
                botCharacter.name = "Bot " + (botClientId * -1).ToString();

                PlayerData botData = new PlayerData(botClientId,
                    botCharacter,
                    team);
                AddPlayerData(botData);
            }
            else
            {
                AddBotDataServerRpc(team);
            }
        }

        [ServerRpc(RequireOwnership = false)] private void AddBotDataServerRpc(Team team) { AddBotData(team); }

        public void AddPlayerData(PlayerData playerData)
        {
            if (!IsSpawned)
            {
                StartCoroutine(WaitForSpawnToAddPlayerData(playerData));
            }
            else
            {
                playerDataList.Add(playerData);
                if (GameModeManager.Singleton) { GameModeManager.Singleton.AddPlayerScore(playerData.id); }
            }
        }

        private IEnumerator WaitForSpawnToAddPlayerData(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            AddPlayerData(playerData);
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
            if (GameModeManager.Singleton) { GameModeManager.Singleton.RemovePlayerScore(clientId); }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerDataServerRpc(PlayerData playerData) { SetPlayerData(playerData); }

        public static PlayerDataManager Singleton { get { return _singleton; } }
        private static PlayerDataManager _singleton;

        private void Awake()
        {
            _singleton = this;
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

            if (IsServer)
            {
                if (NetSceneManager.Singleton.ShouldSpawnPlayer())
                {
                    foreach (PlayerData playerData in playerDataList)
                    {
                        StartCoroutine(SpawnPlayer(playerData));
                    }
                }
            }
        }

        void OnSceneUnload(Scene scene)
        {
            playerSpawnPoints = null;

            if (IsServer)
            {
                if (!NetSceneManager.Singleton.ShouldSpawnPlayer())
                {
                    foreach (Attributes attributes in GetActivePlayerObjects())
                    {
                        attributes.NetworkObject.Despawn(true);
                    }
                }
            }
        }

        private void Start()
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        public override void OnNetworkSpawn()
        {
            playerDataList.OnListChanged += OnPlayerDataListChange;
            gameMode.OnValueChanged += OnGameModeChange;
            NetworkManager.NetworkTickSystem.Tick += Tick;
        }

        public override void OnNetworkDespawn()
        {
            playerDataList.OnListChanged -= OnPlayerDataListChange;
            gameMode.OnValueChanged -= OnGameModeChange;
            NetworkManager.NetworkTickSystem.Tick -= Tick;

            playerDataList.Clear();
            localPlayers.Clear();
            botClientId = 0;
        }

        private void Tick()
        {
            if (playersToSpawnQueue.Count > 0)
            {
                StartCoroutine(SpawnPlayer(playersToSpawnQueue.Dequeue()));
            }
        }

        private Queue<PlayerData> playersToSpawnQueue = new Queue<PlayerData>();
        private void OnPlayerDataListChange(NetworkListEvent<PlayerData> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<PlayerData>.EventType.Add)
            {
                Debug.Log("Id: " + networkListEvent.Value.id + " - Name: " + networkListEvent.Value.character.name + "'s data has been added.");
                if (IsServer)
                {
                    if (NetSceneManager.Singleton.ShouldSpawnPlayer())
                    {
                        playersToSpawnQueue.Enqueue(networkListEvent.Value);
                    }
                    StartCoroutine(WebRequestManager.Singleton.UpdateServerPopulation(playerDataList.Count, GetLobbyLeader().character.name.ToString()));
                }
            }
            else if (networkListEvent.Type == NetworkListEvent<PlayerData>.EventType.Remove | networkListEvent.Type == NetworkListEvent<PlayerData>.EventType.RemoveAt)
            {
                if (IsServer)
                {
                    StartCoroutine(WebRequestManager.Singleton.UpdateServerPopulation(playerDataList.Count, GetLobbyLeader().character.name.ToString()));
                }
            }
        }

        private void OnClientConnectCallback(ulong clientId)
        {
            Debug.Log("Id: " + clientId + " has connected.");
        }

        public void RespawnPlayer(Attributes attributesToRespawn, bool findMostPeacefulSpawnPosition)
        {
            PlayerSpawnPoints.TransformData transformData = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, attributesToRespawn.GetTeam());
            Vector3 spawnPosition = transformData.position;
            Quaternion spawnRotation = transformData.rotation;

            attributesToRespawn.ResetStats(false);
            attributesToRespawn.GetComponent<AnimationHandler>().CancelAllActions();
            attributesToRespawn.GetComponent<MovementHandler>().SetOrientation(spawnPosition, spawnRotation);
        }

        public void RespawnAllPlayers()
        {
            foreach (KeyValuePair<int, Attributes> kvp in localPlayers)
            {
                RespawnPlayer(kvp.Value, false);
            }
        }

        public void SetAllPlayersMobility(bool canMove)
        {
            foreach (KeyValuePair<int, Attributes> kvp in localPlayers)
            {
                kvp.Value.GetComponent<MovementHandler>().SetCanMove(canMove);
            }
        }

        private IEnumerator SpawnPlayer(PlayerData playerData)
        {
            if (playerData.id >= 0)
            {
                yield return new WaitUntil(() => NetworkManager.ConnectedClientsIds.Contains((ulong)playerData.id));
                Debug.Log("INVENTORY IS FOUND: " + WebRequestManager.Singleton.InventoryItems.ContainsKey(playerData.character._id.ToString()));
            }
            if (localPlayers.ContainsKey(playerData.id)) { Debug.LogError("Calling SpawnPlayer() while there is an entry for this local player already! Id: " + playerData.id); yield break; }

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (playerSpawnPoints)
            {
                PlayerSpawnPoints.TransformData transformData = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team);
                spawnPosition = transformData.position;
                spawnRotation = transformData.rotation;
            }

            KeyValuePair<int, int> kvp = Singleton.GetCharacterReference().GetPlayerModelOptionIndices(playerData.character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            GameObject playerObject;
            if (GetPlayerData(playerData.id).team == Team.Spectator)
            {
                playerObject = Instantiate(spectatorPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                if (playerData.id >= 0)
                    playerObject = Instantiate(characterReference.GetPlayerModelOptions()[characterIndex].playerPrefab, spawnPosition, spawnRotation);
                else
                    playerObject = Instantiate(characterReference.GetPlayerModelOptions()[characterIndex].botPrefab, spawnPosition, spawnRotation);
            }

            playerObject.GetComponent<Attributes>().SetPlayerDataId(playerData.id);

            if (playerData.id >= 0)
                playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject((ulong)GetPlayerData(playerData.id).id, true);
            else
                playerObject.GetComponent<NetworkObject>().Spawn(true);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log("Id: " + clientId + " - Name: " + GetPlayerData(clientId).character.name + " has disconnected.");
            if (IsServer) { RemovePlayerData((int)clientId); }
        }

        public List<PlayerData> GetPlayerDataList()
        {
            List<PlayerData> playerDatas = new List<PlayerData>();
            foreach (PlayerData playerData in playerDataList)
            {
                playerDatas.Add(playerData);
            }
            return playerDatas;
        }

        private NetworkList<PlayerData> playerDataList;

        [System.Serializable]
        private struct TeamDefinition
        {
            public Team team;
            public ulong clientId;
        }

        public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
        {
            public int id;
            public WebRequestManager.Character character;
            public Team team;

            public PlayerData(int id)
            {
                this.id = id;
                character = new();
                team = Team.Environment;
            }

            public PlayerData(int id, WebRequestManager.Character character, Team team)
            {
                this.id = id;
                this.character = character;
                this.team = team;
            }

            public bool Equals(PlayerData other)
            {
                return id == other.id;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeNetworkSerializable(ref character);
                serializer.SerializeValue(ref team);
            }
        }
    }
}