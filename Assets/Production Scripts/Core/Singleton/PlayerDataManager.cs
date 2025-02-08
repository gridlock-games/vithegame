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
using Vi.Core.CombatAgents;
using Vi.Core.Structures;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.Core
{
    public class PlayerDataManager : NetworkBehaviour
    {
        public Sprite DefaultEnvironmentKillIcon { get { return _defaultEnvironmentKillIcon; } }
        public Sprite _defaultEnvironmentKillIcon;

        [SerializeField] private GameObject spectatorPrefab;
        
        [SerializeField] private List<GameModeInfo> gameModeInfos;

        [System.Serializable]
        public struct GameModeInfo
        {
            public GameMode gameMode;
            public Sprite gameModeIcon;
            public Team[] possibleTeams;
            public string[] possibleMapSceneGroupNames;
            public int[] maxPlayersOnMap;
        }

        public static bool IsCharacterReferenceLoaded() { return CharacterReferenceHandle.IsValid() & CharacterReferenceHandle.IsDone; }

        [SerializeField] private AssetReference characterReferenceAddressable;
        public static AsyncOperationHandle<CharacterReference> CharacterReferenceHandle { get; private set; }
        public CharacterReference GetCharacterReference()
        {
            if (CharacterReferenceHandle.IsValid() & CharacterReferenceHandle.IsDone)
            {
                return CharacterReferenceHandle.Result;
            }
            else
            {
                Debug.LogError("Accessing character reference before it is loaded!");
                return null;
            }
        }

        [SerializeField] private AssetReference controlsImageMappingAddressable;
        public static AsyncOperationHandle<ControlsImageMapping> ControlsImageMappingHandle;
        public ControlsImageMapping GetControlsImageMapping()
        {
            if (ControlsImageMappingHandle.IsValid() & ControlsImageMappingHandle.IsDone)
            {
                return ControlsImageMappingHandle.Result;
            }
            else
            {
                Debug.LogError("Accessing controls image mapping before it is loaded!");
                return null;
            }
        }

        public GameModeInfo GetGameModeInfo() { return gameModeInfos.Find(item => item.gameMode == gameMode.Value); }

        public Sprite GetGameModeIcon(GameMode gameMode) { return gameModeInfos.Find(item => item.gameMode == gameMode).gameModeIcon; }

        private NetworkVariable<GameMode> gameMode = new NetworkVariable<GameMode>();
        public GameMode GetGameMode() { return gameMode.Value; }

        private bool CanSwapGameModes()
        {
            if (GameModeManager.Singleton) { return false; }
            return true;
        }

        public void SetGameMode(GameMode newGameMode)
        {
            if (!CanSwapGameModes()) { return; }

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
            if (IsServer)
            {
                if (gameModeInfos.Exists(item => item.gameMode == prev))
                {
                    var prevGameModeInfo = gameModeInfos.Find(item => item.gameMode == prev);
                    if (mapIndex.Value < prevGameModeInfo.possibleMapSceneGroupNames.Length)
                    {
                        string oldMapName = prevGameModeInfo.possibleMapSceneGroupNames[mapIndex.Value];
                        if (GetGameModeInfo().possibleMapSceneGroupNames.Contains(oldMapName))
                        {
                            mapIndex.Value = System.Array.IndexOf(GetGameModeInfo().possibleMapSceneGroupNames, oldMapName);
                        }
                        else
                        {
                            mapIndex.Value = 0;
                        }
                    }
                    else
                    {
                        mapIndex.Value = 0;
                    }
                }
                else
                {
                    mapIndex.Value = 0;
                }
            }
        }

        private NetworkVariable<int> mapIndex = new NetworkVariable<int>();
        public string GetMapName()
        {
            return GetGameModeInfo().possibleMapSceneGroupNames[mapIndex.Value];
        }

        public int GetMapIndex()
        {
            return mapIndex.Value;
        }

        public int GetMaxPlayersForMap()
        {
            try
            {
                return GetGameModeInfo().maxPlayersOnMap[mapIndex.Value];
            }
            catch
            {
                return 8;
            }
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
            if (attackerTeam == Team.Spectator | victimTeam == Team.Spectator) { return false; }
            if (attackerTeam == Team.Peaceful | victimTeam == Team.Peaceful) { return false; }

            if (attackerTeam != Team.Competitor & victimTeam != Team.Competitor)
            {
                if (attackerTeam == victimTeam) { return false; }
            }
            return true;
        }

        public bool CanHit(HittableAgent attacker, HittableAgent victim)
        {
            if (!attacker) { Debug.LogWarning("Calling PlayerDataManager.CanHit() with a null attacker!"); return false; }
            if (!victim) { Debug.LogWarning("Calling PlayerDataManager.CanHit() with a null victim!"); return false; }
            return CanHit(attacker.GetTeam(), victim.GetTeam()) & attacker != victim;
        }

        public bool CanHit(CombatAgent attacker, CombatAgent victim)
        {
            if (attacker.Master == victim) { return false; }
            if (attacker.GetSlaves().Contains(victim)) { return false; }

            if (attacker.Master)
            {
                if (attacker.Master.GetSlaves().Contains(victim)) { return false; }
            }

            if (!attacker) { Debug.LogWarning("Calling PlayerDataManager.CanHit() with a null attacker!"); return false; }
            if (!victim) { Debug.LogWarning("Calling PlayerDataManager.CanHit() with a null victim!"); return false; }
            return CanHit(attacker.GetTeam(), victim.GetTeam()) & attacker != victim;
        }

        private readonly static Dictionary<Team, Color> teamColors = new Dictionary<Team, Color>()
        {
            { Team.Peaceful, new Color(65 / 255f, 65 / 255f, 65 / 255f, 1) },
            { Team.Competitor, new Color(65 / 255f, 65 / 255f, 65 / 255f, 1) },
            { Team.Red, Color.red },
            { Team.Orange, new Color(239 / (float)255, 130 / (float)255, 37 / (float)255, 1) },
            { Team.Yellow, Color.yellow },
            { Team.Green, Color.green },
            { Team.Blue, Color.blue },
            { Team.Purple, Color.magenta },
            { Team.Light, new Color(5f / 255, 159f / 255, 242f / 255, 1) },
            { Team.Corruption, new Color(237f / 255, 85f / 255, 84f / 255, 1) },
            { Team.Spectator, Color.white }
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

        public Color GetRelativeTeamColor(Team team)
        {
            if (LocalPlayerData.team == Team.Spectator | team == Team.Spectator)
            {
                return GetTeamColor(team);
            }
            else
            {
                return CanHit(team, LocalPlayerData.team) ? enemyColor : teammateColor;
            }
        }

        public Color GetRelativeHealthBarColor(Team team)
        {
            if (LocalPlayerData.team == Team.Spectator)
            {
                if (team == Team.Peaceful)
                {
                    return teammateColor;
                }
                else if (team == Team.Competitor | team == Team.Environment)
                {
                    return enemyColor;
                }
                else
                {
                    return GetTeamColor(team);
                }
            }
            else
            {
                return CanHit(team, LocalPlayerData.team) ? enemyColor : teammateColor;
            }
        }

        private Color enemyColor = Color.red;
        private Color teammateColor = Color.cyan;
        public Color LocalPlayerColor { get; private set; } = Color.white;
        public static readonly Color LocalPlayerBackgroundColor = new Color(65 / 255f, 65 / 255f, 65 / 255f, 1);
        protected virtual void RefreshStatus()
        {
            enemyColor = FasterPlayerPrefs.Singleton.GetColor("EnemyColor");
            teammateColor = FasterPlayerPrefs.Singleton.GetColor("TeammateColor");
            LocalPlayerColor = FasterPlayerPrefs.Singleton.GetColor("LocalPlayerColor");
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
                if (IsLobbyLeader())
                {
                    SetTeamNameOverrideServerRpc(team, teamName, prefix);
                }
                else
                {
                    Debug.LogError("Trying to set team name overrides when we're not the lobby leader!");
                }
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)] private void SetTeamNameOverrideServerRpc(Team team, string teamName, string prefix) { SetTeamNameOverride(team, teamName, prefix); }

        public bool TeamNameOverridesUpdatedThisFrame { get; private set; }
        private void OnTeamNameOverridesJsonChange(FixedString512Bytes prev, FixedString512Bytes current)
        {
            if (!IsServer) { teamNameOverrides = JsonConvert.DeserializeObject<Dictionary<Team, TeamNameOverride>>(teamNameOverridesJson.Value.ToString()); }
            TeamNameOverridesUpdatedThisFrame = true;
            if (teamNameOverridesWasUpdatedThisFrameCoroutine != null) { StopCoroutine(teamNameOverridesWasUpdatedThisFrameCoroutine); }
            teamNameOverridesWasUpdatedThisFrameCoroutine = StartCoroutine(ResetTeamNameOverridesUpdatedBool());
        }

        private Coroutine teamNameOverridesWasUpdatedThisFrameCoroutine;
        private IEnumerator ResetTeamNameOverridesUpdatedBool()
        {
            yield return null;
            TeamNameOverridesUpdatedThisFrame = false;
        }

        public string GetTeamText(Team team)
        {
            if (teamNameOverrides != null)
            {
                if (teamNameOverrides.ContainsKey(team)) { return teamNameOverrides[team].teamName; }
            }

            switch (team)
            {
                case Team.Environment:
                case Team.Peaceful:
                case Team.Light:
                case Team.Corruption:
                    return team.ToString();
                case Team.Competitor:
                    return "Competitors";
                default:
                    return team.ToString() + " Team";
            }
        }

        public string GetTeamPrefix(Team team)
        {
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
            if (teamNameOverrides != null)
            {
                if (teamNameOverrides.ContainsKey(team)) { return teamNameOverrides[team].prefix; }
            }
            return "";
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
            Peaceful,
            Light,
            Corruption
        }

        public static int GetGameModeMinPlayers(GameMode gameMode)
        {
            if (gameMode == GameMode.HordeMode)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public static string GetGameModeString(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.None:
                    return "No Game Mode";
                case GameMode.FreeForAll:
                    return "Free For All";
                case GameMode.TeamElimination:
                    return "Team Elimination";
                case GameMode.EssenceWar:
                    return "Essence War";
                case GameMode.OutpostRush:
                    return "Outpost Rush";
                case GameMode.TeamDeathmatch:
                    return "Team Deathmatch";
                case GameMode.HordeMode:
                    return "Corrupted Abyss";
                default:
                    Debug.LogError(gameMode + " doesn't know how to format game mode display string");
                    return StringUtility.FromCamelCase(gameMode.ToString());
            }
        }

        public enum GameMode
        {
            None,
            FreeForAll,
            TeamElimination,
            EssenceWar,
            OutpostRush,
            TeamDeathmatch,
            HordeMode
        }

        public bool LocalPlayersWasUpdatedThisFrame { get; private set; } = false;
        private Dictionary<int, Attributes> localPlayers = new Dictionary<int, Attributes>();
        public void AddPlayerObject(int clientId, Attributes playerObject)
        {
            if (localPlayers.ContainsKey(clientId))
            {
                Debug.LogError("Trying to add a local player that is already present. Client Id: " + clientId);
            }
            else
            {
                localPlayers.Add(clientId, playerObject);
            }
            LocalPlayersWasUpdatedThisFrame = true;

            if (resetLocalPlayerBoolCoroutine != null) { StopCoroutine(resetLocalPlayerBoolCoroutine); }
            resetLocalPlayerBoolCoroutine = StartCoroutine(ResetLocalPlayersWasUpdatedBool());

            playerObject.SetCachedPlayerData(Singleton.GetPlayerData(playerObject.GetPlayerDataId()));
        }

        public void RemovePlayerObject(int clientId)
        {
            if (!localPlayers.Remove(clientId) & !NetworkManager.ShutdownInProgress) { Debug.LogError("Could not remove client id local player " + clientId); }
            LocalPlayersWasUpdatedThisFrame = true;

            if (resetLocalPlayerBoolCoroutine != null) { StopCoroutine(resetLocalPlayerBoolCoroutine); }
            resetLocalPlayerBoolCoroutine = StartCoroutine(ResetLocalPlayersWasUpdatedBool());
        }

        private List<CombatAgent> activeCombatAgents = new List<CombatAgent>();
        public void AddCombatAgent(CombatAgent combatAgent)
        {
            if (!activeCombatAgents.Contains(combatAgent))
            {
                activeCombatAgents.Add(combatAgent);
            }
            else
            {
                Debug.LogError("Trying to add a duplicate combat agent! " + combatAgent);
            }

            LocalPlayersWasUpdatedThisFrame = true;
            if (resetLocalPlayerBoolCoroutine != null) { StopCoroutine(resetLocalPlayerBoolCoroutine); }
            resetLocalPlayerBoolCoroutine = StartCoroutine(ResetLocalPlayersWasUpdatedBool());
        }

        public void RemoveCombatAgent(CombatAgent combatAgent)
        {
            if (activeCombatAgents.Contains(combatAgent))
            {
                activeCombatAgents.Remove(combatAgent);
            }
            else
            {
                Debug.LogError("Trying to remove a combat agent that isn't present in the list! " + combatAgent);
            }

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

        public bool StructuresListWasUpdatedThisFrame { get; private set; } = false;
        private List<Structure> activeStructures = new List<Structure>();
        public void AddStructure(Structure structure)
        {
            activeStructures.Add(structure);

            StructuresListWasUpdatedThisFrame = true;
            if (resetStructureBoolCoroutine != null) { StopCoroutine(resetStructureBoolCoroutine); }
            resetStructureBoolCoroutine = StartCoroutine(ResetStructuresWasUpdatedBool());
        }

        public void RemoveStructure(Structure structure)
        {
            if (activeStructures.Contains(structure))
            {
                activeStructures.Remove(structure);
            }
            else
            {
                Debug.LogError("Trying to remove a structure that isn't present in the list! " + structure);
            }
            
            StructuresListWasUpdatedThisFrame = true;
            if (resetStructureBoolCoroutine != null) { StopCoroutine(resetStructureBoolCoroutine); }
            resetStructureBoolCoroutine = StartCoroutine(ResetStructuresWasUpdatedBool());
        }

        private Coroutine resetStructureBoolCoroutine;
        private IEnumerator ResetStructuresWasUpdatedBool()
        {
            yield return null;
            StructuresListWasUpdatedThisFrame = false;
        }

        public Structure[] GetActiveStructures()
        {
            activeStructures.RemoveAll(item => !item);
            return activeStructures.ToArray();
        }

        public List<Attributes> GetPlayerObjectsOnTeam(Team team, Attributes attributesToExclude = null)
        {
            // If the attributes to exclude is on competitor or peaceful teams, we don't want to return any teammates for this attributes
            if (attributesToExclude)
            {
                if (attributesToExclude.GetTeam() == Team.Competitor | attributesToExclude.GetTeam() == Team.Peaceful) { return new List<Attributes>(); }
            }
            return localPlayers.Where(kvp => kvp.Value.GetTeam() == team & kvp.Value != attributesToExclude).Select(kvp => kvp.Value).ToList();
        }

        public List<CombatAgent> GetCombatAgentsOnTeam(Team team, CombatAgent combatAgentToExclude = null)
        {
            // If the combatagent to exclude is on competitor or peaceful teams, we don't want to return any teammates for this attributes
            if (combatAgentToExclude)
            {
                if (combatAgentToExclude.GetTeam() == Team.Competitor | combatAgentToExclude.GetTeam() == Team.Peaceful) { return new List<CombatAgent>(); }
            }
            return activeCombatAgents.Where(item => item.GetTeam() == team & item != combatAgentToExclude).ToList();
        }

        public List<Attributes> GetActivePlayerObjects(Attributes attributesToExclude = null)
        {
            return localPlayers.Where(kvp => kvp.Value.GetTeam() != Team.Spectator & kvp.Value != attributesToExclude).Select(kvp => kvp.Value).ToList();
        }

        public List<CombatAgent> GetActiveCombatAgents(CombatAgent combatAgentToExclude = null)
        {
            return activeCombatAgents.Where(item => item.GetTeam() != Team.Spectator & item != combatAgentToExclude).ToList();
        }

        public List<Attributes> GetActivePlayerObjectsInChannel(int channel)
        {
            return localPlayers.Where(kvp => kvp.Value.GetTeam() != Team.Spectator & kvp.Value.CachedPlayerData.channel == channel).Select(kvp => kvp.Value).ToList();
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

        public bool ContainsId(int clientId) { return cachedIdList.Contains(clientId); }

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
                    "Jun"
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

        public string GetRandomPlayerName(CharacterReference.RaceAndGender raceAndGender)
        {
            List<string> potentialNames = botNames[raceAndGender];
            potentialNames.AddRange(botNames[CharacterReference.RaceAndGender.Universal]);
            potentialNames.RemoveAll(item => string.IsNullOrWhiteSpace(item));
            potentialNames = potentialNames.Distinct().ToList();

            List<int> validNameIndexes = new List<int>();
            for (int i = 0; i < potentialNames.Count; i++)
            {
                validNameIndexes.Add(i);
            }

            foreach (PlayerData playerData in GetPlayerDataListWithSpectators())
            {
                int index = potentialNames.IndexOf(playerData.character.name.ToString());
                if (index != -1) { validNameIndexes.Remove(index); }
            }

            return potentialNames[validNameIndexes[Random.Range(0, validNameIndexes.Count)]];
        }

        private int botClientId = 0;
        public void AddBotData(Team team, bool useDefaultPrimaryWeapon)
        {
            if (team == Team.Spectator) { Debug.LogError("Trying to add a bot as a spectator!"); return; }

            if (!CanPlayersChangeTeams(team)) { return; }

            if (IsServer)
            {
                botClientId--;

                CharacterManager.Character botCharacter = WebRequestManager.Singleton.CharacterManager.GetRandomizedCharacter(useDefaultPrimaryWeapon);
                botCharacter.name = GetRandomPlayerName(botCharacter.raceAndGender);
                PlayerData botData = new PlayerData(botClientId,
                    defaultChannel,
                    botCharacter,
                    team);
                AddPlayerData(botData);
            }
            else
            {
                AddBotDataServerRpc(team, useDefaultPrimaryWeapon);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)] private void AddBotDataServerRpc(Team team, bool useDefaultPrimaryWeapon) { AddBotData(team, useDefaultPrimaryWeapon); }

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

                int loadoutSlotIndex = serverSideOriginalLoadouts.FindIndex(item => item.Item1 == playerData.id);
                if (loadoutSlotIndex == -1)
                {
                    serverSideOriginalLoadouts.Add((playerData.id, playerData.character.GetActiveLoadout().loadoutSlot));
                }
                else
                {
                    serverSideOriginalLoadouts[loadoutSlotIndex] = (playerData.id, playerData.character.GetActiveLoadout().loadoutSlot);
                }

                if (GameModeManager.Singleton & playerData.team != Team.Spectator) { GameModeManager.Singleton.AddPlayerScore(playerData.id, playerData.character._id); }
            }
        }

        private List<(int, FixedString64Bytes)> serverSideOriginalLoadouts = new List<(int, FixedString64Bytes)>();

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

        public bool CanPlayersChangeTeams(PlayerDataManager.Team teamToChangeTo)
        {
            if (GetGameMode() == GameMode.None) { return true; }
            if (GameModeManager.Singleton) { return false; }

            int limitTotalNumberOfPlayersOnTeam = PlayerDataManager.Singleton.GetMaxPlayersForMap() / PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams.Length;
            return GetPlayerDataListWithoutSpectators().Where(item => item.team == teamToChangeTo).ToArray().Length < limitTotalNumberOfPlayersOnTeam;
        }

        public void SetPlayerDataWithServerAuth(PlayerData playerData)
        {
            if (!IsServer) { Debug.LogError("SetPlayerDataWithServerAuth should only be called on the server!"); return; }

            int index = playerDataList.IndexOf(playerData);
            if (index == -1) { return; }

            playerDataList[index] = playerData;
        }

        public void SetPlayerData(PlayerData playerData)
        {
            int index = playerDataList.IndexOf(playerData);
            if (index == -1) { return; }

            if (!CanPlayersChangeTeams(playerData.team))
            {
                if (playerData.team != playerDataList[index].team) { return; }
            }
            
            if (IsServer)
            {
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
            RefreshStatus();
            _singleton = this;
            playerDataList = new NetworkList<PlayerData>();
            disconnectedPlayerDataList = new NetworkList<DisconnectedPlayerData>();

            List<int> initialChannelCounts = new List<int>();
            for (int i = 0; i < maxChannels; i++)
            {
                initialChannelCounts.Add(0);
            }

            channelCounts = new NetworkList<int>(initialChannelCounts);
        }

        private void Start()
        {
            StartCoroutine(LoadScriptableObjects());
        }

        private IEnumerator LoadScriptableObjects()
        {
            if (!CharacterReferenceHandle.IsValid()) { CharacterReferenceHandle = characterReferenceAddressable.LoadAssetAsync<CharacterReference>(); }
            yield return new WaitUntil(() => CharacterReferenceHandle.IsDone);
            if (!ControlsImageMappingHandle.IsValid()) { ControlsImageMappingHandle = controlsImageMappingAddressable.LoadAssetAsync<ControlsImageMapping>(); }
            yield return new WaitUntil(() => ControlsImageMappingHandle.IsDone);
        }

        private void OnEnable()
        {
            RefreshStatus();

            EventDelegateManager.sceneLoaded += OnSceneLoad;
            EventDelegateManager.sceneUnloaded += OnSceneUnload;
            EventDelegateManager.clientFinishedLoadingScenes += OnClientFinishedLoadingScenes;

            NetworkManager.OnClientConnectedCallback += OnClientConnectCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        private void OnDisable()
        {
            EventDelegateManager.sceneLoaded -= OnSceneLoad;
            EventDelegateManager.sceneUnloaded -= OnSceneUnload;
            EventDelegateManager.clientFinishedLoadingScenes -= OnClientFinishedLoadingScenes;

            if (NetworkManager)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnectCallback;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        public SpawnPoints.TransformData[] GetEnvironmentViewPoints()
        {
            if (playerSpawnPoints)
            {
                return playerSpawnPoints.GetEnvironmentViewPoints();
            }
            else
            {
                Debug.LogWarning("Trying to access environment view points when there is no player spawn points object");
                return new SpawnPoints.TransformData[0];
            }
        }

        public SpawnPoints.TransformData[] GetGameItemSpawnPoints()
        {
            if (playerSpawnPoints)
            {
                float distanceThreshold = 8;
                List<SpawnPoints.TransformData> possibleSpawnPoints = new List<SpawnPoints.TransformData>();
                List<Attributes> localPlayerList = localPlayers.Values.ToList();
                foreach (SpawnPoints.TransformData transformData in playerSpawnPoints.GetGameItemSpawnPoints())
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
                return new SpawnPoints.TransformData[0];
            }
        }

        public bool HasPlayerSpawnPoints() { return playerSpawnPoints; }

        public SpawnPoints GetPlayerSpawnPoints() { return playerSpawnPoints; }

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

        private SpawnPoints playerSpawnPoints;
        void OnSceneLoad(Scene scene)
        {
            foreach (GameObject g in scene.GetRootGameObjects())
            {
                if (g.TryGetComponent(out playerSpawnPoints)) { break; }
            }

            // Need to check singleton since this object may be despawned
            if (NetworkManager.Singleton.IsServer)
            {
                if (NetSceneManager.Singleton.ShouldSpawnPlayerCached)
                {
                    for (int i = 0; i < playerDataList.Count; i++)
                    {
                        if (playerDataList[i].id < 0)
                        {
                            AddPlayerToSpawnQueue(playerDataList[i]);
                        }
                    }
                }

                if (scene.name == "Lobby")
                {
                    disconnectedPlayerDataList.Clear();
                    if (GameModeManager.Singleton)
                    {
                        GameModeManager.Singleton.ClearDisconnectedScoreList();
                    }
                }
            }
        }

        private void AddPlayerToSpawnQueue(PlayerData playerData)
        {
            //Debug.Log("Adding player to spawn queue " + playerData.character.name.ToString());
            playersToSpawnQueue.Enqueue(playerData);
        }

        private void OnClientFinishedLoadingScenes(ulong clientId)
        {
            StartCoroutine(WaitForPlayerDataToAddPlayerToSpawnQueue(clientId));
        }

        private IEnumerator WaitForPlayerDataToAddPlayerToSpawnQueue(ulong clientId)
        {
            float waitTime = 0;
            while (true)
            {
                if (ContainsId((int)clientId)) { break; }

                // Time out the player if they take too long
                if (NetworkManager.LocalClientId != NetworkManager.ServerClientId)
                {
                    waitTime += Time.unscaledDeltaTime;
                    if (waitTime > 120)
                    {
                        Debug.LogWarning("Timed out while waiting for player data after scene loading completed on client " + clientId);
                        if (NetworkManager.ConnectedClientsIds.Contains(clientId))
                        {
                            NetworkManager.DisconnectClient((ulong)clientId, "Timed out while spawning player.");
                        }
                        yield break;
                    }
                }
                
                yield return null;
            }
            AddPlayerToSpawnQueue(GetPlayerData((int)clientId));
        }

        void OnSceneUnload()
        {
            if (IsServer)
            {
                if (!NetSceneManager.Singleton.ShouldSpawnPlayerCached)
                {
                    foreach (CombatAgent combatAgent in GetActiveCombatAgents())
                    {
                        if (combatAgent.IsSpawned) { combatAgent.NetworkObject.Despawn(true); }
                        else
                        {
                            Debug.LogError("Unsure how to handle despawned combat agent on scene unload " + combatAgent);
                        }
                    }

                    foreach (NetworkObject spectator in localSpectators.Values.ToList())
                    {
                        if (spectator.IsSpawned) { spectator.Despawn(true); }
                        else
                        {
                            Debug.LogError("Unsure how to handle despawned spectator on scene unload " + spectator);
                        }
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
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!playerSpawnPoints)
            {
                if (NetSceneManager.IsEnvironmentLoaded())
                {
                    playerSpawnPoints = FindFirstObjectByType<SpawnPoints>();
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            playerDataList.OnListChanged += OnPlayerDataListChange;
            gameMode.OnValueChanged += OnGameModeChange;
            teamNameOverridesJson.OnValueChanged += OnTeamNameOverridesJsonChange;

            if (IsServer)
            {
                NetworkManager.NetworkTickSystem.Tick += ServerTick;
                playerDataList.Clear();
            }
            SyncCachedPlayerDataList();

            if (IsOwner)
            {
                RefreshStatus();
            }
        }

        public override void OnNetworkDespawn()
        {
            playerDataList.OnListChanged -= OnPlayerDataListChange;
            gameMode.OnValueChanged -= OnGameModeChange;
            teamNameOverridesJson.OnValueChanged -= OnTeamNameOverridesJsonChange;

            if (IsServer)
            {
                NetworkManager.NetworkTickSystem.Tick -= ServerTick;
            }

            localPlayers.Clear();
            botClientId = 0;
            SyncCachedPlayerDataList();
        }

        private void SyncCachedPlayerDataList()
        {
            cachedPlayerDataList.Clear();
            cachedIdList.Clear();
            foreach (PlayerData playerData in playerDataList)
            {
                cachedIdList.Add(playerData.id);
                cachedPlayerDataList.Add(playerData);
            }
        }

        private Queue<PlayerData> playersToSpawnQueue = new Queue<PlayerData>();
        private void ServerTick()
        {
            if (!NetSceneManager.IsBusyLoadingScenes())
            {
                if (playersToSpawnQueue.Count > 0 & !spawnPlayerRunning)
                {
                    if (NetSceneManager.GetShouldSpawnPlayer())
                    {
                        spawnPlayerCoroutine = StartCoroutine(SpawnPlayer(playersToSpawnQueue.Dequeue()));
                    }
                    else
                    {
                        //Debug.Log("Clearing spawn queue");
                        playersToSpawnQueue.Clear();
                    }
                }
            }
            
            if (Time.time - lastSpawnPlayerStartTime > spawnPlayerTimeoutThreshold & spawnPlayerRunning)
            {
                EndSpawnPlayerCoroutine();
            }
        }

        private void OnPlayerDataListChange(NetworkListEvent<PlayerData> networkListEvent)
        {
            SyncCachedPlayerDataList();

            if ((int)NetworkManager.LocalClientId == networkListEvent.Value.id) { LocalPlayerData = networkListEvent.Value; }

            switch (networkListEvent.Type)
            {
                case NetworkListEvent<PlayerData>.EventType.Add:
                    if (IsServer)
                    {
                        // Spawn bots
                        if (NetSceneManager.Singleton.ShouldSpawnPlayerCached & networkListEvent.Value.id < 0)
                        {
                            AddPlayerToSpawnQueue(networkListEvent.Value);
                        }

                        KeyValuePair<bool, PlayerData> kvp = GetLobbyLeader();
                        StartCoroutine(WebRequestManager.Singleton.ServerManager.UpdateServerPopulation(GetPlayerDataListWithSpectators().Count(item => item.id >= 0),
                            kvp.Key ? kvp.Value.character.name.ToString() : GetGameModeString(GetGameMode()),
                            kvp.Key ? kvp.Value.character._id.ToString() : ""));

                        channelCounts[networkListEvent.Value.channel]++;

                        foreach (Attributes player in localPlayers.Values)
                        {
                            if (player)
                            {
                                player.UpdateNetworkVisiblity();
                            }
                        }
                    }

                    if (networkListEvent.Value.id >= 0)
                    {
                        StartCoroutine(WebRequestManager.Singleton.CharacterManager.GetCharacterAttributes(networkListEvent.Value.character._id.ToString()));
                    }
                    break;
                case NetworkListEvent<PlayerData>.EventType.Insert:
                    break;
                case NetworkListEvent<PlayerData>.EventType.Remove:
                case NetworkListEvent<PlayerData>.EventType.RemoveAt:
                    if (IsServer)
                    {
                        KeyValuePair<bool, PlayerData> kvp = GetLobbyLeader();
                        StartCoroutine(WebRequestManager.Singleton.ServerManager.UpdateServerPopulation(GetPlayerDataListWithSpectators().Count(item => item.id >= 0),
                            kvp.Key ? kvp.Value.character.name.ToString() : GetGameModeString(GetGameMode()),
                            kvp.Key ? kvp.Value.character._id.ToString() : ""));

                        // If there is a local player for this id, despawn it
                        if (localPlayers.ContainsKey(networkListEvent.Value.id)) { localPlayers[networkListEvent.Value.id].NetworkObject.Despawn(true); }

                        channelCounts[networkListEvent.Value.channel]--;

                        foreach (Attributes player in localPlayers.Values)
                        {
                            if (player) { player.UpdateNetworkVisiblity(); }
                        }
                    }
                    break;
                case NetworkListEvent<PlayerData>.EventType.Value:
                    if (localPlayers.ContainsKey(networkListEvent.Value.id))
                    {
                        bool waitForRespawn = GetGameMode() != GameMode.None;
                        if (GameModeManager.Singleton) { waitForRespawn &= !GameModeManager.Singleton.ShouldDisplayNextGameAction(); }

                        LoadoutManager loadoutManager = localPlayers[networkListEvent.Value.id].LoadoutManager;
                        loadoutManager.ApplyLoadout(networkListEvent.Value.character.raceAndGender, networkListEvent.Value.character.GetActiveLoadout(), networkListEvent.Value.character._id.ToString(), waitForRespawn);

                        localPlayers[networkListEvent.Value.id].SetCachedPlayerData(networkListEvent.Value);
                    }

                    if (IsServer)
                    {
                        if (networkListEvent.PreviousValue.channel != networkListEvent.Value.channel)
                        {
                            channelCounts[networkListEvent.PreviousValue.channel]--;
                            channelCounts[networkListEvent.Value.channel]++;

                            foreach (Attributes player in localPlayers.Values)
                            {
                                if (player) { player.UpdateNetworkVisiblity(); }
                            }
                        }
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

        public PlayerData LocalPlayerData { get; private set; }

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
            (bool spawnPointFound, SpawnPoints.TransformData transformData) = playerSpawnPoints.GetRespawnOrientation(gameMode.Value, attributesToRespawn.GetTeam(), attributesToRespawn);
            if (attributesToRespawn.GetTeam() != Team.Peaceful & attributesToRespawn.GetTeam() != Team.Spectator)
            {
                float waitTime = 0;
                while (!spawnPointFound)
                {
                    attributesToRespawn.isWaitingForSpawnPoint = true;
                    (spawnPointFound, transformData) = playerSpawnPoints.GetRespawnOrientation(gameMode.Value, attributesToRespawn.GetTeam(), attributesToRespawn);
                    yield return null;
                    waitTime += Time.unscaledDeltaTime;
                    if (waitTime > maxSpawnPointWaitTime) { break; }
                }
            }
            
            attributesToRespawn.isWaitingForSpawnPoint = false;

            Vector3 spawnPosition = transformData.position;
            Quaternion spawnRotation = transformData.rotation;

            attributesToRespawn.ResetStats(1, true, true, false);
            attributesToRespawn.AnimationHandler.CancelAllActions(0, true);
            attributesToRespawn.MovementHandler.SetOrientation(spawnPosition, spawnRotation);
            attributesToRespawn.LoadoutManager.SwapLoadoutOnRespawn();
        }

        [SerializeField] private AudioClip reviveAudioClip;
        public void RevivePlayer(Attributes attributesToRevive)
        {
            if (!IsServer) { Debug.LogError("PlayerDataManager.RevivePlayer() should only be called on the server!"); return; }

            attributesToRevive.ResetStats(0.5f, true, true, false);
            attributesToRevive.AnimationHandler.CancelAllActions(0, true);
            PlayReviveEffectsRpc(new NetworkObjectReference(attributesToRevive.NetworkObject));
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
        private void PlayReviveEffectsRpc(NetworkObjectReference networkObjectReference)
        {
            if (networkObjectReference.TryGet(out NetworkObject networkObject, NetworkManager))
            {
                if (networkObject.TryGetComponent(out Attributes attributesToRevive))
                {
                    AudioManager.Singleton.PlayClipAtPoint(attributesToRevive.gameObject, reviveAudioClip, attributesToRevive.MovementHandler.GetPosition(), 0.7f);
                    attributesToRevive.SessionProgressionHandler.LevelUpVisualEffect.Play();
                }
                else
                {
                    Debug.LogWarning("Couldn't find attributes component on network object for revive effects");
                }
            }
        }

        public void RespawnAllPlayers()
        {
            foreach (KeyValuePair<int, Attributes> kvp in localPlayers)
            {
                kvp.Value.StopRespawnSelfCoroutine();
                AddPlayerToSpawnQueue(GetPlayerData(kvp.Key));
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
                    ObjectPoolingManager.ReturnObjectToPool(playerObjectToSpawn.GetComponent<PooledObject>());
                    playerObjectToSpawn = null;
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
            Debug.Log("Spawning player object for " + playerData.character.name.ToString());
            if (playerData.id >= 0)
            {
                // TODO Add a timeout here
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
                (bool spawnPointFound, SpawnPoints.TransformData transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team, playerData.channel);
                if (playerData.team != Team.Peaceful & playerData.team != Team.Spectator)
                {
                    float waitTime = 0;
                    while (!spawnPointFound)
                    {
                        isWaitingForSpawnPoint.Value = true;
                        (spawnPointFound, transformData) = playerSpawnPoints.GetSpawnOrientation(gameMode.Value, playerData.team, playerData.channel);
                        yield return null;
                        waitTime += Time.unscaledDeltaTime;
                        if (waitTime > maxSpawnPointWaitTime) { break; }
                    }
                    isWaitingForSpawnPoint.Value = false;
                }
                spawnPosition = transformData.position;
                spawnRotation = transformData.rotation;
            }
            else
            {
                Debug.LogError("Trying to spawn player without a player spawn points object!");
            }

            if (!ContainsId(playerData.id))
            {
                spawnPlayerRunning = false;
                yield break;
            }

            bool isSpectator = playerData.team == Team.Spectator;
            if (isSpectator)
            {
                playerObjectToSpawn = ObjectPoolingManager.SpawnObject(spectatorPrefab.GetComponent<PooledObject>(), spawnPosition, spawnRotation).gameObject;
            }
            else
            {
                if (playerData.id >= 0)
                    playerObjectToSpawn = ObjectPoolingManager.SpawnObject(GetCharacterReference().PlayerPrefab.GetComponent<PooledObject>(), spawnPosition, spawnRotation).gameObject;
                else
                    playerObjectToSpawn = ObjectPoolingManager.SpawnObject(GetCharacterReference().BotPrefab.GetComponent<PooledObject>(), spawnPosition, spawnRotation).gameObject;

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

            if (!isSpectator)
            {
                if (playerObjectToSpawn.TryGetComponent(out WeaponHandler weaponHandler))
                {
                    yield return new WaitUntil(() => weaponHandler.WeaponInitialized);
                }
                else
                {
                    Debug.LogWarning("Player object has no weapon handler component");
                }
            }

            Debug.Log("Finished spawning player object for " + playerData.character.name.ToString());

            playerObjectToSpawn = null;
            playerIdThatIsBeingSpawned = default;
            spawnPlayerRunning = false;
        }

        public bool WasDisconnectedByClient { get; set; }

        [SerializeField] private GameObject alertBoxPrefab;
        private void OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log("Id: " + clientId + " - Name: " + GetPlayerData((int)clientId).character.name + " has disconnected.");
            if (IsServer)
            {
                if (ContainsId((int)clientId))
                {
                    // If player
                    if (clientId >= 0)
                    {
                        PlayerData playerData = GetPlayerData((int)clientId);
                        CharacterManager.Loadout activeLoadout = playerData.character.GetActiveLoadout();

                        int loadoutSlotIndex = serverSideOriginalLoadouts.FindIndex(item => item.Item1 == playerData.id);
                        if (loadoutSlotIndex != -1)
                        {
                            if (serverSideOriginalLoadouts[loadoutSlotIndex].Item2 != activeLoadout.loadoutSlot)
                            {
                                PersistentLocalObjects.Singleton.StartCoroutine(ExecuteLoadoutSwap(playerData, activeLoadout));
                            }
                        }
                    }
                    RemovePlayerData((int)clientId);
                }
            }

            if (!NetworkManager.IsServer && NetworkManager.DisconnectReason != string.Empty)
            {
                Debug.Log($"Approval Declined Reason: {NetworkManager.DisconnectReason}");
            }

            if (IsClient)
            {
                if (!WasDisconnectedByClient)
                {
                    // This object gets despawned, so make sure to not start this on a networkobject
                    PersistentLocalObjects.Singleton.StartCoroutine(ReturnToCharacterSelectOnServerShutdown());
                }
            }
        }

        private IEnumerator ExecuteLoadoutSwap(PlayerData playerData, CharacterManager.Loadout loadout)
        {
            if (loadout.EqualsIgnoringSlot(CharacterManager.Loadout.GetEmptyLoadout()))
            {
                yield return WebRequestManager.Singleton.CharacterManager.UpdateCharacterLoadout(playerData.character._id.ToString(), loadout, false);
            }
            yield return WebRequestManager.Singleton.CharacterManager.UseCharacterLoadout(playerData.character._id.ToString(), loadout.loadoutSlot.ToString());
        }

        private IEnumerator ReturnToCharacterSelectOnServerShutdown()
        {
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
            yield return null;
            if (NetworkManager.Singleton.IsListening) { yield break; }
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

        private NetworkList<int> channelCounts;

        public const int defaultChannel = 0;
        private const int maxChannels = 5;
        private const int maxPlayersInChannel = 15;

        public List<int> GetChannelCountList()
        {
            List<int> channelCountsList = new List<int>();
            for (int i = 0; i < channelCounts.Count; i++)
            {
                channelCountsList.Add(channelCounts[i]);
            }
            return channelCountsList;
        }

        public int GetBestChannel()
        {
            if (GetGameMode() != GameMode.None) { return defaultChannel; }

            // Return first channel that has less than the max players in channel
            List<int> channelCounts = GetChannelCountList();
            for (int channelIndex = 0; channelIndex < channelCounts.Count; channelIndex++)
            {
                if (channelCounts[channelIndex] < maxPlayersInChannel) { return channelIndex; }
            }

            // Return channel with the lowest number of players in it
            int lowestCountIndex = channelCounts.FindIndex(item => item == channelCounts.Min());
            return lowestCountIndex == -1 ? defaultChannel : lowestCountIndex;
        }

        public void SetCharAttributes(int dataId, CharacterManager.CharacterAttributes newAttributes)
        {
            if (GetGameMode() != GameMode.None) { return; }
            if (!ContainsId(dataId)) { return; }

            if (IsServer)
            {
                PlayerData playerData = GetPlayerData(dataId);
                if (newAttributes.Equals(playerData.character.attributes)) { return; }
                CharacterManager.Character newChar = playerData.character;
                newChar.attributes = newAttributes;
                playerData.character = newChar;
                SetPlayerData(playerData);

                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.UpdateCharacterAttributes(playerData.character._id.ToString(),
                    playerData.character.attributes));

                GetCharAttributesRpc(playerData.character._id.ToString());
            }
            else
            {
                SetCharAttributesRpc(dataId, newAttributes);
            }
        }

        [Rpc(SendTo.NotServer)]
        private void GetCharAttributesRpc(string characterId)
        {
            PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.GetCharacterAttributes(characterId));
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetCharAttributesRpc(int dataId, CharacterManager.CharacterAttributes newAttributes)
        {
            SetCharAttributes(dataId, newAttributes);
        }

        public void SetCharAttributes(int dataId, CharacterManager.Character.AttributeType attributeType, int newValue)
        {
            if (GetGameMode() != GameMode.None) { return; }
            if (!ContainsId(dataId)) { return; }

            //PlayerData playerData = GetPlayerData(dataId);
            //if (WebRequestManager.Singleton.TryGetCharacterAttributesInLookup(playerData.character._id.ToString(), out var stats))
            //{
            //    if (stats.GetAvailableSkillPoints(playerData.character.attributes) <= 0) { return; }
            //}

            if (IsServer)
            {
                PlayerData playerData = GetPlayerData(dataId);
                CharacterManager.Character newChar = playerData.character.SetStat(attributeType, newValue);
                playerData.character = newChar;
                SetPlayerData(playerData);

                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.UpdateCharacterAttributes(playerData.character._id.ToString(),
                    playerData.character.attributes));

                GetCharAttributesRpc(playerData.character._id.ToString());
            }
            else
            {
                SetCharAttributesRpc(dataId, attributeType, newValue);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetCharAttributesRpc(int dataId, CharacterManager.Character.AttributeType attributeType, int newValue)
        {
            SetCharAttributes(dataId, attributeType, newValue);
        }

        private NetworkList<PlayerData> playerDataList;
        private List<PlayerData> cachedPlayerDataList = new List<PlayerData>();
        private List<int> cachedIdList = new List<int>();

        private NetworkList<DisconnectedPlayerData> disconnectedPlayerDataList;

        public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
        {
            public int id;
            public int channel;
            public CharacterManager.Character character;
            public Team team;

            public PlayerData(int id)
            {
                this.id = id;
                channel = defaultChannel;
                character = new();
                team = Team.Environment;
            }

            public PlayerData(int id, int channel, CharacterManager.Character character, Team team)
            {
                this.id = id;
                this.channel = channel;
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
                serializer.SerializeValue(ref channel);
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