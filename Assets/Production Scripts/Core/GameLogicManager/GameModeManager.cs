using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

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
            if (!isFirstRound) { PlayerDataManager.Singleton.RespawnAllPlayers(); }
            isFirstRound = false;
        }

        protected virtual void OnRoundEnd(int[] winningPlayersDataIds)
        {
            overtime.Value = false;
            bool shouldEndGame = false;
            foreach (int id in winningPlayersDataIds)
            {
                int index = scoreList.IndexOf(new PlayerScore(id));
                PlayerScore score = scoreList[index];
                score.roundWins += 1;
                scoreList[index] = score;

                if (score.roundWins >= numberOfRoundsWinsToWinGame) { shouldEndGame = true; }
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
                    AddPlayerScore(playerData.id);
                }
            }
            //roundTimer.Value = roundDuration;
            nextGameActionTimer.Value = nextGameActionDuration;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                roundTimer.OnValueChanged -= OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged -= OnNextGameActionTimerChange;
            }
        }

        public void AddPlayerScore(int id)
        {
            if (!IsSpawned)
            {
                StartCoroutine(WaitForSpawnToAddPlayerData(id));
            }
            else
            {
                if (scoreList.Contains(new PlayerScore(id))) { Debug.LogError("Player score with id: " + id + " has already been added!"); return; }
                scoreList.Add(new PlayerScore(id));
            }
        }

        private IEnumerator WaitForSpawnToAddPlayerData(int id)
        {
            yield return new WaitUntil(() => IsSpawned);
            AddPlayerScore(id);
        }

        public PlayerScore GetPlayerScore(int id) { return scoreList[scoreList.IndexOf(new PlayerScore(id))]; }

        public void RemovePlayerScore(int id)
        {
            scoreList.Remove(new PlayerScore(id));
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

        protected void Awake()
        {
            scoreList = new NetworkList<PlayerScore>();

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

        private GameObject UIInstance;
        protected void Start()
        {
            if (UIPrefab) { UIInstance = Instantiate(UIPrefab, transform); }
        }

        protected void Update()
        {
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
    }
}