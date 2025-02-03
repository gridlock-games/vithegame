using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Vi.Core.CombatAgents;
using Vi.Core.Structures;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core.GameModeManagers
{
    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Singleton { get { return _singleton; } }
        protected static GameModeManager _singleton;

        [SerializeField] private GameObject UIPrefab;

        [SerializeField] protected int numberOfRoundsWinsToWinGame = 2;
        [SerializeField] protected float roundDuration = 30;
        [SerializeField] private float nextGameActionDuration = 10;
        public bool DespawnProjectilesInBetweenRounds { get { return despawnProjectilesInBetweenRounds; } }
        [SerializeField] private bool despawnProjectilesInBetweenRounds = true;
        [Header("Leave respawn time as 0 to disable respawns during a round")]
        [SerializeField] private float respawnTime = 5;
        [SerializeField] protected RespawnType respawnType = RespawnType.Respawn;
        [SerializeField] private bool levelingEnabled = false;
        [SerializeField] private bool incapacitatedPlayerStateEnabled = false;

        public float RoundDuration { get { return roundDuration; } }

        public float RoundTimer { get { return roundTimer.Value; } }

        protected const float overtimeDuration = 20;

        public enum RespawnType
        {
            Respawn,
            DontRespawn,
            ResetStats,
            ResetHP,
            ResetHPAndArmor
        }

        public bool LevelingEnabled { get { return levelingEnabled; } }

        public bool IncapacitatedPlayerStateEnabled { get { return incapacitatedPlayerStateEnabled; } }

        public RespawnType GetRespawnType() { return respawnType; }

        public int GetNumberOfRoundsWinsToWinGame() { return numberOfRoundsWinsToWinGame; }

        public float GetRespawnTime() { return respawnTime; }

        protected NetworkVariable<float> roundTimer = new NetworkVariable<float>();
        private NetworkVariable<float> nextGameActionTimer = new NetworkVariable<float>();

        protected NetworkVariable<FixedString64Bytes> roundResultMessage = new NetworkVariable<FixedString64Bytes>();
        protected NetworkVariable<FixedString64Bytes> gameEndMessage = new NetworkVariable<FixedString64Bytes>();

        public bool IsInOvertime() { return overtime.Value; }
        protected NetworkVariable<bool> overtime = new NetworkVariable<bool>();

        public string GetRoundResultMessage() { return roundResultMessage.Value.ToString(); }
        public string GetGameEndMessage() { return gameEndMessage.Value.ToString(); }
        protected NetworkList<PlayerScore> scoreList;
        private NetworkList<DisconnectedPlayerScore> disconnectedScoreList;

        private List<int> gameItemSpawnIndexTracker = new List<int>();
        protected GameItem SpawnGameItem(GameItem gameItemPrefab)
        {
            if (!IsServer) { Debug.LogError("GameModeManager.SpawnGameItem() should only be called from the server!"); return null; }

            List<SpawnPoints.TransformData> possibleSpawnPoints = PlayerDataManager.Singleton.GetGameItemSpawnPoints().ToList();

            bool shouldResetSpawnTracker = false;
            foreach (int index in gameItemSpawnIndexTracker)
            {
                try
                {
                    possibleSpawnPoints.RemoveAt(index);
                }
                catch
                {
                    shouldResetSpawnTracker = true;
                    break;
                }
            }

            if (shouldResetSpawnTracker)
            {
                gameItemSpawnIndexTracker.Clear();
                possibleSpawnPoints = PlayerDataManager.Singleton.GetGameItemSpawnPoints().ToList();
            }
            else if (possibleSpawnPoints.Count == 0)
            {
                gameItemSpawnIndexTracker.Clear();
                possibleSpawnPoints = PlayerDataManager.Singleton.GetGameItemSpawnPoints().ToList();
            }

            int randomIndex = Random.Range(0, possibleSpawnPoints.Count);
            gameItemSpawnIndexTracker.Add(randomIndex);
            SpawnPoints.TransformData spawnPoint = new SpawnPoints.TransformData();
            if (possibleSpawnPoints.Count != 0)
                spawnPoint = possibleSpawnPoints[randomIndex];
            else
                Debug.LogError("Possible spawn point count is 0! Game item - " + gameItemPrefab);

            GameItem gameItemInstance = ObjectPoolingManager.SpawnObject(gameItemPrefab.GetComponent<PooledObject>(), spawnPoint.position, spawnPoint.rotation).GetComponent<GameItem>();
            gameItemInstance.NetworkObject.Spawn(true);
            return gameItemInstance;
        }

        protected Mob SpawnMob(Mob mobPrefab, PlayerDataManager.Team team, bool useGenericSpawnPoint)
        {
            SpawnPoints.TransformData transformData = useGenericSpawnPoint ? PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetGenericMobSpawnPoint() : PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetMobSpecificSpawnPoint(mobPrefab, team);

            Mob mob = ObjectPoolingManager.SpawnObject(mobPrefab.GetComponent<PooledObject>(), transformData.position, transformData.rotation).GetComponent<Mob>();
            mob.SetTeam(team);
            mob.NetworkObject.Spawn(true);
            return mob.GetComponent<Mob>();
        }

        [SerializeField] private Sprite environmentKillFeedIcon;
        public struct KillHistoryElement : INetworkSerializable, System.IEquatable<KillHistoryElement>
        {
            public FixedString64Bytes killerName;
            public PlayerDataManager.Team killerTeam;
            public ulong killerNetObjId;

            public FixedString64Bytes assistName;
            public PlayerDataManager.Team assistTeam;
            public ulong assistNetObjId;

            public FixedString64Bytes victimName;
            public PlayerDataManager.Team victimTeam;
            public ulong victimNetObjId;

            public FixedString64Bytes weaponName;
            public KillType killType;

            public KillHistoryElement(CombatAgent killer, CombatAgent victim)
            {
                killerName = PlayerDataManager.Singleton.GetTeamPrefix(killer.GetTeam()) + killer.GetName();
                killerTeam = killer.GetTeam();
                killerNetObjId = killer.NetworkObjectId;

                assistName = "";
                assistTeam = PlayerDataManager.Team.Environment;
                assistNetObjId = 0;

                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victim.GetTeam()) + victim.GetName();
                victimTeam = victim.GetTeam();
                victimNetObjId = victim.NetworkObjectId;

                weaponName = killer.WeaponHandler.GetWeapon().name.Replace("(Clone)", "");
                killType = KillType.Player;
            }

            public KillHistoryElement(CombatAgent killer, CombatAgent assist, CombatAgent victim)
            {
                killerName = PlayerDataManager.Singleton.GetTeamPrefix(killer.GetTeam()) + killer.GetName();
                killerTeam = killer.GetTeam();
                killerNetObjId = killer.NetworkObjectId;

                assistName = PlayerDataManager.Singleton.GetTeamPrefix(assist.GetTeam()) + assist.GetName();
                assistTeam = assist.GetTeam();
                assistNetObjId = assist.NetworkObjectId;

                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victim.GetTeam()) + victim.GetName();
                victimTeam = victim.GetTeam();
                victimNetObjId = victim.NetworkObjectId;

                weaponName = killer.WeaponHandler.GetWeapon().name.Replace("(Clone)", "");
                killType = KillType.PlayerWithAssist;
            }

            public KillHistoryElement(CombatAgent victim)
            {
                killerName = "";
                killerTeam = PlayerDataManager.Team.Environment;
                killerNetObjId = 0;

                assistName = "";
                assistTeam = PlayerDataManager.Team.Environment;
                assistNetObjId = 0;

                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victim.GetTeam()) + victim.GetName();
                victimTeam = victim.GetTeam();
                victimNetObjId = victim.NetworkObjectId;

                weaponName = "Environment";
                killType = KillType.Environment;
            }

            public KillHistoryElement(KillType killType)
            {
                this.killType = killType;
                killerNetObjId = 0;
                assistNetObjId = 0;
                victimNetObjId = 0;
                killerTeam = PlayerDataManager.Team.Competitor;
                assistTeam = PlayerDataManager.Team.Competitor;
                victimTeam = PlayerDataManager.Team.Competitor;
                var weaponOptions = PlayerDataManager.IsCharacterReferenceLoaded() ? PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions() : null;
                switch (killType)
                {
                    case KillType.Player:
                        killerName = "Killer";
                        assistName = "";
                        victimName = "Victim";
                        weaponName = weaponOptions == null ? "Weapon" : weaponOptions[Random.Range(0, weaponOptions.Length - 1)].weapon.name;
                        return;
                    case KillType.PlayerWithAssist:
                        killerName = "Killer";
                        assistName = "Assist";
                        victimName = "Victim";
                        weaponName = weaponOptions == null ? "Weapon" : weaponOptions[Random.Range(0, weaponOptions.Length - 1)].weapon.name;
                        return;
                    case KillType.Environment:
                        killerName = "";
                        assistName = "";
                        victimName = "Victim";
                        weaponName = "Environment";
                        return;
                    default:
                        Debug.LogError("Unsure how to handle kill type" + killType);
                        break;
                }
                killerName = "";
                assistName = "";
                victimName = "";
                weaponName = "";
            }

            public Sprite GetKillFeedIcon(KillHistoryElement killHistoryElement)
            {
                if (killType == KillType.Player | killType == KillType.PlayerWithAssist)
                {
                    Sprite killFeedIcon = null;
                    if (PlayerDataManager.IsCharacterReferenceLoaded())
                    {
                        CharacterReference.WeaponOption weaponOption = System.Array.Find(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions(), item => item.weapon.name == killHistoryElement.weaponName.ToString());
                        if (weaponOption != null) { killFeedIcon = weaponOption.killFeedIcon; }
                    }
                    return killFeedIcon ? killFeedIcon : PlayerDataManager.Singleton.DefaultEnvironmentKillIcon;
                }
                else if (killType == KillType.Environment)
                    return Singleton ? Singleton.environmentKillFeedIcon : PlayerDataManager.Singleton.DefaultEnvironmentKillIcon;
                else
                    Debug.LogError("Not sure what icon to provide for kill type: " + killHistoryElement.killType);
                return null;
            }

            public enum KillType
            {
                Player,
                PlayerWithAssist,
                Environment
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref killerName);
                serializer.SerializeValue(ref killerTeam);
                serializer.SerializeValue(ref killerNetObjId);

                serializer.SerializeValue(ref assistName);
                serializer.SerializeValue(ref assistTeam);
                serializer.SerializeValue(ref assistNetObjId);

                serializer.SerializeValue(ref victimName);
                serializer.SerializeValue(ref victimTeam);
                serializer.SerializeValue(ref victimNetObjId);

                serializer.SerializeValue(ref weaponName);
                serializer.SerializeValue(ref killType);
            }

            public bool Equals(KillHistoryElement other)
            {
                return killerName == other.killerName & victimName == other.victimName & weaponName == other.weaponName & killType == other.killType;
            }
        }

        private NetworkList<KillHistoryElement> killHistory;

        public List<KillHistoryElement> GetKillHistory()
        {
            List<KillHistoryElement> killHistoryList = new List<KillHistoryElement>();
            for (int i = 0; i < killHistory.Count; i++)
            {
                killHistoryList.Add(killHistory[i]);
            }
            return killHistoryList;
        }

        public virtual void OnDamageOccuring(CombatAgent attacker, CombatAgent victim, float HPDamage)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                if (victim.ExcludeFromKillFeed) { return; }

                if (attacker is Attributes attackerAttributes)
                {
                    int attackerIndex = scoreList.IndexOf(new PlayerScore(attackerAttributes.GetPlayerDataId()));
                    PlayerScore attackerScore = scoreList[attackerIndex];
                    attackerScore.cumulativeDamageDealt += HPDamage;
                    attackerScore.damageDealtThisRound += HPDamage;
                    scoreList[attackerIndex] = attackerScore;
                }

                if (victim is Attributes victimAttributes)
                {
                    int victimIndex = scoreList.IndexOf(new PlayerScore(victimAttributes.GetPlayerDataId()));
                    PlayerScore victimScore = scoreList[victimIndex];
                    victimScore.cumulativeDamageRecieved += HPDamage;
                    victimScore.damageRecievedThisRound += HPDamage;
                    scoreList[victimIndex] = victimScore;
                }
            }
        }

        public virtual void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            if (gameOver.Value) { return; }
            if (nextGameActionTimer.Value <= 0)
            {
                if (victim.ExcludeFromKillFeed) { return; }

                if (killer.Master)
                {
                    killer = killer.Master;
                }

                if (killer is Attributes killerAttributes)
                {
                    int killerIndex = scoreList.IndexOf(new PlayerScore(killerAttributes.GetPlayerDataId()));
                    PlayerScore killerScore = scoreList[killerIndex];
                    killerScore.cumulativeKills += 1;
                    killerScore.killsThisRound += 1;
                    scoreList[killerIndex] = killerScore;

                    if (killerAttributes.NetworkObject.IsPlayerObject)
                    {
                        killerAttributes.SessionProgressionHandler.AddEssence();
                    }
                }

                if (victim is Attributes victimAttributes)
                {
                    int victimIndex = scoreList.IndexOf(new PlayerScore(victimAttributes.GetPlayerDataId()));
                    PlayerScore victimScore = scoreList[victimIndex];
                    victimScore.cumulativeDeaths += 1;
                    victimScore.deathsThisRound += 1;
                    scoreList[victimIndex] = victimScore;
                }

                // Damage is in negative numbers
                CombatAgent assist = victim.GetDamageMappingThisLife().Where(item => item.Key != killer & item.Key != victim & item.Value > minAssistDamage).OrderByDescending(item => item.Value).FirstOrDefault().Key;
                if (assist)
                {
                    if (assist is Attributes assistAttributes)
                    {
                        int assistIndex = scoreList.IndexOf(new PlayerScore(assistAttributes.GetPlayerDataId()));
                        PlayerScore assistScore = scoreList[assistIndex];
                        assistScore.cumulativeAssists += 1;
                        assistScore.assistsThisRound += 1;
                        scoreList[assistIndex] = assistScore;

                        if (assistAttributes.NetworkObject.IsPlayerObject)
                        {
                            assistAttributes.SessionProgressionHandler.AddEssence();
                        }
                    }
                    killHistory.Add(new KillHistoryElement(killer, assist, victim));
                }
                else
                {
                    killHistory.Add(new KillHistoryElement(killer, victim));
                }

                if (LevelingEnabled)
                {
                    foreach (KeyValuePair<CombatAgent, float> kvp in victim.GetDamageMappingThisLife())
                    {
                        if (!kvp.Key) { continue; }
                        kvp.Key.SessionProgressionHandler.AddExperience(kvp.Value * experienceDamageAwardMultiplier);
                    }
                }
            }
        }

        private const float minAssistDamage = 30;
        private const float experienceDamageAwardMultiplier = 1;

        public virtual void OnEnvironmentKill(CombatAgent victim)
        {
            if (gameOver.Value) { return; }
            if (nextGameActionTimer.Value <= 0)
            {
                if (victim.ExcludeFromKillFeed) { return; }

                if (victim is Attributes victimAttributes)
                {
                    int victimIndex = scoreList.IndexOf(new PlayerScore(victimAttributes.GetPlayerDataId()));
                    PlayerScore victimScore = scoreList[victimIndex];
                    victimScore.cumulativeDeaths += 1;
                    victimScore.deathsThisRound += 1;
                    scoreList[victimIndex] = victimScore;
                }

                killHistory.Add(new KillHistoryElement(victim));
            }
        }

        public virtual void OnStructureKill(CombatAgent killer, Structure structure)
        {
            if (gameOver.Value) { return; }
        }

        public PlayerScore GetMVPScore() { return MVPScore.Value; }

        protected NetworkVariable<bool> gameOver = new NetworkVariable<bool>();
        protected NetworkVariable<PlayerScore> MVPScore = new NetworkVariable<PlayerScore>();
        protected virtual void OnGameEnd(int[] winningPlayersDataIds)
        {
            gameOver.Value = true;
            gameEndMessage.Value = "Returning to Lobby";

            foreach (int playerDataId in winningPlayersDataIds)
            {
                this.winningPlayerDataIds.Add(playerDataId);
            }

            if (PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams.Length > 1)
            {
                List<PlayerScore> highestKillPlayers = GetHighestKillPlayersCumulative(winningPlayersDataIds);
                if (highestKillPlayers.Count > 1)
                {
                    float highestDamage = highestKillPlayers.Max(item => item.cumulativeDamageDealt);
                    MVPScore.Value = highestKillPlayers.Find(item => item.cumulativeDamageDealt == highestDamage);
                }
                else if (highestKillPlayers.Count == 1) // If there is only 1 entry in the list
                {
                    MVPScore.Value = highestKillPlayers[0];
                }
                else
                {
                    Debug.LogError("Couldn't find an MVP!");
                }
            }
        }

        public List<int> GetGameWinnerIds()
        {
            if (!IsGameOver()) { Debug.LogWarning("Calling GetGameWinnerIds when the game isn't over yet!"); return new List<int>(); }

            List<int> returnedList = new List<int>();
            foreach (int id in winningPlayerDataIds)
            {
                returnedList.Add(id);
            }
            return returnedList;
        }

        private NetworkList<int> winningPlayerDataIds;
        public float ExpEarnedFromMatch { get; private set; } = -1;
        public int TokensEarnedFromMatch { get; private set; }
        protected virtual void OnGameOverChanged(bool prev, bool current)
        {
            if (!current) { return; }

            if (FasterPlayerPrefs.IsAutomatedClient)
            {
                ReturnToHub();
                return;
            }

            StartCoroutine(AwardExpBasedOnWin());

            if (IsClient)
            {
                if (PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator)
                {
                    PlayerScore localPlayerScore = GetPlayerScore(PlayerDataManager.Singleton.LocalPlayerData.id);

                    PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.SendKillsLeaderboardResult(
                        PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(),
                        PlayerDataManager.Singleton.LocalPlayerData.character.name.ToString(),
                        PlayerDataManager.Singleton.GetGameMode(),
                        localPlayerScore.cumulativeKills, localPlayerScore.cumulativeDeaths, localPlayerScore.cumulativeAssists));

                    ExpEarnedFromMatch += localPlayerScore.GetExpReward();

                    // TODO Change this to use web requests on the server
                    if (GameModeManager.Singleton.LevelingEnabled)
                    {
                        KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                        if (kvp.Value)
                        {
                            TokensEarnedFromMatch = kvp.Value.SessionProgressionHandler.Essences;
                            FasterPlayerPrefs.Singleton.SetInt("Tokens", FasterPlayerPrefs.Singleton.GetInt("Tokens") + kvp.Value.SessionProgressionHandler.Essences);
                        }
                    }
                    else
                    {
                        TokensEarnedFromMatch = localPlayerScore.cumulativeKills + localPlayerScore.cumulativeAssists;
                        FasterPlayerPrefs.Singleton.SetInt("Tokens", FasterPlayerPrefs.Singleton.GetInt("Tokens")
                            + localPlayerScore.cumulativeKills
                            + localPlayerScore.cumulativeAssists);
                    }
                }
            }
        }

        private IEnumerator AwardExpBasedOnWin()
        {
            yield return new WaitUntil(() => GetGameWinnerIds().Count > 0);

            if (IsServer)
            {
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    if (playerData.id > 0)
                    {
                        PlayerScore playerScore = GetPlayerScore(playerData.id);
                        float expAward = playerScore.GetExpReward();
                        expAward += GetGameWinnerIds().Contains(PlayerDataManager.Singleton.LocalPlayerData.id) ? 20 : 12;
                        PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterExp(playerData.character._id.ToString(), expAward));
                    }
                }
            }

            if (IsClient)
            {
                if (PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator)
                {
                    float expToAward = GetGameWinnerIds().Contains(PlayerDataManager.Singleton.LocalPlayerData.id) ? 20 : 12;
                    ExpEarnedFromMatch += expToAward;
                }
            }
        }

        private void ReturnToHub()
        {
            Debug.Log("Returning to Hub on Game Over");

            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown); }

            NetSceneManager.Singleton.LoadScene("Character Select");
            PersistentLocalObjects.Singleton.StartCoroutine(ReturnToHubCoroutine());
        }

        private IEnumerator ReturnToHubCoroutine()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                PlayerDataManager.Singleton.WasDisconnectedByClient = true;
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            if (WebRequestManager.Singleton.HubServers.Length > 0)
            {
                yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData(WebRequestManager.Singleton.HubServers[0].ip, ushort.Parse(WebRequestManager.Singleton.HubServers[0].port), FasterPlayerPrefs.serverListenAddress);
                NetworkManager.Singleton.StartClient();
            }
        }

        private void EndGamePrematurely(string gameEndMessage)
        {
            gameOver.Value = true;
            this.gameEndMessage.Value = gameEndMessage;
            roundResultMessage.Value = "Game Over! ";
            nextGameActionTimer.Value = nextGameActionDuration;
        }

        public int GetRoundCount() { return roundCount.Value; }

        private NetworkVariable<int> roundCount = new NetworkVariable<int>();
        protected virtual void OnRoundStart()
        {
            for (int i = 0; i < scoreList.Count; i++)
            {
                PlayerScore playerScore = scoreList[i];
                playerScore.ResetRoundVariables();
                scoreList[i] = playerScore;
            }
            for (int i = 0; i < disconnectedScoreList.Count; i++)
            {
                FixedString64Bytes charId = disconnectedScoreList[i].characterId;
                PlayerScore playerScore = disconnectedScoreList[i].playerScore;
                playerScore.ResetRoundVariables();
                disconnectedScoreList[i] = new DisconnectedPlayerScore(charId, playerScore);
            }
            roundCount.Value++;
            killHistory.Clear();
        }

        protected virtual void OnRoundEnd(int[] winningPlayersDataIds)
        {
            SessionProgressionHandler.RemoveAllEssenceBuffStatuses();
            bool shouldEndGame = false;
            foreach (int id in winningPlayersDataIds)
            {
                int index = scoreList.IndexOf(new PlayerScore(id));

                if (index == -1)
                {
                    List<DisconnectedPlayerScore> cachedList = new List<DisconnectedPlayerScore>();
                    for (int i = 0; i < disconnectedScoreList.Count; i++)
                    {
                        cachedList.Add(disconnectedScoreList[i]);
                    }

                    int disconnectedIndex = cachedList.FindIndex(item => item.playerScore.id == id);
                    if (disconnectedIndex == -1) { continue; }

                    PlayerScore score = disconnectedScoreList[disconnectedIndex].playerScore;
                    FixedString64Bytes charId = disconnectedScoreList[disconnectedIndex].characterId;
                    score.roundWins += 1;
                    disconnectedScoreList[disconnectedIndex] = new DisconnectedPlayerScore(charId, score);

                    if (score.roundWins >= numberOfRoundsWinsToWinGame) { shouldEndGame = true; }
                }
                else
                {
                    PlayerScore score = scoreList[index];
                    score.roundWins += 1;
                    scoreList[index] = score;

                    if (score.roundWins >= numberOfRoundsWinsToWinGame) { shouldEndGame = true; }
                }
            }

            if (shouldEndGame)
            {
                OnGameEnd(winningPlayersDataIds);
            }
            else
            {
                overtime.Value = false;
            }

            nextGameActionTimer.Value = nextGameActionDuration;
        }

        public string GetRoundTimerDisplayString()
        {
            int roundTimerValue = Mathf.CeilToInt(roundTimer.Value);

            int minutes = roundTimerValue / 60;
            int seconds = roundTimerValue - (minutes * 60);

            // Add a 0 in front of a single digit second
            string secondsString = seconds.ToString();
            if (secondsString.Length == 1) { secondsString = "0" + secondsString; }

            if (overtime.Value)
                return "+" + minutes.ToString() + ":" + secondsString;
            else
                return minutes.ToString() + ":" + secondsString;
        }

        public bool ShouldDisplaySpecialNextGameActionMessage() { return ShouldDisplayNextGameAction() & nextGameActionTimer.Value <= 1 & !gameOver.Value; }

        public virtual bool ShouldDisplayNextGameAction() { return nextGameActionTimer.Value > 0; }
        public bool IsGameOver() { return gameOver.Value; }
        public bool ShouldDisplayNextGameActionTimer() { return nextGameActionTimer.Value <= nextGameActionDuration / 2; }
        public string GetNextGameActionTimerDisplayString() { return Mathf.Ceil(nextGameActionTimer.Value).ToString("F0"); }

        private GameObject UIInstance;
        public override void OnNetworkSpawn()
        {
            if (UIPrefab) { UIInstance = Instantiate(UIPrefab, transform); }
            _singleton = this;
            scoreList.OnListChanged += OnScoreListChange;
            nextGameActionTimer.OnValueChanged += OnNextGameActionTimerChange;
            if (IsServer)
            {
                roundTimer.OnValueChanged += OnRoundTimerChange;
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    AddPlayerScore(playerData.id, playerData.character._id);
                }
                //roundTimer.Value = roundDuration;
                nextGameActionTimer.Value = nextGameActionDuration / 2;
            }
            gameOver.OnValueChanged += OnGameOverChanged;
        }

        public override void OnNetworkDespawn()
        {
            scoreList.OnListChanged -= OnScoreListChange;
            nextGameActionTimer.OnValueChanged -= OnNextGameActionTimerChange;
            if (IsServer)
            {
                roundTimer.OnValueChanged -= OnRoundTimerChange;
            }
            gameOver.OnValueChanged -= OnGameOverChanged;
        }

        public void AddPlayerScore(int id, FixedString64Bytes characterId)
        {
            if (!IsSpawned)
            {
                StartCoroutine(WaitForSpawnToAddPlayerData(id, characterId));
            }
            else
            {
                PlayerScore playerScore = new PlayerScore(id);
                if (scoreList.Contains(playerScore)) { Debug.LogError("Player score with id: " + id + " has already been added!"); return; }

                int index = disconnectedScoreList.IndexOf(new DisconnectedPlayerScore(characterId, playerScore));
                if (index == -1)
                {
                    scoreList.Add(playerScore);
                }
                else
                {
                    playerScore.killsThisRound = disconnectedScoreList[index].playerScore.killsThisRound;
                    playerScore.deathsThisRound = disconnectedScoreList[index].playerScore.deathsThisRound;
                    playerScore.roundWins = disconnectedScoreList[index].playerScore.roundWins;
                    scoreList.Add(playerScore);
                    disconnectedScoreList.RemoveAt(index);
                }
            }
        }

        private IEnumerator WaitForSpawnToAddPlayerData(int id, FixedString64Bytes characterId)
        {
            yield return new WaitUntil(() => IsSpawned);
            AddPlayerScore(id, characterId);
        }

        public PlayerScore GetPlayerScore(int id)
        {
            int index = scoreList.IndexOf(new PlayerScore(id));
            if (index == -1) { Debug.LogError("Could not find player score with id: " + id); return new PlayerScore(); }
            return scoreList[index];
        }

        public PlayerScore GetDisconnectedPlayerScore(int id)
        {
            for (int i = 0; i < disconnectedScoreList.Count; i++)
            {
                DisconnectedPlayerScore disconnectedPlayerScore = disconnectedScoreList[i];
                if (disconnectedPlayerScore.playerScore.id == id) { return disconnectedPlayerScore.playerScore; }
            }
            return new PlayerScore();
        }

        public void RemovePlayerScore(int id, FixedString64Bytes characterId)
        {
            int index = scoreList.IndexOf(new PlayerScore(id));
            if (index == -1) { Debug.LogError("Trying to remove score from list, but can't find it for id: " + id); return; }
            if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None) { disconnectedScoreList.Add(new DisconnectedPlayerScore(characterId, scoreList[index])); }
            scoreList.RemoveAt(index);
        }

        public void ClearDisconnectedScoreList() { disconnectedScoreList.Clear(); }

        private void OnRoundTimerChange(float prev, float current)
        {
            if (timerMode == TimerMode.CountDown)
            {
                if (Mathf.Approximately(current, roundDuration))
                {
                    OnRoundTimerStart();
                }
                else if (current <= 0 & prev > 0)
                {
                    OnRoundTimerEnd();
                }
            }
        }

        protected virtual void OnRoundTimerStart()
        {
            OnRoundStart();
        }

        protected virtual void OnRoundTimerEnd()
        {
            List<int> highestKillIdList = new List<int>();
            foreach (PlayerScore playerScore in GetHighestKillPlayersThisRound())
            {
                highestKillIdList.Add(playerScore.id);
            }
            OnRoundEnd(highestKillIdList.ToArray());
        }

        public bool ShouldFadeToBlack()
        {
            if (respawnType == RespawnType.Respawn)
            {
                return nextGameActionTimer.Value > nextGameActionDuration / 2 & nextGameActionDuration - nextGameActionTimer.Value > 3 & GetRoundCount() > 0 & !gameOver.Value;
            }
            else
            {
                return false;
            }
        }

        public bool WaitingToPlayGame() { return nextGameActionTimer.Value > 0; }

        private List<int> respawnsCalledByRoundCount = new List<int>();

        protected string roundAboutToStartPrefix = "Round ";
        protected string roundAboutToStartSuffix = " is About to Start ";

        protected virtual void OnNextGameActionTimerThreeFourths() { }

        protected virtual void OnNextGameActionTimerHalfway()
        {
            if (IsServer)
            {
                if (!respawnsCalledByRoundCount.Contains(GetRoundCount()))
                {
                    respawnsCalledByRoundCount.Add(GetRoundCount());
                    switch (respawnType)
                    {
                        case RespawnType.Respawn:
                            PlayerDataManager.Singleton.RespawnAllPlayers();
                            break;
                        case RespawnType.DontRespawn:
                            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                            {
                                if (attributes.GetAilment() == ActionClip.Ailment.Death)
                                {
                                    StartCoroutine(PlayerDataManager.Singleton.RespawnPlayer(attributes));
                                }
                                else
                                {
                                    attributes.LoadoutManager.SwapLoadoutOnRespawn();
                                }
                            }
                            break;
                        case RespawnType.ResetStats:
                            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                            {
                                if (attributes.GetAilment() == ActionClip.Ailment.Death)
                                {
                                    StartCoroutine(PlayerDataManager.Singleton.RespawnPlayer(attributes));
                                }
                                else
                                {
                                    attributes.ResetStats(1, true, true, false);
                                    attributes.LoadoutManager.SwapLoadoutOnRespawn();
                                }
                            }
                            break;
                        case RespawnType.ResetHP:
                            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                            {
                                if (attributes.GetAilment() == ActionClip.Ailment.Death)
                                {
                                    StartCoroutine(PlayerDataManager.Singleton.RespawnPlayer(attributes));
                                }
                                else
                                {
                                    attributes.ResetStats(1, false, false, false);
                                    attributes.LoadoutManager.SwapLoadoutOnRespawn();
                                }
                            }
                            break;
                        case RespawnType.ResetHPAndArmor:
                            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                            {
                                if (attributes.GetAilment() == ActionClip.Ailment.Death)
                                {
                                    StartCoroutine(PlayerDataManager.Singleton.RespawnPlayer(attributes));
                                }
                                else
                                {
                                    attributes.ResetStats(1, false, true, false);
                                    attributes.LoadoutManager.SwapLoadoutOnRespawn();
                                }
                            }
                            break;
                        default:
                            Debug.LogError("Unsure how to handle respawn type " + respawnType);
                            break;
                    }
                    roundResultMessage.Value = roundAboutToStartPrefix + (GetRoundCount() + 1).ToString() + roundAboutToStartSuffix;
                }
            }
        }

        protected virtual void OnNextGameActionTimerOneFourth()
        {

        }

        private void OnNextGameActionTimerChange(float prev, float current)
        {
            if (!gameOver.Value & GetRoundCount() > 0)
            {
                if (current <= nextGameActionDuration * 0.75f & prev > nextGameActionDuration * 0.75f)
                {
                    OnNextGameActionTimerThreeFourths();
                }

                if (current <= nextGameActionDuration / 2 & prev > nextGameActionDuration / 2)
                {
                    OnNextGameActionTimerHalfway();
                }

                if (current <= nextGameActionDuration * 0.25f & prev > nextGameActionDuration * 0.25f)
                {
                    OnNextGameActionTimerOneFourth();
                }
            }

            if (IsServer)
            {
                if (current == 0 & prev > 0)
                {
                    if (gameOver.Value)
                    {
                        if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None) { StartCoroutine(DisplayPostGameEvents()); }
                    }
                    else
                    {
                        switch (timerMode)
                        {
                            case TimerMode.CountDown:
                                roundTimer.Value = roundDuration;
                                break;
                            case TimerMode.CountUp:
                                OnRoundStart();
                                break;
                            default:
                                Debug.LogError("Unsure how to handle timer mode " + timerMode + " when next game action timer reaches 0!");
                                break;
                        }
                    }
                }
            }
        }

        public enum PostGameStatus
        {
            None,
            Rewards,
            MVP
        }

        public PostGameStatus GetPostGameStatus() { return postGameStatus.Value; }
        private NetworkVariable<PostGameStatus> postGameStatus = new NetworkVariable<PostGameStatus>(PostGameStatus.None);

        private IEnumerator DisplayPostGameEvents()
        {
            postGameStatus.Value = PostGameStatus.Rewards;
            yield return new WaitForSeconds(15);

            if (GetMVPScore().isValid)
            {
                // MVP Presentation
                postGameStatus.Value = PostGameStatus.MVP;
                yield return new WaitForSeconds(7.5f);
            }

            // Return to Lobby
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        protected List<PlayerScore> GetHighestKillPlayersThisRound()
        {
            List<PlayerScore> allPlayerScores = new List<PlayerScore>();
            for (int i = 0; i < scoreList.Count; i++)
            {
                allPlayerScores.Add(scoreList[i]);
            }
            for (int i = 0; i < disconnectedScoreList.Count; i++)
            {
                allPlayerScores.Add(disconnectedScoreList[i].playerScore);
            }

            List<PlayerScore> highestKillPlayerScores = new List<PlayerScore>();
            for (int i = 0; i < allPlayerScores.Count; i++)
            {
                PlayerScore playerScore = allPlayerScores[i];
                if (highestKillPlayerScores.Count > 0)
                {
                    if (playerScore.killsThisRound > highestKillPlayerScores[0].killsThisRound)
                    {
                        highestKillPlayerScores.Clear();
                        highestKillPlayerScores.Add(playerScore);
                    }
                    else if (playerScore.killsThisRound == highestKillPlayerScores[0].killsThisRound)
                    {
                        highestKillPlayerScores.Add(playerScore);
                    }
                }
                else
                {
                    highestKillPlayerScores.Add(playerScore);
                }
            }
            return highestKillPlayerScores;
        }

        private List<PlayerScore> GetHighestKillPlayersCumulative(int[] scoreIdsToPickFrom)
        {
            List<PlayerScore> allPlayerScores = new List<PlayerScore>();
            for (int i = 0; i < scoreList.Count; i++)
            {
                if (scoreIdsToPickFrom.Contains(scoreList[i].id)) { allPlayerScores.Add(scoreList[i]); }
            }
            for (int i = 0; i < disconnectedScoreList.Count; i++)
            {
                if (scoreIdsToPickFrom.Contains(scoreList[i].id)) { allPlayerScores.Add(disconnectedScoreList[i].playerScore); }
            }

            List<PlayerScore> highestKillPlayerScores = new List<PlayerScore>();
            for (int i = 0; i < allPlayerScores.Count; i++)
            {
                PlayerScore playerScore = allPlayerScores[i];
                if (highestKillPlayerScores.Count > 0)
                {
                    if (playerScore.cumulativeKills > highestKillPlayerScores[0].cumulativeKills)
                    {
                        highestKillPlayerScores.Clear();
                        highestKillPlayerScores.Add(playerScore);
                    }
                    else if (playerScore.cumulativeKills == highestKillPlayerScores[0].cumulativeKills)
                    {
                        highestKillPlayerScores.Add(playerScore);
                    }
                }
                else
                {
                    highestKillPlayerScores.Add(playerScore);
                }
            }
            return highestKillPlayerScores;
        }

        public virtual string GetLeftScoreString() { return string.Empty; }

        public virtual string GetRightScoreString() { return string.Empty; }

        protected void Awake()
        {
            scoreList = new NetworkList<PlayerScore>();
            disconnectedScoreList = new NetworkList<DisconnectedPlayerScore>();
            killHistory = new NetworkList<KillHistoryElement>();
            winningPlayerDataIds = new NetworkList<int>();

            foreach (string propertyString in PlayerDataManager.Singleton.GetGameModeSettings().Split("|"))
            {
                string[] propertySplit = propertyString.Split(":");
                string propertyName = "";
                int value = 0;
                for (int i = 0; i < propertySplit.Length; i++)
                {
                    if (i == 0)
                    {
                        propertyName = propertySplit[i];
                    }
                    else if (i == 1)
                    {
                        value = int.Parse(propertySplit[i]);
                    }
                    else
                    {
                        Debug.LogError("Not sure how to parse game mode property string " + propertyString);
                    }
                }

                if (string.IsNullOrWhiteSpace(propertyName)) { continue; }

                try
                {
                    GetType().GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, value);
                }
                catch
                {
                    Debug.LogError("Error while setting value for field: " + propertyName);
                }
            }
        }

        public UnityAction onScoreListChanged;

        private void OnScoreListChange(NetworkListEvent<PlayerScore> networkListEvent)
        {
            if (IsServer)
            {
                if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
                {
                    if (!gameOver.Value & !IsWaitingForPlayers)
                    {
                        if (scoreList.Count < PlayerDataManager.GetGameModeMinPlayers(PlayerDataManager.Singleton.GetGameMode()))
                        {
                            EndGamePrematurely("Returning to lobby due to having no opponents!");
                        }
                    }
                }
            }

            if (networkListEvent.Type == NetworkListEvent<PlayerScore>.EventType.Value)
            {
                if (respawnType == RespawnType.Respawn)
                {
                    scoresToEvaluate.RemoveAll(item => item.Item2.Equals(networkListEvent.Value));
                    scoresToEvaluate.Add((networkListEvent.PreviousValue.roundWins < networkListEvent.Value.roundWins, networkListEvent.Value));
                    if (!isEvaluatingRoundEndAnimations) { StartCoroutine(EvaluateRoundEndAnimations()); }
                }
                else if (gameOver.Value)
                {
                    scoresToEvaluate.RemoveAll(item => item.Item2.Equals(networkListEvent.Value));
                    scoresToEvaluate.Add((networkListEvent.PreviousValue.roundWins < networkListEvent.Value.roundWins, networkListEvent.Value));
                    if (!isEvaluatingRoundEndAnimations) { StartCoroutine(EvaluateRoundEndAnimations()); }
                }
            }

            onScoreListChanged?.Invoke();
        }

        private List<(bool, PlayerScore)> scoresToEvaluate = new List<(bool, PlayerScore)>();

        private bool isEvaluatingRoundEndAnimations;
        private IEnumerator EvaluateRoundEndAnimations()
        {
            isEvaluatingRoundEndAnimations = true;
            yield return null;

            // If there is no victor, do not evaluate the round end
            if (scoresToEvaluate.TrueForAll(item => !item.Item1))
            {
                scoresToEvaluate.Clear();
                isEvaluatingRoundEndAnimations = false;
                yield break;
            }

            foreach ((bool isVictor, PlayerScore playerScore) in scoresToEvaluate)
            {
                Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerScore.id);
                if (attributes)
                {
                    if (attributes.GetAilment() == ActionClip.Ailment.Death) { continue; }

                    StartCoroutine(PlayAnimation(attributes.AnimationHandler, isVictor));
                }
            }
            scoresToEvaluate.Clear();
            isEvaluatingRoundEndAnimations = false;
        }

        private IEnumerator PlayAnimation(AnimationHandler animationHandler, bool isVictor)
        {
            yield return new WaitUntil(() => animationHandler.IsAtRest());
            animationHandler.Animator.CrossFadeInFixedTime(isVictor ? "Victory" : "Defeat", 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));
            yield return ResetAnimation(animationHandler);
        }

        private IEnumerator ResetAnimation(AnimationHandler animationHandler)
        {
            yield return new WaitUntil(() => nextGameActionTimer.Value <= (nextGameActionDuration / 2));
            if (animationHandler)
            {
                animationHandler.Animator.CrossFadeInFixedTime("Empty", 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));
            }
        }

        public bool IsWaitingForPlayers { get; private set; } = true;
        private bool AreAllPlayersConnected()
        {
            if (Time.time - startTime > 15) { return true; }

            if (!PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().TrueForAll(item => PlayerDataManager.Singleton.IdHasLocalPlayer(item.id))) { return false; }

            List<Attributes> players = PlayerDataManager.Singleton.GetActivePlayerObjects();
            return players.Count > 0;
        }

        protected enum TimerMode
        {
            CountDown,
            CountUp
        }

        [SerializeField] private TimerMode timerMode = TimerMode.CountDown;

        private float startTime;
        protected virtual void Start()
        {
            startTime = Time.time;
        }

        protected virtual void Update()
        {
            if (IsWaitingForPlayers) { if (!AreAllPlayersConnected()) { return; } }
            IsWaitingForPlayers = false;

            if (!IsServer) { return; }

            if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
            {
                nextGameActionTimer.Value = 0;
            }
            else
            {
                if (nextGameActionTimer.Value > 0)
                {
                    nextGameActionTimer.Value = Mathf.Clamp(nextGameActionTimer.Value - Time.deltaTime, 0, nextGameActionDuration);
                }
                else if (!gameOver.Value)
                {
                    switch (timerMode)
                    {
                        case TimerMode.CountDown:
                            roundTimer.Value = Mathf.Clamp(roundTimer.Value - Time.deltaTime, 0, roundDuration);
                            break;
                        case TimerMode.CountUp:
                            roundTimer.Value += Time.deltaTime;
                            break;
                        default:
                            Debug.LogError("Not sure how to handle timer mode " + timerMode);
                            break;
                    }
                }
            }
        }

        public struct PlayerScore : INetworkSerializable, System.IEquatable<PlayerScore>
        {
            public int id;
            public int cumulativeKills;
            public int killsThisRound;
            public int cumulativeDeaths;
            public int deathsThisRound;
            public int cumulativeAssists;
            public int assistsThisRound;
            public float cumulativeDamageDealt;
            public float damageDealtThisRound;
            public float cumulativeDamageRecieved;
            public float damageRecievedThisRound;
            public int roundWins;
            public bool isValid;

            public PlayerScore(int id)
            {
                this.id = id;
                cumulativeKills = 0;
                killsThisRound = 0;
                cumulativeDeaths = 0;
                deathsThisRound = 0;
                cumulativeAssists = 0;
                assistsThisRound = 0;
                cumulativeDamageDealt = 0;
                damageDealtThisRound = 0;
                cumulativeDamageRecieved = 0;
                damageRecievedThisRound = 0;
                roundWins = 0;
                isValid = true;
            }

            public float GetExpReward()
            {
                return cumulativeKills * 5 + cumulativeAssists * 3;
            }

            public void ResetRoundVariables()
            {
                killsThisRound = 0;
                deathsThisRound = 0;
                assistsThisRound = 0;
            }

            public bool Equals(PlayerScore other)
            {
                return id == other.id;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeValue(ref cumulativeKills);
                serializer.SerializeValue(ref killsThisRound);
                serializer.SerializeValue(ref cumulativeDeaths);
                serializer.SerializeValue(ref deathsThisRound);
                serializer.SerializeValue(ref cumulativeAssists);
                serializer.SerializeValue(ref assistsThisRound);
                serializer.SerializeValue(ref cumulativeDamageDealt);
                serializer.SerializeValue(ref damageDealtThisRound);
                serializer.SerializeValue(ref cumulativeDamageRecieved);
                serializer.SerializeValue(ref damageRecievedThisRound);
                serializer.SerializeValue(ref roundWins);
                serializer.SerializeValue(ref isValid);
            }
        }

        private struct DisconnectedPlayerScore : INetworkSerializable, System.IEquatable<DisconnectedPlayerScore>
        {
            public FixedString64Bytes characterId;
            public PlayerScore playerScore;

            public DisconnectedPlayerScore(FixedString64Bytes characterId, PlayerScore playerScore)
            {
                this.characterId = characterId;
                this.playerScore = playerScore;
            }

            public bool Equals(DisconnectedPlayerScore other)
            {
                return characterId == other.characterId;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref characterId);
                serializer.SerializeNetworkSerializable(ref playerScore);
            }
        }

        public EssenceBuffOption[] EssenceBuffOptions { get { return essenceBuffOptions.ToArray(); } }

        [SerializeField] protected EssenceBuffOption[] essenceBuffOptions;

        [System.Serializable]
        public struct EssenceBuffOption : System.IEquatable<EssenceBuffOption>
        {
            public string title;
            public string description;
            public Color iconColor;
            public Sprite iconSprite;
            public int requiredEssenceCount;
            public bool stackable;

            public bool Equals(EssenceBuffOption other)
            {
                return title == other.title & description == other.description & iconColor == other.iconColor & iconSprite == other.iconSprite & requiredEssenceCount == other.requiredEssenceCount & stackable == other.stackable;
            }
        }
    }
}