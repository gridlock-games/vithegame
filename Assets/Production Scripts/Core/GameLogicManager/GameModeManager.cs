using Unity.Netcode;
using UnityEngine;

namespace Vi.Core.GameModeManagers
{
    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Singleton { get { return _singleton; } }
        protected static GameModeManager _singleton;

        [SerializeField] private GameObject UIPrefab;
        [SerializeField] private float roundDuration = 30;
        [SerializeField] private float nextGameActionDuration = 5;

        private NetworkVariable<float> roundTimer = new NetworkVariable<float>();
        private NetworkVariable<float> nextGameActionTimer = new NetworkVariable<float>();

        protected NetworkList<PlayerScore> scoreList;

        public virtual void OnPlayerKill(Attributes killer, Attributes victim)
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

        protected virtual void OnGameEnd()
        {
            roundTimer.Value = 0;
        }

        public bool ShouldUpdateRoundTimerDisplay() { return nextGameActionTimer.Value <= 0; }
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
            roundTimer.Value = roundDuration;
            if (IsServer)
            {
                PlayerDataManager.Singleton.playerDataList.OnListChanged += OnPlayerDataListChange;
                roundTimer.OnValueChanged += OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged += OnNextGameTimerChange;
            }
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

        protected void ChangePlayerScore(PlayerScore playerScore)
        {
            scoreList[scoreList.IndexOf(playerScore)] = playerScore;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                PlayerDataManager.Singleton.playerDataList.OnListChanged += OnPlayerDataListChange;
                roundTimer.OnValueChanged -= OnRoundTimerChange;
                nextGameActionTimer.OnValueChanged -= OnNextGameTimerChange;
            }
        }

        private void OnRoundTimerChange(float prev, float current)
        {
            PlayerDataManager.Singleton.SetAllPlayersMobility(current <= 0);

            if (current == 0 & prev > 0)
            {
                Debug.Log("Round over");
                nextGameActionTimer.Value = nextGameActionDuration;
            }
        }

        private void OnNextGameTimerChange(float prev, float current)
        {
            if (current == 0 & prev > 0)
            {
                Debug.Log("Next Game Action Timer over");
                PlayerDataManager.Singleton.RespawnPlayers();
                roundTimer.Value = roundDuration;
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

            roundTimer.Value = Mathf.Clamp(roundTimer.Value - Time.deltaTime, 0, roundDuration);
            nextGameActionTimer.Value = Mathf.Clamp(nextGameActionTimer.Value - Time.deltaTime, 0, nextGameActionDuration);
        }

        protected struct PlayerScore : INetworkSerializable, System.IEquatable<PlayerScore>
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