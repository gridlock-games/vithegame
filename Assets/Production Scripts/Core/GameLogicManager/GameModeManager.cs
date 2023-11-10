using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class GameModeManager : NetworkBehaviour
    {
        [SerializeField] private GameObject UIPrefab;
        [SerializeField] private float roundDuration = 30;

        private NetworkVariable<float> roundTimer = new NetworkVariable<float>();

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

            if (IsServer) { roundTimer.OnValueChanged += OnRoundTimerEndChange; }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) { roundTimer.OnValueChanged -= OnRoundTimerEndChange; }
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
    }
}