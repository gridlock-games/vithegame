using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.SceneManagement;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;
using Vi.Utility;
using Newtonsoft.Json;

namespace Vi.Core
{
    public class PlayerDataManager : NetworkBehaviour
    {
        [SerializeField] private GameObject spectatorPrefab;
        [SerializeField] private CharacterReference characterReference;
        [SerializeField] private ControlsImageMapping controlsImageMapping;

        [SerializeField] private List<GameModeInfo> gameModeInfos;

        [System.Serializable]
        public struct GameModeInfo
        {
            public GameMode gameMode;
            public Sprite gameModeIcon;
            public Team[] possibleTeams;
            public string[] possibleMapSceneGroupNames;
        }

        public CharacterReference GetCharacterReference() { return characterReference; }

        public ControlsImageMapping GetControlsImageMapping() { return controlsImageMapping; }

        public GameModeInfo GetGameModeInfo() { return gameModeInfos.Find(item => item.gameMode == gameMode.Value); }

        public Sprite GetGameModeIcon(GameMode gameMode) { return gameModeInfos.Find(item => item.gameMode == gameMode).gameModeIcon; }

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

        [Rpc(SendTo.Server, RequireOwnership = false)]
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetMapServerRpc(string map)
        {
            SetMap(map);
        }

        private NetworkVariable<FixedString512Bytes> gameModeSettings = new NetworkVariable<FixedString512Bytes>();

        public string GetGameModeSettings() { return gameModeSettings.Value.ToString(); }

        public void SetGameModeSettings(string gameModeSettings)
        {
            if (gameModeSettings == null) { Debug.LogError("Trying to set game mode settings to be null!"); return; }
            if (gameModeSettings == this.gameModeSettings.Value.ToString()) { return; }

            if (IsServer)
            {
                this.gameModeSettings.Value = gameModeSettings;
            }
            else
            {
                SetGameModeSettingsServerRpc(gameModeSettings);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetGameModeSettingsServerRpc(string gameModeSettings)
        {
            SetGameModeSettings(gameModeSettings);
        }

        public bool IsLobbyLeader()
        {
            List<PlayerData> playerDataList = GetPlayerDataListWithSpectators();
            playerDataList.RemoveAll(item => item.id < 0);
            playerDataList = playerDataList.OrderBy(item => item.id).ToList();

            if (playerDataList.Count > 0)
                return playerDataList[0].id == (int)NetworkManager.LocalClientId;
            else
                return IsServer;
        }

        public KeyValuePair<bool, PlayerData> GetLobbyLeader()
        {
            List<PlayerData> playerDataList = GetPlayerDataListWithSpectators();
            playerDataList.RemoveAll(item => item.id < 0);
            playerDataList = playerDataList.OrderBy(item => item.id).ToList();

            if (playerDataList.Count > 0)
                return new KeyValuePair<bool, PlayerData>(true, playerDataList[0]);
            else
                return new KeyValuePair<bool, PlayerData>(false, new PlayerData());
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

        public bool CanHit(Attributes attacker, Attributes victim)
        {
            if (!attacker) { return false; }
            if (!victim) { return false; }
            return CanHit(GetPlayerData(attacker.GetPlayerDataId()).team, GetPlayerData(victim.GetPlayerDataId()).team) & attacker != victim;
        }

        private readonly static Dictionary<Team, Color> teamColors = new Dictionary<Team, Color>()
        {
            { Team.Competitor, new Color(65 / 255f, 65 / 255f, 65 / 255f, 1) },
            { Team.Red, Color.red },
            { Team.Orange, new Color(239 / (float)255, 91 / (float)255, 37 / (float)255) },
            { Team.Yellow, Color.yellow },
            { Team.Green, Color.green },
            { Team.Blue, Color.blue },
            { Team.Purple, Color.magenta },
            { Team.Peaceful, new Color(65 / 255f, 65 / 255f, 65 / 255f, 1) }
        };

        public static Color GetTeamColor(Team team)
        {
            if (teamColors.ContainsKey(team))
            {
                return teamColors[team];
            }
            else
            {
                return Color.black;
            }
        }

        private NetworkVariable<FixedString512Bytes> teamNameOverridesJson = new NetworkVariable<FixedString512Bytes>();

        private Dictionary<Team, TeamNameOverride> teamNameOverrides = new Dictionary<Team, TeamNameOverride>();

        private struct TeamNameOverride
        {
            public string teamName;
            public string prefix;

            public TeamNameOverride(string teamName, string prefix)
            {
                this.teamName = teamName;
                this.prefix = prefix;
            }
        }

        public void SetTeamNameOverride(Team team, string teamName, string prefix)
        {
            if (IsServer)
            {
                if (teamNameOverrides.ContainsKey(team))
                {
                    teamNameOverrides[team] = new TeamNameOverride(teamName, prefix);
                    if (string.IsNullOrWhiteSpace(teamName)) { teamNameOverrides.Remove(team); }
                }
                else
                {
                    teamNameOverrides.Add(team, new TeamNameOverride(teamName, prefix));
                    if (string.IsNullOrWhiteSpace(teamName)) { teamNameOverrides.Remove(team); }
                }
                string stringToAssign = JsonConvert.SerializeObject(teamNameOverrides);
                teamNameOverridesJson.Value = stringToAssign ?? "";
            }
            else
            {
                SetTeamNameOverrideServerRpc(team, teamName, prefix);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)] private void SetTeamNameOverrideServerRpc(Team team, string teamName, string prefix) { SetTeamNameOverride(team, teamName, prefix); }

        public string GetTeamText(Team team)
        {
            Dictionary<Team, TeamNameOverride> teamNameOverrides = JsonConvert.DeserializeObject<Dictionary<Team, TeamNameOverride>>(teamNameOverridesJson.Value.ToString());
            if (teamNameOverrides != null)
            {
                if (teamNameOverrides.ContainsKey(team)) { return teamNameOverrides[team].teamName; }
            }

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

        public string GetTeamPrefix(Team team)
        {
            Dictionary<Team, TeamNameOverride> teamNameOverrides = JsonConvert.DeserializeObject<Dictionary<Team, TeamNameOverride>>(teamNameOverridesJson.Value.ToString());
            if (teamNameOverrides != null)
            {
                if (teamNameOverrides.ContainsKey(team))
                {
                    if (string.IsNullOrWhiteSpace(teamNameOverrides[team].prefix))
                        return "";
                    else
                        return teamNameOverrides[team].prefix + " | ";
                }
            }
            return "";
        }

        public string GetTeamPrefixRaw(Team team)
        {
            Dictionary<Team, TeamNameOverride> teamNameOverrides = JsonConvert.DeserializeObject<Dictionary<Team, TeamNameOverride>>(teamNameOverridesJson.Value.ToString());
            if (teamNameOverrides != null)
            {
                if (teamNameOverrides.ContainsKey(team)) { return teamNameOverrides[team].prefix; }
            }
            return "";
        }

        public enum GameMode
        {
            None,
            FreeForAll,
            TeamElimination,
            EssenceWar,
            OutpostRush,
            TeamDeathmatch
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

        public bool LocalPlayersWasUpdatedThisFrame { get; private set; } = false;
        private Dictionary<int, Attributes> localPlayers = new Dictionary<int, Attributes>();
        public void AddPlayerObject(int clientId, Attributes playerObject)
        {
            localPlayers.Add(clientId, playerObject);
            LocalPlayersWasUpdatedThisFrame = true;

            if (resetLocalPlayerBoolCoroutine != null) { StopCoroutine(resetLocalPlayerBoolCoroutine); }
            resetLocalPlayerBoolCoroutine = StartCoroutine(ResetLocalPlayersWasUpdatedBool());

            playerObject.SetCachedPlayerData(Singleton.GetPlayerData(playerObject.GetPlayerDataId()));
        }

        public void RemovePlayerObject(int clientId)
        {
            localPlayers.Remove(clientId);
            LocalPlayersWasUpdatedThisFrame = true;

            if (resetLocalPlayerBoolCoroutine != null) { StopCoroutine(resetLocalPlayerBoolCoroutine); }
            resetLocalPlayerBoolCoroutine = StartCoroutine(ResetLocalPlayersWasUpdatedBool());
        }

        private Coroutine resetLocalPlayerBoolCoroutine;
        private IEnumerator ResetLocalPlayersWasUpdatedBool()
        {
            yield return null;
            LocalPlayersWasUpdatedThisFrame = false;
        }

        public List<Attributes> GetPlayerObjectsOnTeam(Team team, Attributes attributesToExclude = null)
        {
            // If the attributes to exclude is on competitor or peaceful teams, we don't want to return any teammates for this attributes
            if (attributesToExclude)
            {
                if (attributesToExclude.GetTeam() == Team.Competitor | attributesToExclude.GetTeam() == Team.Peaceful) { return new List<Attributes>(); }
            }
            return localPlayers.Where(kvp => kvp.Value.CachedPlayerData.team == team & kvp.Value != attributesToExclude).Select(kvp => kvp.Value).ToList();
        }

        public List<Attributes> GetActivePlayerObjects(Attributes attributesToExclude = null)
        {
            return localPlayers.Where(kvp => kvp.Value.CachedPlayerData.team != Team.Spectator & kvp.Value != attributesToExclude).Select(kvp => kvp.Value).ToList();
        }

        public Attributes GetPlayerObjectById(int id)
        {
            if (!localPlayers.ContainsKey(id)) { Debug.LogError("No player object for Id: " + id); return null; }
            return localPlayers[id];
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

        public bool IdHasLocalPlayer(int clientId) { return localPlayers.ContainsKey(clientId); }

        public bool ContainsId(int clientId)
        {
            return cachedPlayerDataList.Contains(new PlayerData(clientId));
        }

        public bool ContainsDisconnectedPlayerData(int clientId)
        {
            foreach (DisconnectedPlayerData disconnectedPlayerData in disconnectedPlayerDataList)
            {
                if (disconnectedPlayerData.playerData.id == clientId) { return true; }
            }
            return false;
        }

        private static readonly Dictionary<CharacterReference.RaceAndGender, List<string>> botNames = new Dictionary<CharacterReference.RaceAndGender, List<string>>()
        {
            { CharacterReference.RaceAndGender.HumanMale, new List<string>()
                {
                    "Omar",
                    "Ahmed",
                    "Tom",
                    "Justin",
                    "David",
                    "Adam",
                    "Tyler",
                    "James",
                    "John",
                    "Michael",
                    "Liam",
                    "Oliver",
                    "Ren",
                    "Haruto",
                    "Yuto",
                    "Miguel",
                    "Arthur",
                    "Aarav",
                    "Alexander",
                    "Wei",
                    "Min",
                    "Jun",
                    ""
                }
            },
            { CharacterReference.RaceAndGender.HumanFemale, new List<string>()
                {
                    "Rebecca",
                    "Irene",
                    "Farin",
                    "Maria",
                    "Lin",
                    "Sofia",
                    "Hanna",
                    "Emma",
                    "Julia",
                    "Olivia",
                    "Anna",
                    "Mary",
                    "Yui",
                    "Sakura",
                    "Akari",
                    "Emilia",
                    "Saanvi",
                    "Xiao",
                    "Yi",
                    "Jia"
                }
            },
            { CharacterReference.RaceAndGender.Universal, new List<string>()
                {
                    "BlazeX",
                    "FrostyZ",
                    "NovaKid",
                    "ThunderZ",
                    "FlameX",
                    "ViperX",
                    "WraithZ",
                    "MysticZ",
                    "NinjaKid",
                    "TitanZ",
                    "GhostX",
                    "LunaZ",
                    "DragonZ",
                    "QuakeX",
                    "StormZ",
                    "PulseZ",
                    "BlazeZ",
                    "FlameY",
                    "CosmoZ",
                    "PhoenixZ",
                    "SamuZ",
                    "RageX",
                    "HunterX",
                    "EnigmaX",
                    "SpartanX",
                    "FangX",
                    "GGuard",
                    "NinjaX",
                    "SaviorX",
                    "BlastX",
                    "NeonZ",
                    "TitanX",
                    "SorcX",
                    "EchoX",
                    "ShogunX",
                    "FireX",
                    "Nemesis",
                    "Tempest",
                    "FuryX",
                    "LanceX",
                    "Inferno",
                    "QuasarX",
                    "Icebound",
                    "StormX",
                    "EchoZ",
                    "ChampX",
                    "DoomZ",
                    "Striker",
                    "ThornX",
                    "EnigmaY"
                }
            }
        };

        private int botClientId = 0;
        public void AddBotData(Team team, bool useDefaultPrimaryWeapon, int limitTotalNumberOfPlayersOnTeam = -1)
        {
            if (team == Team.Spectator) { Debug.LogError("Trying to add a bot as a spectator!"); return; }

            if (IsServer)
            {
                if (limitTotalNumberOfPlayersOnTeam > -1)
                {
                    if (GetPlayerDataListWithoutSpectators().Where(item => item.team == team).ToArray().Length >= limitTotalNumberOfPlayersOnTeam) { return; }
                }

                botClientId--;

                WebRequestManager.Character botCharacter = WebRequestManager.Singleton.GetRandomizedCharacter(useDefaultPrimaryWeapon);

                List<string> potentialNames = botNames[botCharacter.raceAndGender];
                potentialNames.AddRange(botNames[CharacterReference.RaceAndGender.Universal]);
                botCharacter.name = potentialNames[Random.Range(0, potentialNames.Count)];
                if (string.IsNullOrWhiteSpace(botCharacter.name.ToString())) { botCharacter.name = "Bot"; Debug.LogError("Bot " + botClientId + " name is empty!"); }

                PlayerData botData = new PlayerData(botClientId,
                    botCharacter,
                    team);
                AddPlayerData(botData);
            }
            else
            {
                AddBotDataServerRpc(team, useDefaultPrimaryWeapon, limitTotalNumberOfPlayersOnTeam);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)] private void AddBotDataServerRpc(Team team, bool useDefaultPrimaryWeapon, int limitTotalNumberOfPlayersOnTeam) { AddBotData(team, useDefaultPrimaryWeapon, limitTotalNumberOfPlayersOnTeam); }

        public void AddPlayerData(PlayerData playerData)
        {
            if (!IsSpawned)
            {
                StartCoroutine(WaitForSpawnToAddPlayerData(playerData));
            }
            else
            {
                if (playerDataList.Contains(playerData)) { Debug.LogError("Player score with id: " + playerData.id + " has already been added!"); return; }

                int index = disconnectedPlayerDataList.IndexOf(new DisconnectedPlayerData(playerData));
                if (index == -1)
                {
                    playerDataList.Add(playerData);
                }
                else
                {
                    playerData.team = disconnectedPlayerDataList[index].playerData.team;
                    playerDataList.Add(playerData);
                    disconnectedPlayerDataList.RemoveAt(index);
                }
                
                if (GameModeManager.Singleton & playerData.team != Team.Spectator) { GameModeManager.Singleton.AddPlayerScore(playerData.id, playerData.character._id); }
            }
        }

        private IEnumerator WaitForSpawnToAddPlayerData(PlayerData playerData)
        {
            yield return new WaitUntil(() => IsSpawned);
            AddPlayerData(playerData);
        }

        public PlayerData GetPlayerData(int clientId) { return cachedPlayerDataList.Find(item => item.id == clientId); }

        public PlayerData GetDisconnectedPlayerData(int clientId)
        {
            for (int i = 0; i < disconnectedPlayerDataList.Count; i++)
            {
                DisconnectedPlayerData disconnectedPlayerData = disconnectedPlayerDataList[i];
                if (clientId == disconnectedPlayerData.playerData.id) { return disconnectedPlayerData.playerData; }
            }
            Debug.LogError("Could not find disconnected player data with ID: " + clientId);
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

        public void KickPlayer(int clientId)
        {
            if (IsServer)
            {
                KickPlayerOnServer(clientId);
            }
            else
            {
                KickPlayerServerRpc(clientId);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)] private void KickPlayerServerRpc(int clientId) { KickPlayerOnServer(clientId); }

        private void KickPlayerOnServer(int clientId)
        {
            if (clientId >= 0)
            {
                NetworkManager.DisconnectClient((ulong)clientId, "You have been kicked from the session.");
            }
            else
            {
                RemovePlayerData(clientId);
            }

            if (playerIdThatIsBeingSpawned == clientId)
            {
                EndSpawnPlayerCoroutine();
            }
        }

        public void RemovePlayerData(int clientId)
        {
            int index = playerDataList.IndexOf(new PlayerData(clientId));
            if (index == -1) { Debug.LogError("Could not find player data to remove for id: " + clientId); return; }
            if (GameModeManager.Singleton)
            {
                if (GetGameMode() != GameMode.None) { disconnectedPlayerDataList.Add(new DisconnectedPlayerData(playerDataList[index])); }
                GameModeManager.Singleton.RemovePlayerScore(clientId, playerDataList[index].character._id);
            }
            playerDataList.RemoveAt(index);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetPlayerDataServerRpc(PlayerData playerData) { SetPlayerData(playerData); }

        public static bool DoesExist() { return _singleton; }

        public static PlayerDataManager Singleton
        {
            get
            {
                if (!_singleton) { Debug.LogError("Player Data Manager is null"); }
                return _singleton;
            }
        }

        private static PlayerDataManager _singleton;
        private void Awake()
        {
            _singleton = this;
            playerDataList = new NetworkList<PlayerData>();
            disconnectedPlayerDataList = new NetworkList<DisconnectedPlayerData>();
        }

        private void OnEnable()
        {
            EventDelegateManager.sceneLoaded += OnSceneLoad;
            EventDelegateManager.sceneUnloaded += OnSceneUnload;

            NetworkManager.OnClientConnectedCallback += OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        private void OnDisable()
        {
            EventDelegateManager.sceneLoaded -= OnSceneLoad;
            EventDelegateManager.sceneUnloaded -= OnSceneUnload;

            NetworkManager.OnClientConnectedCallback -= OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }

        public PlayerSpawnPoints.TransformData[] GetEnvironmentViewPoints()
        {
            if (playerSpawnPoints)
            {
                return playerSpawnPoints.GetEnvironmentViewPoints();
            }
            else
            {
                Debug.LogWarning("Trying to access environment view points when there is no player spawn points object");
                return new PlayerSpawnPoints.TransformData[0];
            }
        }

        public PlayerSpawnPoints.TransformData[] GetGameItemSpawnPoints()
        {
            if (playerSpawnPoints)
            {
                float distanceThreshold = 8;
                List<PlayerSpawnPoints.TransformData> possibleSpawnPoints = new List<PlayerSpawnPoints.TransformData>();
                List<Attributes> localPlayerList = localPlayers.Values.ToList();
                foreach (PlayerSpawnPoints.TransformData transformData in playerSpawnPoints.GetGameItemSpawnPoints())
                {
                    if (localPlayerList.TrueForAll(item => Vector3.Distance(item.transform.position, transformData.position) > distanceThreshold))
                    {
                        possibleSpawnPoints.Add(transformData);
                    }
                }
                return possibleSpawnPoints.ToArray();
            }
            else
            {
                Debug.LogWarning("Trying to access game item spawn points when there is no player spawn points object");
                return new PlayerSpawnPoints.TransformData[0];
            }
        }

        public bool HasPlayerSpawnPoints()
        {
            return playerSpawnPoints;
        }

        public PlayerSpawnPoints GetPlayerSpawnPoints()
        {
            return playerSpawnPoints;
        }

        public Vector3 GetDamageCircleMaxScale()
        {
            if (playerSpawnPoints)
            {
                return playerSpawnPoints.GetDamageCircleMaxScale();
            }
            else
            {
                Debug.LogError("Trying to get damage circle max scale without a player spawn points object");
                return default;
            }
        }

        public Vector3 GetDamageCircleMinScale()
        {
            if (playerSpawnPoints)
            {
                return playerSpawnPoints.GetDamageCircleMinScale();
            }
            else
            {
                Debug.LogError("Trying to get damage circle min scale without a player spawn points object");
                return default;
            }
        }

        public float GetDamageCircleShrinkSize()
        {
            if (playerSpawnPoints)
            {
                return playerSpawnPoints.GetDamageCircleShrinkSize();
            }
            else
            {
                Debug.LogError("Trying to get damage circle shrink size without a player spawn points object");
                return default;
            }
        }

        private PlayerSpawnPoints playerSpawnPoints;
        void OnSceneLoad(Scene scene)
        {
            foreach (GameObject g in scene.GetRootGameObjects())
            {
                if (g.TryGetComponent(out playerSpawnPoints)) { break; }
            }

            // Need to check singleton since this object may be despawned
            if (NetworkManager.Singleton.IsServer)
            {
                if (NetSceneManager.Singleton.ShouldSpawnPlayer())
                {
                    for (int i = 0; i < playerDataList.Count; i++)
                    {
                        playersToSpawnQueue.Enqueue(playerDataList[i]);
                    }
                }
            }
        }

        void OnSceneUnload()
        {
            if (IsServer)
            {
                if (!NetSceneManager.Singleton.ShouldSpawnPlayer())
                {
                    foreach (Attributes attributes in GetActivePlayerObjects())
                    {
                        attributes.NetworkObject.Despawn(true);
                    }

                    foreach (NetworkObject spectator in localSpectators.Values.ToList())
                    {
                        spectator.Despawn(true);
                    }
                }
            }
        }

        private Dictionary<ulong, NetworkObject> localSpectators = new Dictionary<ulong, NetworkObject>();

        public void AddSpectatorInstance(ulong clientId, NetworkObject networkObject)
        {
            localSpectators.Add(clientId, networkObject);
        }

        public void RemoveSpectatorInstance(ulong clientId)
        {
            localSpectators.Remove(clientId);
        }

        public KeyValuePair<ulong, NetworkObject> GetLocalSpectatorObject()
        {
            try
            {
                return localSpectators.First(kvp => kvp.Value.IsLocalPlayer);
            }
            catch
            {
                return new KeyValuePair<ulong, NetworkObject>(NetworkManager.LocalClientId, null);
            }
        }

        private void Update()
        {
            if (playerSpawnPoints == null & NetSceneManager.Singleton.IsEnvironmentLoaded())
            {
                playerSpawnPoints = FindObjectOfType<PlayerSpawnPoints>();
            }
        }

        public override void OnNetworkSpawn()
        {
            playerDataList.OnListChanged += OnPlayerDataListChange;
            gameMode.OnValueChanged += OnGameModeChange;
            NetworkManager.NetworkTickSystem.Tick += Tick;

            if (IsServer)
            {
                playerDataList.Clear();
            }
            SyncCachedPlayerDataList();
        }

        public override void OnNetworkDespawn()
        {
            playerDataList.OnListChanged -= OnPlayerDataListChange;
            gameMode.OnValueChanged -= OnGameModeChange;
            NetworkManager.NetworkTickSystem.Tick -= Tick;

            localPlayers.Clear();
            botClientId = 0;
            SyncCachedPlayerDataList();
        }

        private void SyncCachedPlayerDataList()
        {
            cachedPlayerDataList.Clear();
            foreach (PlayerData playerData in playerDataList)
            {
                cachedPlayerDataList.Add(playerData);
            }
        }

        private void Tick()
        {
            if (playersToSpawnQueue.Count > 0 & !spawnPlayerRunning)
            {
                spawnPlayerCoroutine = StartCoroutine(SpawnPlayer(playersToSpawnQueue.Dequeue()));
            }

            if (Time.time - lastSpawnPlayerStartTime > spawnPlayerTimeoutThreshold & spawnPlayerRunning)
            {
                EndSpawnPlayerCoroutine();
            }
        }

        private Queue<PlayerData> playersToSpawnQueue = new Queue<PlayerData>();
        private void OnPlayerDataListChange(NetworkListEvent<PlayerData> networkListEvent)
        {
            SyncCachedPlayerDataList();

            if ((int)NetworkManager.LocalClientId == networkListEvent.Value.id) { LocalPlayerData = networkListEvent.Value; }

            switch (networkListEvent.Type)
            {
                case NetworkListEvent<PlayerData>.EventType.Add:
                    if (IsServer)
                    {
                        if (NetSceneManager.Singleton.ShouldSpawnPlayer())
                        {
                            playersToSpawnQueue.Enqueue(networkListEvent.Value);
                        }

                        KeyValuePair<bool, PlayerData> kvp = GetLobbyLeader();
                        StartCoroutine(WebRequestManager.Singleton.UpdateServerPopulation(GetPlayerDataListWithSpectators().FindAll(item => item.id >= 0).Count,
                            kvp.Key ? kvp.Value.character.name.ToString() : StringUtility.FromCamelCase(GetGameMode().ToString())));
                    }
                    break;
                case NetworkListEvent<PlayerData>.EventType.Insert:
                    break;
                case NetworkListEvent<PlayerData>.EventType.Remove:
                case NetworkListEvent<PlayerData>.EventType.RemoveAt:
                    if (IsServer)
                    {
                        KeyValuePair<bool, PlayerData> kvp = GetLobbyLeader();
                        StartCoroutine(WebRequestManager.Singleton.UpdateServerPopulation(GetPlayerDataListWithSpectators().FindAll(item => item.id >= 0).Count,
                            kvp.Key ? kvp.Value.character.name.ToString() : StringUtility.FromCamelCase(GetGameMode().ToString())));

                        // If there is a local player for this id, despawn it
                        if (localPlayers.ContainsKey(networkListEvent.Value.id)) { localPlayers[networkListEvent.Value.id].NetworkObject.Despawn(true); }
                    }
                    break;
                case NetworkListEvent<PlayerData>.EventType.Value:
                    if (localPlayers.ContainsKey(networkListEvent.Value.id))
                    {
                        LoadoutManager loadoutManager = localPlayers[networkListEvent.Value.id].GetComponent<LoadoutManager>();
                        loadoutManager.ApplyLoadout(networkListEvent.Value.character.raceAndGender, networkListEvent.Value.character.GetActiveLoadout(), networkListEvent.Value.character._id.ToString(), GetGameMode() != GameMode.None);

                        localPlayers[networkListEvent.Value.id].SetCachedPlayerData(networkListEvent.Value);
                    }
                    break;
                case NetworkListEvent<PlayerData>.EventType.Clear:
                    break;
                case NetworkListEvent<PlayerData>.EventType.Full:
                    break;
            }

            DataListWasUpdatedThisFrame = true;

            if (resetDataListBoolCoroutine != null) { StopCoroutine(resetDataListBoolCoroutine); }
            resetDataListBoolCoroutine = StartCoroutine(ResetDataListWasUpdatedBool());
        }

        public PlayerData LocalPlayerData;

        public bool DataListWasUpdatedThisFrame { get; private set; } = false;

        private Coroutine resetDataListBoolCoroutine;
        private IEnumerator ResetDataListWasUpdatedBool()
        {
            yield return null;
            DataListWasUpdatedThisFrame = false;
        }

        private void OnClientConnectCallback(ulong clientId)
        {
            //Debug.Log("Id: " + clientId + " has connected.");
        }

        public IEnumerator RespawnPlayer(Attributes attributesToRespawn)
        {
            (bool spawnPointFound, PlayerSpawnPoints.TransformData transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, attributesToRespawn.GetTeam(), attributesToRespawn);
            float waitTime = 0;
            while (!spawnPointFound)
            {
                attributesToRespawn.isWaitingForSpawnPoint = true;
                (spawnPointFound, transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, attributesToRespawn.GetTeam(), attributesToRespawn);
                yield return null;
                waitTime += Time.deltaTime;
                if (waitTime > maxSpawnPointWaitTime) { break; }
            }

            attributesToRespawn.isWaitingForSpawnPoint = false;

            Vector3 spawnPosition = transformData.position;
            Quaternion spawnRotation = transformData.rotation;

            attributesToRespawn.ResetStats(1, false);
            attributesToRespawn.GetComponent<AnimationHandler>().CancelAllActions(0);
            attributesToRespawn.GetComponent<MovementHandler>().SetOrientation(spawnPosition, spawnRotation);
            attributesToRespawn.GetComponent<LoadoutManager>().SwapLoadoutOnRespawn();
        }

        public void RevivePlayer(Attributes attributesToRevive)
        {
            attributesToRevive.ResetStats(0.5f, false);
            attributesToRevive.GetComponent<AnimationHandler>().CancelAllActions(0);
        }

        public void RespawnAllPlayers()
        {
            foreach (KeyValuePair<int, Attributes> kvp in localPlayers)
            {
                playersToSpawnQueue.Enqueue(GetPlayerData(kvp.Key));
            }
        }

        private void EndSpawnPlayerCoroutine()
        {
            if (!IsServer) { Debug.LogError("PlayerDataManager.EndSpawnPlayerCoroutine() shold only be called on the server!"); return; }

            if (spawnPlayerCoroutine != null) { StopCoroutine(spawnPlayerCoroutine); }
            spawnPlayerRunning = false;

            if (playerObjectToSpawn)
            {
                if (playerObjectToSpawn.GetComponent<NetworkObject>().IsSpawned)
                {
                    playerObjectToSpawn.GetComponent<NetworkObject>().Despawn(true);
                }
                else
                {
                    Destroy(playerObjectToSpawn);
                }
            }
            
            if (playerIdThatIsBeingSpawned >= 0)
            {
                if (NetworkManager.ConnectedClientsIds.Contains((ulong)playerIdThatIsBeingSpawned))
                {
                    NetworkManager.DisconnectClient((ulong)playerIdThatIsBeingSpawned, "Timed out while spawning player object");
                }
            }
        }

        public bool IsWaitingForSpawnPoint() { return isWaitingForSpawnPoint.Value; }

        private NetworkVariable<bool> isWaitingForSpawnPoint = new NetworkVariable<bool>();

        private const float spawnPlayerTimeoutThreshold = 10;
        private const float maxSpawnPointWaitTime = 5;

        private int playerIdThatIsBeingSpawned;
        private bool spawnPlayerRunning;
        private Coroutine spawnPlayerCoroutine;
        private float lastSpawnPlayerStartTime;
        private GameObject playerObjectToSpawn;
        private IEnumerator SpawnPlayer(PlayerData playerData)
        {
            spawnPlayerRunning = true;
            playerIdThatIsBeingSpawned = playerData.id;
            lastSpawnPlayerStartTime = Time.time;
            if (playerData.id >= 0)
            {
                yield return new WaitUntil(() => NetworkManager.ConnectedClientsIds.Contains((ulong)playerData.id));
            }
            if (localPlayers.ContainsKey(playerData.id))
            {
                //Debug.LogError("Calling SpawnPlayer() while there is an entry for this local player already! Id: " + playerData.id);
                yield return RespawnPlayer(localPlayers[playerData.id]);
                spawnPlayerRunning = false;
                yield break;
            }
            yield return new WaitUntil(() => playerSpawnPoints);

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (playerSpawnPoints)
            {
                (bool spawnPointFound, PlayerSpawnPoints.TransformData transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team, null);
                float waitTime = 0;
                while (!spawnPointFound)
                {
                    isWaitingForSpawnPoint.Value = true;
                    (spawnPointFound, transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team, null);
                    yield return null;
                    waitTime += Time.deltaTime;
                    if (waitTime > maxSpawnPointWaitTime) { break; }
                }
                isWaitingForSpawnPoint.Value = false;

                spawnPosition = transformData.position;
                spawnRotation = transformData.rotation;
            }
            else
            {
                Debug.LogError("Trying to spawn player without a player spawn points object!");
            }

            KeyValuePair<int, int> kvp = Singleton.GetCharacterReference().GetPlayerModelOptionIndices(playerData.character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            if (!ContainsId(playerData.id))
            {
                spawnPlayerRunning = false;
                yield break;
            }

            bool isSpectator = GetPlayerData(playerData.id).team == Team.Spectator;
            if (isSpectator)
            {
                playerObjectToSpawn = Instantiate(spectatorPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                if (playerData.id >= 0)
                    playerObjectToSpawn = Instantiate(characterReference.GetPlayerModelOptions()[characterIndex].playerPrefab, spawnPosition, spawnRotation);
                else
                    playerObjectToSpawn = Instantiate(characterReference.GetPlayerModelOptions()[characterIndex].botPrefab, spawnPosition, spawnRotation);

                playerObjectToSpawn.GetComponent<Attributes>().SetPlayerDataId(playerData.id);
            }

            NetworkObject netObj = playerObjectToSpawn.GetComponent<NetworkObject>();
            if (playerData.id >= 0)
            {
                netObj.SpawnAsPlayerObject((ulong)playerData.id, true);
            }
            else
            {
                netObj.Spawn(true);
            }

            //yield return new WaitUntil(() => playerObject.GetComponent<NetworkObject>().IsSpawned);
            spawnPlayerRunning = false;
        }

        [HideInInspector] public bool wasDisconnectedByClient;

        [SerializeField] private GameObject alertBoxPrefab;
        private void OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log("Id: " + clientId + " - Name: " + GetPlayerData((int)clientId).character.name + " has disconnected.");
            if (IsServer) { RemovePlayerData((int)clientId); }
            if (!NetworkManager.IsServer && NetworkManager.DisconnectReason != string.Empty)
            {
                Debug.Log($"Approval Declined Reason: {NetworkManager.DisconnectReason}");
            }
            if (IsClient)
            {
                if (!wasDisconnectedByClient)
                {
                    // This object gets despawned, so make sure to not start this on a networkobject
                    PersistentLocalObjects.Singleton.StartCoroutine(ReturnToCharacterSelectOnServerShutdown());
                }
            }
        }

        private IEnumerator ReturnToCharacterSelectOnServerShutdown()
        {
            yield return null;
            if (NetworkManager.Singleton.IsListening) { yield break; }
            yield return new WaitUntil(() => !NetSceneManager.Singleton.IsBusyLoadingScenes());
            if (!NetSceneManager.Singleton.IsSceneGroupLoaded("Character Select"))
            {
                NetSceneManager.Singleton.LoadScene("Character Select");
            }

            if (!string.IsNullOrWhiteSpace(NetworkManager.DisconnectReason))
            {
                Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = NetworkManager.DisconnectReason;
            }
            else
            {
                Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "Disconnected From Server.";
            }
        }

        public List<PlayerData> GetPlayerDataListWithSpectators() { return cachedPlayerDataList.ToList(); }

        public List<PlayerData> GetPlayerDataListWithoutSpectators() { return cachedPlayerDataList.Where(item => item.team != Team.Spectator).ToList(); }

        public List<PlayerData> GetDisconnectedPlayerDataList()
        {
            List<PlayerData> playerDatas = new List<PlayerData>();
            for (int i = 0; i < disconnectedPlayerDataList.Count; i++)
            {
                playerDatas.Add(disconnectedPlayerDataList[i].playerData);
            }
            return playerDatas;
        }

        private NetworkList<PlayerData> playerDataList;
        private List<PlayerData> cachedPlayerDataList = new List<PlayerData>();

        private NetworkList<DisconnectedPlayerData> disconnectedPlayerDataList;

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

        public struct DisconnectedPlayerData : INetworkSerializable, System.IEquatable<DisconnectedPlayerData>
        {
            public PlayerData playerData;

            public DisconnectedPlayerData(PlayerData playerData)
            {
                this.playerData = playerData;
            }

            public bool Equals(DisconnectedPlayerData other)
            {
                return playerData.character._id == other.playerData.character._id;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeNetworkSerializable(ref playerData);
            }
        }
    }
}