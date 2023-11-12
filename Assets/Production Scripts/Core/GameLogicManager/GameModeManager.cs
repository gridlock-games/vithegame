using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

namespace Vi.Core.GameModeManagers
{
    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Singleton { get { return _singleton; } }
        protected static GameModeManager _singleton;

        [SerializeField] private GameObject UIPrefab;
        [SerializeField] private int numberOfRoundsWinsToWinGame = 2;
        [SerializeField] private float roundDuration = 30;
        [SerializeField] private float nextGameActionDuration = 5;

        private NetworkVariable<float> roundTimer = new NetworkVariable<float>();
        private NetworkVariable<float> nextGameActionTimer = new NetworkVariable<float>();

        protected NetworkVariable<FixedString64Bytes> roundResultMessage = new NetworkVariable<FixedString64Bytes>();
        protected NetworkVariable<FixedString64Bytes> gameEndMessage = new NetworkVariable<FixedString64Bytes>();

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

        protected virtual void OnRoundEnd(int[] winningPlayersDataIds)
        {
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
            return minutes.ToString() + ":" + seconds.ToString("F2");
        }

        public bool ShouldDisplayNextGameAction() { return nextGameActionTimer.Value > 0; }
        public string GetNextGameActionTimerDisplayString() { return ((int)Mathf.Ceil(nextGameActionTimer.Value)).ToString(); }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                PlayerDataManager.Singleton.playerDataList.OnListChanged += OnPlayerDataListChange;
                roundTimer.OnValueChanged += OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged += OnNextGameActionTimerChange;
            }
            //roundTimer.Value = roundDuration;
            nextGameActionTimer.Value = nextGameActionDuration;
        }

        public void OnPlayerDataListChange(NetworkListEvent<PlayerDataManager.PlayerData> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<PlayerDataManager.PlayerData>.EventType.Add)
            {
                scoreList.Add(new PlayerScore(networkListEvent.Value.id));
            }
            else if (networkListEvent.Type == NetworkListEvent<PlayerDataManager.PlayerData>.EventType.Remove)
            {
                scoreList.Remove(new PlayerScore(networkListEvent.Value.id));
            }
        }

        public PlayerScore GetPlayerScore(int id) { return scoreList[scoreList.IndexOf(new PlayerScore(id))]; }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                PlayerDataManager.Singleton.playerDataList.OnListChanged += OnPlayerDataListChange;
                roundTimer.OnValueChanged -= OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged -= OnNextGameActionTimerChange;
            }
        }

        private void OnRoundTimerChange(float prev, float current)
        {
            if (current <= 0 & prev > 0)
            {
                List<int> highestKillIdList = new List<int>();
                foreach (PlayerScore playerScore in GetHighestKillPlayers())
                {
                    highestKillIdList.Add(playerScore.id);
                }
                OnRoundEnd(highestKillIdList.ToArray());
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

        private void OnNextGameActionTimerChange(float prev, float current)
        {
            PlayerDataManager.Singleton.SetAllPlayersMobility(current <= 0);

            if (current == 0 & prev > 0)
            {
                if (gameOver)
                {
                    NetworkManager.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
                else
                {
                    PlayerDataManager.Singleton.RespawnPlayers();
                    roundTimer.Value = roundDuration;
                }
            }
        }

        protected void Awake()
        {
            _singleton = this;
            scoreList = new NetworkList<PlayerScore>();
        }

        private GameObject UIInstance;
        protected void Start()
        {
            UIInstance = Instantiate(UIPrefab, transform);
        }

        protected void Update()
        {
            if (!IsServer) { return; }

            if (nextGameActionTimer.Value > 0)
                nextGameActionTimer.Value = Mathf.Clamp(nextGameActionTimer.Value - Time.deltaTime, 0, nextGameActionDuration);
            else if (!gameOver)
                roundTimer.Value = Mathf.Clamp(roundTimer.Value - Time.deltaTime, 0, roundDuration);
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