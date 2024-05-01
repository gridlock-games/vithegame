using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Singleton { get { return _singleton; } }
        protected static GameModeManager _singleton;

        [SerializeField] private GameObject UIPrefab;
        [SerializeField] protected int numberOfRoundsWinsToWinGame = 2;
        [SerializeField] protected float roundDuration = 30;
        [SerializeField] private float nextGameActionDuration = 5;
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

        [SerializeField] private Sprite environmentKillFeedIcon;
        public struct KillHistoryElement : INetworkSerializable, System.IEquatable<KillHistoryElement>
        {
            public FixedString32Bytes killerName;
            public FixedString32Bytes victimName;
            public FixedString32Bytes weaponName;
            public KillType killType;

            public KillHistoryElement(Attributes killer, Attributes victim)
            {
                killerName = PlayerDataManager.Singleton.GetPlayerData(killer.GetPlayerDataId()).character.name;
                victimName = PlayerDataManager.Singleton.GetPlayerData(victim.GetPlayerDataId()).character.name;
                weaponName = killer.GetComponent<WeaponHandler>().GetWeapon().name.Replace("(Clone)", "");
                killType = KillType.Player;
            }

            public KillHistoryElement(Attributes victim)
            {
                killerName = "";
                victimName = PlayerDataManager.Singleton.GetPlayerData(victim.GetPlayerDataId()).character.name.ToString();
                weaponName = "Environment";
                killType = KillType.Environment;
            }

            public KillHistoryElement(bool isEnvironmentKill)
            {
                if (isEnvironmentKill)
                {
                    killerName = "";
                    victimName = "Victim";
                    weaponName = "Environment";
                    killType = KillType.Environment;
                }
                else
                {
                    var weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
                    killerName = "Killer";
                    victimName = "Victim";
                    weaponName = weaponOptions[Random.Range(0, weaponOptions.Length-1)].weapon.name;
                    killType = KillType.Player;
                }
            }

            public Sprite GetKillFeedIcon(KillHistoryElement killHistoryElement)
            {
                if (killType == KillType.Player)
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
                Environment
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref killerName);
                serializer.SerializeValue(ref victimName);
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
            foreach (KillHistoryElement ele in killHistory)
            {
                killHistoryList.Add(ele);
            }
            return killHistoryList;
        }

        public virtual void OnPlayerKill(Attributes killer, Attributes victim)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));
                PlayerScore killerScore = scoreList[killerIndex];
                killerScore.kills += 1;
                scoreList[killerIndex] = killerScore;

                int victimIndex = scoreList.IndexOf(new PlayerScore(victim.GetPlayerDataId()));
                PlayerScore victimScore = scoreList[victimIndex];
                victimScore.deaths += 1;
                scoreList[victimIndex] = victimScore;

                killHistory.Add(new KillHistoryElement(killer, victim));
            }
        }

        public virtual void OnEnvironmentKill(Attributes victim)
        {
            if (nextGameActionTimer.Value <= 0)
            {
                int victimIndex = scoreList.IndexOf(new PlayerScore(victim.GetPlayerDataId()));
                PlayerScore victimScore = scoreList[victimIndex];
                victimScore.deaths += 1;
                scoreList[victimIndex] = victimScore;

                killHistory.Add(new KillHistoryElement(victim));
            }
        }

        protected bool gameOver;
        protected virtual void OnGameEnd(int[] winningPlayersDataIds)
        {
            gameOver = true;
            gameEndMessage.Value = "Returning to lobby!";
        }

        private bool isFirstRound = true;
        protected virtual void OnRoundStart()
        {
            for (int i = 0; i < scoreList.Count; i++)
            {
                PlayerScore playerScore = scoreList[i];
                scoreList[i] = new PlayerScore(playerScore.id, playerScore.roundWins);
            }
            for (int i = 0; i < disconnectedScoreList.Count; i++)
            {
                FixedString32Bytes charId = disconnectedScoreList[i].characterId;
                PlayerScore playerScore = disconnectedScoreList[i].playerScore;
                playerScore = new PlayerScore(playerScore.id, playerScore.roundWins);
                disconnectedScoreList[i] = new DisconnectedPlayerScore(charId, playerScore);
            }
            if (!isFirstRound) { PlayerDataManager.Singleton.RespawnAllPlayers(); }
            isFirstRound = false;
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
                    foreach (DisconnectedPlayerScore disconnectedPlayerScore in disconnectedScoreList)
                    {
                        cachedList.Add(disconnectedPlayerScore);
                    }

                    int disconnectedIndex = cachedList.FindIndex(item => item.playerScore.id == id);
                    if (disconnectedIndex == -1) { continue; }

                    PlayerScore score = disconnectedScoreList[disconnectedIndex].playerScore;
                    FixedString32Bytes charId = disconnectedScoreList[disconnectedIndex].characterId;
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
                return "+" + minutes.ToString() + ":" + seconds.ToString("F2");
            else
                return minutes.ToString() + ":" + seconds.ToString("F2");
        }

        public bool ShouldDisplayNextGameAction() { return nextGameActionTimer.Value > 0; }
        public string GetNextGameActionTimerDisplayString() { return ((int)Mathf.Ceil(nextGameActionTimer.Value)).ToString(); }

        public override void OnNetworkSpawn()
        {
            _singleton = this;
            if (IsServer)
            {
                roundTimer.OnValueChanged += OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged += OnNextGameActionTimerChange;
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    AddPlayerScore(playerData.id, playerData.character._id);
                }
                //roundTimer.Value = roundDuration;
                nextGameActionTimer.Value = nextGameActionDuration;
            }

            #if UNITY_EDITOR
            if (!IsClient)
            {
                gameObject.AddComponent<AudioListener>();
                AudioListener.volume = 0;
            }
            #endif
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                roundTimer.OnValueChanged -= OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged -= OnNextGameActionTimerChange;
            }
        }

        public void AddPlayerScore(int id, FixedString32Bytes characterId)
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
                    playerScore.kills = disconnectedScoreList[index].playerScore.kills;
                    playerScore.deaths = disconnectedScoreList[index].playerScore.deaths;
                    playerScore.roundWins = disconnectedScoreList[index].playerScore.roundWins;
                    scoreList.Add(playerScore);
                    disconnectedScoreList.RemoveAt(index);
                }
            }
        }

        private IEnumerator WaitForSpawnToAddPlayerData(int id, FixedString32Bytes characterId)
        {
            yield return new WaitUntil(() => IsSpawned);
            AddPlayerScore(id, characterId);
        }

        public PlayerScore GetPlayerScore(int id) { return scoreList[scoreList.IndexOf(new PlayerScore(id))]; }

        public PlayerScore GetDisconnectedPlayerScore(int id)
        {
            foreach (DisconnectedPlayerScore disconnectedPlayerScore in disconnectedScoreList)
            {
                if (disconnectedPlayerScore.playerScore.id == id) { return disconnectedPlayerScore.playerScore; }
            }
            return new PlayerScore();
        }

        public void RemovePlayerScore(int id, FixedString32Bytes characterId)
        {
            int index = scoreList.IndexOf(new PlayerScore(id));
            if (index == -1) { Debug.LogError("Trying to remove score list, but can't find it for id: " + id); return; }
            disconnectedScoreList.Add(new DisconnectedPlayerScore(characterId, scoreList[index]));
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

        private void OnNextGameActionTimerChange(float prev, float current)
        {
            PlayerDataManager.Singleton.SetAllPlayersMobility(nextGameActionTimer.Value <= 0);
            if (current == 0 & prev > 0)
            {
                if (gameOver)
                {
                    NetSceneManager.Singleton.LoadScene("Lobby");
                }
                else
                {
                    roundTimer.Value = roundDuration;
                }
            }
        }

        protected List<PlayerScore> GetHighestKillPlayers()
        {
            List<PlayerScore> highestKillPlayerScores = new List<PlayerScore>();
            foreach (PlayerScore playerScore in scoreList)
            {
                if (highestKillPlayerScores.Count > 0)
                {
                    if (playerScore.kills > highestKillPlayerScores[0].kills)
                    {
                        highestKillPlayerScores.Clear();
                        highestKillPlayerScores.Add(playerScore);
                    }
                    else if (playerScore.kills == highestKillPlayerScores[0].kills)
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
                    Debug.LogError("Error whiel setting value for field: " + propertyName);
                }
            }
        }

        public void RegisterCallback(NetworkList<PlayerScore>.OnListChangedDelegate onListChangedDelegate)
        {
            scoreList.OnListChanged += onListChangedDelegate;
        }

        public void UnsubscribeCallback(NetworkList<PlayerScore>.OnListChangedDelegate onListChangedDelegate)
        {
            scoreList.OnListChanged -= onListChangedDelegate;
        }

        private GameObject UIInstance;
        protected void Start()
        {
            if (UIPrefab) { UIInstance = Instantiate(UIPrefab, transform); }
        }

        public bool IsWaitingForPlayers { get; private set; } = true;
        private bool AreAllPlayersConnected()
        {
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
                else if (!gameOver)
                    roundTimer.Value = Mathf.Clamp(roundTimer.Value - Time.deltaTime, 0, roundDuration);
            }
        }

        public struct PlayerScore : INetworkSerializable, System.IEquatable<PlayerScore>
        {
            public int id;
            public int kills;
            public int deaths;
            public int roundWins;

            public PlayerScore(int id)
            {
                this.id = id;
                kills = 0;
                deaths = 0;
                roundWins = 0;
            }

            public PlayerScore(int id, int roundWins)
            {
                this.id = id;
                kills = 0;
                deaths = 0;
                this.roundWins = roundWins;
            }

            public bool Equals(PlayerScore other)
            {
                return id == other.id;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeValue(ref kills);
                serializer.SerializeValue(ref deaths);
                serializer.SerializeValue(ref roundWins);
            }
        }

        private struct DisconnectedPlayerScore : INetworkSerializable, System.IEquatable<DisconnectedPlayerScore>
        {
            public FixedString32Bytes characterId;
            public PlayerScore playerScore;

            public DisconnectedPlayerScore(FixedString32Bytes characterId, PlayerScore playerScore)
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