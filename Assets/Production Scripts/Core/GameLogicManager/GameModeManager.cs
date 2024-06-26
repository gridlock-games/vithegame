using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using Vi.Utility;
using Unity.Collections;
using jomarcentermjm.PlatformAPI;

namespace Vi.Core.GameModeManagers
{
    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Singleton { get { return _singleton; } }
        protected static GameModeManager _singleton;

        [SerializeField] private GameObject UIPrefab;
        [SerializeField] private GameObject RPPrefab;

        [SerializeField] protected int numberOfRoundsWinsToWinGame = 2;
        [SerializeField] protected float roundDuration = 30;
        private const float nextGameActionDuration = 10;
        [Header("Leave respawn time as 0 to disable respawns")]
        [SerializeField] private float respawnTime = 5;

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

            List<PlayerSpawnPoints.TransformData> possibleSpawnPoints = PlayerDataManager.Singleton.GetGameItemSpawnPoints().ToList();

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
            PlayerSpawnPoints.TransformData spawnPoint = new PlayerSpawnPoints.TransformData();
            if (possibleSpawnPoints.Count != 0)
                spawnPoint = possibleSpawnPoints[randomIndex];
            else
                Debug.LogError("Possible spawn point count is 0! Game item - " + gameItemPrefab);

            GameItem gameItemInstance = Instantiate(gameItemPrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<GameItem>();
            gameItemInstance.NetworkObject.Spawn(true);
            return gameItemInstance;
        }

        protected GameItem SpawnGameItem(GameItem gameItemPrefab, PlayerSpawnPoints.TransformData spawnPoint)
        {
            if (!IsServer) { Debug.LogError("GameModeManager.SpawnGameItem() should only be called from the server!"); return null; }

            GameItem gameItemInstance = Instantiate(gameItemPrefab.gameObject, spawnPoint.position, spawnPoint.rotation).GetComponent<GameItem>();
            gameItemInstance.NetworkObject.Spawn(true);
            return gameItemInstance;
        }

        protected List<PlayerSpawnPoints.TransformData> GetGameItemSpawnPoints()
        {
            if (!IsServer) { Debug.LogError("GameModeManager.GetGameItemSpawnPoints() should only be called from the server!"); return null; }

            List<PlayerSpawnPoints.TransformData> possibleSpawnPoints = PlayerDataManager.Singleton.GetGameItemSpawnPoints().ToList();

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

            return possibleSpawnPoints;
        }

        [SerializeField] private Sprite environmentKillFeedIcon;
        public struct KillHistoryElement : INetworkSerializable, System.IEquatable<KillHistoryElement>
        {
            public FixedString64Bytes killerName;
            public ulong killerNetObjId;
            public FixedString64Bytes assistName;
            public ulong assistNetObjId;
            public FixedString64Bytes victimName;
            public ulong victimNetObjId;
            public FixedString64Bytes weaponName;
            public KillType killType;

            public KillHistoryElement(Attributes killer, Attributes victim)
            {
                PlayerDataManager.PlayerData killerData = PlayerDataManager.Singleton.GetPlayerData(killer.GetPlayerDataId());
                killerName = PlayerDataManager.Singleton.GetTeamPrefix(killerData.team) + killerData.character.name;
                killerNetObjId = killer.NetworkObjectId;

                assistName = "";
                assistNetObjId = 0;

                PlayerDataManager.PlayerData victimData = PlayerDataManager.Singleton.GetPlayerData(victim.GetPlayerDataId());
                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victimData.team) + victimData.character.name;
                victimNetObjId = victim.NetworkObjectId;

                weaponName = killer.GetComponent<WeaponHandler>().GetWeapon().name.Replace("(Clone)", "");
                killType = KillType.Player;
            }

            public KillHistoryElement(Attributes killer, Attributes assist, Attributes victim)
            {
                PlayerDataManager.PlayerData killerData = PlayerDataManager.Singleton.GetPlayerData(killer.GetPlayerDataId());
                killerName = PlayerDataManager.Singleton.GetTeamPrefix(killerData.team) + killerData.character.name;
                killerNetObjId = killer.NetworkObjectId;

                PlayerDataManager.PlayerData assistData = PlayerDataManager.Singleton.GetPlayerData(assist.GetPlayerDataId());
                assistName = PlayerDataManager.Singleton.GetTeamPrefix(assistData.team) + assistData.character.name;
                assistNetObjId = assist.NetworkObjectId;

                PlayerDataManager.PlayerData victimData = PlayerDataManager.Singleton.GetPlayerData(victim.GetPlayerDataId());
                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victimData.team) + victimData.character.name;
                victimNetObjId = victim.NetworkObjectId;

                weaponName = killer.GetComponent<WeaponHandler>().GetWeapon().name.Replace("(Clone)", "");
                killType = KillType.PlayerWithAssist;
            }

            public KillHistoryElement(Attributes victim)
            {
                killerName = "";
                killerNetObjId = 0;

                assistName = "";
                assistNetObjId = 0;

                PlayerDataManager.PlayerData victimData = PlayerDataManager.Singleton.GetPlayerData(victim.GetPlayerDataId());
                victimName = PlayerDataManager.Singleton.GetTeamPrefix(victimData.team) + victimData.character.name;
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
                var weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
                switch (killType)
                {
                    case KillType.Player:
                        killerName = "Killer";
                        assistName = "";
                        victimName = "Victim";
                        weaponName = weaponOptions[Random.Range(0, weaponOptions.Length - 1)].weapon.name;
                        return;
                    case KillType.PlayerWithAssist:
                        killerName = "Killer";
                        assistName = "Assist";
                        victimName = "Victim";
                        weaponName = weaponOptions[Random.Range(0, weaponOptions.Length - 1)].weapon.name;
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
                    return System.Array.Find(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions(), item => item.weapon.name == killHistoryElement.weaponName.ToString()).killFeedIcon;
                else if (killType == KillType.Environment)
                    return Singleton ? Singleton.environmentKillFeedIcon : PlayerDataManager.Singleton.GetCharacterReference().defaultEnvironmentKillIcon;
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
                serializer.SerializeValue(ref killerNetObjId);
                serializer.SerializeValue(ref assistName);
                serializer.SerializeValue(ref assistNetObjId);
                serializer.SerializeValue(ref victimName);
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

        public virtual void OnDamageOccuring(Attributes attacker, Attributes victim, float HPDamage)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                int attackerIndex = scoreList.IndexOf(new PlayerScore(attacker.GetPlayerDataId()));
                PlayerScore attackerScore = scoreList[attackerIndex];
                attackerScore.cumulativeDamageDealt += HPDamage;
                attackerScore.damageDealtThisRound += HPDamage;
                scoreList[attackerIndex] = attackerScore;

                int victimIndex = scoreList.IndexOf(new PlayerScore(victim.GetPlayerDataId()));
                PlayerScore victimScore = scoreList[victimIndex];
                victimScore.cumulativeDamageRecieved += HPDamage;
                victimScore.damageRecievedThisRound += HPDamage;
                scoreList[victimIndex] = victimScore;
            }
        }

        public virtual void OnPlayerKill(Attributes killer, Attributes victim)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));
                PlayerScore killerScore = scoreList[killerIndex];
                killerScore.cumulativeKills += 1;
                killerScore.killsThisRound += 1;
                scoreList[killerIndex] = killerScore;

                int victimIndex = scoreList.IndexOf(new PlayerScore(victim.GetPlayerDataId()));
                PlayerScore victimScore = scoreList[victimIndex];
                victimScore.cumulativeDeaths += 1;
                victimScore.deathsThisRound += 1;
                scoreList[victimIndex] = victimScore;

                // Damage is in negative numbers
                Attributes assist = victim.GetDamageMappingThisLife().Where(item => item.Key != killer & item.Key != victim & item.Value > minAssistDamage).OrderByDescending(item => item.Value).FirstOrDefault().Key;
                if (assist)
                {
                    int assistIndex = scoreList.IndexOf(new PlayerScore(assist.GetPlayerDataId()));
                    PlayerScore assistScore = scoreList[assistIndex];
                    assistScore.cumulativeAssists += 1;
                    assistScore.assistsThisRound += 1;
                    scoreList[assistIndex] = assistScore;

                    killHistory.Add(new KillHistoryElement(killer, assist, victim));
                }
                else
                {
                    killHistory.Add(new KillHistoryElement(killer, victim));
                }
            }
        }

        private const float minAssistDamage = 30;

        public virtual void OnEnvironmentKill(Attributes victim)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                int victimIndex = scoreList.IndexOf(new PlayerScore(victim.GetPlayerDataId()));
                PlayerScore victimScore = scoreList[victimIndex];
                victimScore.cumulativeDeaths += 1;
                victimScore.deathsThisRound += 1;
                scoreList[victimIndex] = victimScore;

                killHistory.Add(new KillHistoryElement(victim));
            }
        }

        public PlayerScore GetMVPScore() { return MVPScore.Value; }

        protected NetworkVariable<bool> gameOver = new NetworkVariable<bool>();
        protected NetworkVariable<PlayerScore> MVPScore = new NetworkVariable<PlayerScore>();
        protected virtual void OnGameEnd(int[] winningPlayersDataIds)
        {
            gameOver.Value = true;
            gameEndMessage.Value = "Returning to Lobby";

            List<PlayerScore> highestKillPlayers = GetHighestKillPlayers();
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
            roundCount.Value += 1;
            killHistory.Clear();
        }

        protected virtual void OnRoundEnd(int[] winningPlayersDataIds)
        {
            overtime.Value = false;
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

            if (shouldEndGame) { OnGameEnd(winningPlayersDataIds); }
            nextGameActionTimer.Value = nextGameActionDuration;
        }

        public string GetRoundTimerDisplayString()
        {
            int minutes = (int)roundTimer.Value / 60;
            float seconds = roundTimer.Value - (minutes * 60);

            if (overtime.Value)
                return "+" + minutes.ToString() + ":" + seconds.ToString("F0");
            else
                return minutes.ToString() + ":" + seconds.ToString("F0");
        }

        public bool ShouldDisplaySpecialNextGameActionMessage() { return ShouldDisplayNextGameAction() & nextGameActionTimer.Value <= 1 & !gameOver.Value; }

        public bool ShouldDisplayNextGameAction() { return nextGameActionTimer.Value > 0; }
        public bool ShouldDisplayNextGameActionTimer() { return nextGameActionTimer.Value <= nextGameActionDuration / 2; }
        public string GetNextGameActionTimerDisplayString() { return Mathf.Ceil(nextGameActionTimer.Value).ToString("F0"); }

        private GameObject UIInstance;
        private GameObject RPInstance;
        public override void OnNetworkSpawn()
        {
            if (UIPrefab) { UIInstance = Instantiate(UIPrefab, transform); }
            if (RPPrefab) { RPInstance = Instantiate(RPPrefab, transform); }
            _singleton = this;
            scoreList.OnListChanged += OnScoreListChange;
            if (IsServer)
            {
                roundTimer.OnValueChanged += OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged += OnNextGameActionTimerChange;
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    AddPlayerScore(playerData.id, playerData.character._id);
                }
                //roundTimer.Value = roundDuration;
                nextGameActionTimer.Value = nextGameActionDuration / 2;
            }
        }

        public override void OnNetworkDespawn()
        {
            scoreList.OnListChanged -= OnScoreListChange;
            if (IsServer)
            {
                roundTimer.OnValueChanged -= OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged -= OnNextGameActionTimerChange;
            }
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

        private void OnRoundTimerChange(float prev, float current)
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

        protected virtual void OnRoundTimerStart()
        {
            OnRoundStart();
        }

        protected virtual void OnRoundTimerEnd()
        {
            List<int> highestKillIdList = new List<int>();
            foreach (PlayerScore playerScore in GetHighestKillPlayers())
            {
                highestKillIdList.Add(playerScore.id);
            }
            OnRoundEnd(highestKillIdList.ToArray());
        }

        public bool ShouldFadeToBlack()
        {
            return nextGameActionTimer.Value > nextGameActionDuration / 2 & nextGameActionDuration - nextGameActionTimer.Value > 3 & GetRoundCount() > 0 & !gameOver.Value;
        }

        public bool WaitingToPlayGame() { return nextGameActionTimer.Value > 0; }

        private List<int> respawnsCalledByRoundCount = new List<int>();
        private void OnNextGameActionTimerChange(float prev, float current)
        {
            if (!gameOver.Value & GetRoundCount() > 0)
            {
                if (current <= nextGameActionDuration / 2 & prev > nextGameActionDuration / 2)
                {
                    if (!respawnsCalledByRoundCount.Contains(GetRoundCount()))
                    {
                        respawnsCalledByRoundCount.Add(GetRoundCount());
                        PlayerDataManager.Singleton.RespawnAllPlayers();
                        roundResultMessage.Value = "Round " + (GetRoundCount() + 1).ToString() + " is About to Start ";
                    }
                }
            }

            if (current == 0 & prev > 0)
            {
                if (gameOver.Value)
                {
                    if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None) { StartCoroutine(DisplayPostGameEvents()); }
                }
                else
                {
                    roundTimer.Value = roundDuration;
                }
            }
        }

        public enum PostGameStatus
        {
            None,
            MVP,
            Scoreboard
        }

        public PostGameStatus GetPostGameStatus() { return postGameStatus.Value; }
        private NetworkVariable<PostGameStatus> postGameStatus = new NetworkVariable<PostGameStatus>(PostGameStatus.None);

        private IEnumerator DisplayPostGameEvents()
        {
            if (GetMVPScore().isValid)
            {
                // MVP Presentation
                postGameStatus.Value = PostGameStatus.MVP;
                yield return new WaitForSeconds(60);
            }
            
            // Scoreboard
            postGameStatus.Value = PostGameStatus.Scoreboard;

            yield return new WaitForSeconds(5);

            // Return to Lobby
            NetSceneManager.Singleton.LoadScene("Lobby");
        }

        protected List<PlayerScore> GetHighestKillPlayers()
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

        public virtual string GetLeftScoreString() { return string.Empty; }

        public virtual string GetRightScoreString() { return string.Empty; }

        protected void Awake()
        {
            scoreList = new NetworkList<PlayerScore>();
            disconnectedScoreList = new NetworkList<DisconnectedPlayerScore>();
            killHistory = new NetworkList<KillHistoryElement>();

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

        private void OnScoreListChange(NetworkListEvent<PlayerScore> networkListEvent)
        {
            if (IsServer)
            {
                if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
                {
                    if (!gameOver.Value & !IsWaitingForPlayers)
                    {
                        if (scoreList.Count == 1) { EndGamePrematurely("Returning to lobby due to having no opponents!"); }
                    }
                }
            }

            if (networkListEvent.Type == NetworkListEvent<PlayerScore>.EventType.Value)
            {
                scoresToEvaluate.RemoveAll(item => item.Item2.Equals(networkListEvent.Value));
                scoresToEvaluate.Add((networkListEvent.PreviousValue.roundWins < networkListEvent.Value.roundWins, networkListEvent.Value));
                if (!isEvaluatingRoundEndAnimations) { StartCoroutine(EvaluateRoundEndAnimations()); }
            }
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
                    if (attributes.TryGetComponent(out AnimationHandler animationHandler))
                    {
                        StartCoroutine(PlayAnimation(animationHandler, isVictor));
                    }
                }
            }
            scoresToEvaluate.Clear();
            isEvaluatingRoundEndAnimations = false;
        }

        private IEnumerator PlayAnimation(AnimationHandler animationHandler, bool isVictor)
        {
            yield return new WaitUntil(() => animationHandler.IsAtRest());
            animationHandler.Animator.CrossFade(isVictor ? "Victory" : "Defeat", 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));
            yield return ResetAnimation(animationHandler);
        }

        private IEnumerator ResetAnimation(AnimationHandler animationHandler)
        {
            yield return new WaitUntil(() => nextGameActionTimer.Value <= (nextGameActionDuration / 2));
            if (animationHandler)
            {
                animationHandler.Animator.CrossFade("Empty", 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));
            }
        }

        public void SubscribeScoreListCallback(NetworkList<PlayerScore>.OnListChangedDelegate onListChangedDelegate)
        {
            scoreList.OnListChanged += onListChangedDelegate;
        }

        public void UnsubscribeScoreListCallback(NetworkList<PlayerScore>.OnListChangedDelegate onListChangedDelegate)
        {
            scoreList.OnListChanged -= onListChangedDelegate;
        }

        public bool IsWaitingForPlayers { get; private set; } = true;
        private bool AreAllPlayersConnected()
        {
            if (!PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().TrueForAll(item => PlayerDataManager.Singleton.IdHasLocalPlayer(item.id))) { return false; }

            List<Attributes> players = PlayerDataManager.Singleton.GetActivePlayerObjects();
            if (players.Count == 0) { return false; }
            return players.TrueForAll(item => item.IsSpawnedOnOwnerInstance());
        }

        protected void Update()
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
                    nextGameActionTimer.Value = Mathf.Clamp(nextGameActionTimer.Value - Time.deltaTime, 0, nextGameActionDuration);
                else if (!gameOver.Value)
                    roundTimer.Value = Mathf.Clamp(roundTimer.Value - Time.deltaTime, 0, roundDuration);
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
    }
}