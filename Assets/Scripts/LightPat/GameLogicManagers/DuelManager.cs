using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace LightPat.Core
{
    public class DuelManager : GameLogicManager
    {
        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI timerDisplay;
        [SerializeField] private NetworkVariable<float> countdownTime = new NetworkVariable<float>();
        [SerializeField] private NetworkVariable<float> roundTimeInSeconds = new NetworkVariable<float>();

        private NetworkVariable<int> redScore = new NetworkVariable<int>();
        private NetworkVariable<int> blueScore = new NetworkVariable<int>();

        public override void OnPlayerDeath(Team team)
        {
            if (IsServer)
            {
                if (team == Team.Red)
                    redScore.Value += 1;
                else if (team == Team.Blue)
                    blueScore.Value += 1;
            }
        }

        public override void OnNetworkSpawn()
        {
            redScore.OnValueChanged += OnScoreChange;
            blueScore.OnValueChanged += OnScoreChange;
        }

        public override void OnNetworkDespawn()
        {
            redScore.OnValueChanged -= OnScoreChange;
            blueScore.OnValueChanged -= OnScoreChange;
        }

        private void OnScoreChange(int prev, int current)
        {
            OnRoundEnd();
        }

        private void Update()
        {
            if (IsServer)
            {
                bool allPlayersSpawned = true;
                foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                {
                    NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                    if (playerObject)
                        playerObject.GetComponent<GameCreator.Characters.PlayerCharacter>().allowPlayerMovement.Value = !timerDisplay.enabled;
                    else
                        allPlayersSpawned = false;
                }

                if (!allPlayersSpawned) { return; }

                if (countdownTime.Value > 0)
                {
                    countdownTime.Value -= Time.deltaTime;
                    if (countdownTime.Value < 0) { countdownTime.Value = 0; }
                }
                else
                {
                    roundTimeInSeconds.Value -= Time.deltaTime;
                    if (roundTimeInSeconds.Value < 0) { roundTimeInSeconds.Value = 0; }
                }

                if (roundTimeInSeconds.Value <= 0) { OnRoundEnd(); }
            }

            if (countdownTime.Value > 0)
            {
                countdownText.enabled = true;
                timerDisplay.enabled = false;
                allowPlayerMovement.Value = false;
            }
            else
            {
                countdownText.enabled = false;
                timerDisplay.enabled = true;
                allowPlayerMovement.Value = true;
            }

            countdownText.SetText(countdownTime.Value.ToString("F0"));
            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));

            redScoreText.SetText(redScore.Value.ToString());
            blueScoreText.SetText(blueScore.Value.ToString());
        }

        private void OnRoundEnd()
        {
            if (!IsServer) { return; }
            countdownTime.Value = 3;
            roundTimeInSeconds.Value = 180;

            foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
            {
                foreach (TeamSpawnPoint teamSpawnPoint in spawnPoints)
                {
                    NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.position = teamSpawnPoint.spawnPosition;
                    NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.rotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }
        }
    }
}