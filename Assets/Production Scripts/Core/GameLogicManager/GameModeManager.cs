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

        private NetworkVariable<float> roundTimer = new NetworkVariable<float>();

        protected NetworkList<PlayerScore> scoreList;

        public void OnPlayerKill(Attributes killer, Attributes victim)
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

        public float GetRoundTimerValue() { return roundTimer.Value; }

        public string GetRoundTimerDisplayString()
        {
            int minutes = (int)roundTimer.Value / 60;
            float seconds = roundTimer.Value - (minutes * 60);
            return minutes.ToString() + ":" + seconds.ToString("F2");
        }

        public override void OnNetworkSpawn()
        {
            roundTimer.Value = roundDuration;
            if (IsServer)
            {
                PlayerDataManager.Singleton.playerDataList.OnListChanged += OnPlayerDataListChange;
                //foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayers())
                //{
                //    scoreList.Add(new PlayerScore(attributes.GetPlayerDataId()));
                //}
                roundTimer.OnValueChanged += OnRoundTimerEndChange;
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
                roundTimer.OnValueChanged -= OnRoundTimerEndChange;
            }
        }

        private void OnRoundTimerEndChange(float prev, float current)
        {
            if (current == 0 & prev > 0)
            {
                Debug.Log("Round over");
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