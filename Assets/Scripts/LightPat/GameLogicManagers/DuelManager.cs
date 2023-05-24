using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace LightPat.Core
{
    public class DuelManager : GameLogicManager
    {
        public int scoreToWin = 3;

        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI timerDisplay;
        [SerializeField] private TextMeshProUGUI gameEndDisplay;

        private NetworkVariable<float> countdownTime = new NetworkVariable<float>(3);
        private NetworkVariable<float> roundTimeInSeconds = new NetworkVariable<float>(10);
        
        private NetworkVariable<int> redScore = new NetworkVariable<int>();
        private NetworkVariable<int> blueScore = new NetworkVariable<int>();

        private NetworkVariable<bool> allPlayersSpawned = new NetworkVariable<bool>();

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
            if (current >= scoreToWin) { OnGameEnd(); }
        }

        private void Update()
        {
            if (IsServer)
            {
                allPlayersSpawned.Value = true;
                foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                {
                    NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                    if (playerObject)
                        playerObject.GetComponent<GameCreator.Characters.Character>().disableActions.Value = !timerDisplay.enabled;
                    else
                        allPlayersSpawned.Value = false;
                }

                if (!allPlayersSpawned.Value) { return; }

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

                if (roundTimeInSeconds.Value <= 0) { OnTimerEnd(); }
            }

            if (countdownTime.Value > 0)
            {
                countdownText.enabled = true;
                timerDisplay.enabled = false;
            }
            else
            {
                countdownText.enabled = false;
                timerDisplay.enabled = true;
            }

            countdownText.SetText(countdownTime.Value.ToString("F0"));
            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));

            redScoreText.SetText(redScore.Value.ToString());
            blueScoreText.SetText(blueScore.Value.ToString());
        }

        private void OnRoundEnd()
        {
            if (!IsServer) { return; }
            if (roundEndCountdownRunning) { return; }

            countdownTime.Value = 3;

            StartCoroutine(WaitForRoundEndCountdown());
        }

        private bool roundEndCountdownRunning;
        private IEnumerator WaitForRoundEndCountdown()
        {
            roundEndCountdownRunning = true;
            yield return new WaitUntil(() => countdownTime.Value <= 0);

            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                GameCreator.Characters.Character playerChar = NetworkManager.Singleton.ConnectedClients[clientPair.Key].PlayerObject.GetComponent<GameCreator.Characters.Character>();
                playerChar.CancelAilment();
                playerChar.GetComponent<GameCreator.Melee.CharacterMelee>().ResetHP();
                
                foreach (TeamSpawnPoint teamSpawnPoint in spawnPoints)
                {
                    if (teamSpawnPoint.team == clientPair.Value.team)
                    {
                        playerChar.transform.position = teamSpawnPoint.spawnPosition;
                        playerChar.transform.rotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                        break;
                    }
                }
            }

            countdownTime.Value = 3;
            roundTimeInSeconds.Value = 10;
            roundEndCountdownRunning = false;
        }

        private bool timerEnded;
        private void OnTimerEnd()
        {
            if (timerEnded) { return; }
            timerEnded = true;

            Team winningTeam = Team.Environment;
            int lastHP = -1;
            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientPair.Key].PlayerObject;
                int currentHP = playerObject.GetComponent<GameCreator.Melee.CharacterMelee>().GetHP();
                if (currentHP > lastHP)
                {
                    winningTeam = clientPair.Value.team;
                }
                else if (currentHP == lastHP)
                {
                    winningTeam = Team.Environment;
                }
                lastHP = currentHP;
            }

            if (winningTeam == Team.Red)
                redScore.Value += 1;
            else if (winningTeam == Team.Blue)
                blueScore.Value += 1;
            else
                OnRoundEnd();

            StartCoroutine(WaitForTimerReset());
        }

        private IEnumerator WaitForTimerReset()
        {
            yield return new WaitUntil(() => roundTimeInSeconds.Value > 0);
            timerEnded = false;
        }

        private void OnGameEnd()
        {
            // Display game end message
            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                if (redScore.Value >= scoreToWin)
                {
                    if (clientPair.Value.team == Team.Red)
                    {
                        gameEndDisplay.SetText(clientPair.Value.clientName + " wins!");
                        break;
                    }
                }
                else if (blueScore.Value >= scoreToWin)
                {
                    if (clientPair.Value.team == Team.Blue)
                    {
                        gameEndDisplay.SetText(clientPair.Value.clientName + " wins!");
                        break;
                    }
                }
            }

            if (IsServer)
            {
                countdownTime.Value = 3;
            }
        }
    }
}