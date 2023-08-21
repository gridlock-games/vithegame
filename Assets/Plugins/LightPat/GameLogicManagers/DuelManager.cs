using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using GameCreator.Characters;
using System.Linq;

namespace LightPat.Core
{
    public class DuelManager : GameLogicManager
    {
        public int scoreToWin = 3;
        public float roundTimeAmount = 180;

        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI timerDisplay;
        [SerializeField] private TextMeshProUGUI gameEndDisplay;

        private NetworkVariable<float> countdownTime = new NetworkVariable<float>(3);
        private NetworkVariable<float> roundTimeInSeconds = new NetworkVariable<float>(1);
        
        private NetworkVariable<int> redScore = new NetworkVariable<int>();
        private NetworkVariable<int> blueScore = new NetworkVariable<int>();

        private NetworkVariable<bool> allPlayersSpawned = new NetworkVariable<bool>();

        private NetworkVariable<FixedString32Bytes> countdownTimeMessage = new NetworkVariable<FixedString32Bytes>("Starting the duel!");

        public override void OnPlayerKill(ulong killerClientId)
        {
            if (IsServer)
            {
                Team killerTeam = ClientManager.Singleton.GetClient(killerClientId).team;
                if (killerTeam == Team.Red)
                    redScore.Value += 1;
                else if (killerTeam == Team.Blue)
                    blueScore.Value += 1;
            }
        }

        public override void OnNetworkSpawn()
        {
            roundTimeInSeconds.Value = roundTimeAmount;
            redScore.OnValueChanged += OnRedScoreChange;
            blueScore.OnValueChanged += OnBlueScoreChange;
            countdownTime.OnValueChanged += OnCountdownTimerChange;
        }

        public override void OnNetworkDespawn()
        {
            redScore.OnValueChanged -= OnRedScoreChange;
            blueScore.OnValueChanged -= OnBlueScoreChange;
            countdownTime.OnValueChanged -= OnCountdownTimerChange;
        }

        private void OnRedScoreChange(int prev, int current)
        {
            bool gameOver = current >= scoreToWin;
            OnRoundEnd(Team.Red, gameOver);
            if (gameOver) { OnGameEnd(); }
        }

        private void OnBlueScoreChange(int prev, int current)
        {
            bool gameOver = current >= scoreToWin;
            OnRoundEnd(Team.Blue, gameOver);
            if (gameOver) { OnGameEnd(); }
        }

        private void OnCountdownTimerChange(float prev, float current)
        {
            if (!IsServer) { return; }

            if (prev > 0 & current <= 0)
            {
                foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                {
                    if (ClientManager.Singleton.GetClient(clientId).team != Team.Spectator)
                    {
                        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                        Character playerChar = playerObject.GetComponent<Character>();
                        playerChar.characterLocomotion.SetAllowDirectionControlChanges(true, CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);
                    }
                }
            }
            else if (prev <= 0 & current > 0)
            {
                foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                {
                    if (ClientManager.Singleton.GetClient(clientId).team != Team.Spectator)
                    {
                        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                        Character playerChar = playerObject.GetComponent<Character>();
                        playerChar.characterLocomotion.SetAllowDirectionControlChanges(false, CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                    }
                }
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                bool allPlayersSpawnedOnOwnerInstances = true;
                ulong[] clientIdArray = ClientManager.Singleton.GetClientDataDictionary().Keys.ToArray();
                foreach (ulong clientId in clientIdArray)
                {
                    // Assign teams when players spawn in
                    if (ClientManager.Singleton.GetClient(clientId).team == Team.Competitor)
                    {
                        Team targetTeam = Team.Red;
                        foreach (ulong nestedClientId in clientIdArray)
                        {
                            if (ClientManager.Singleton.GetClient(nestedClientId).team == Team.Red)
                            {
                                targetTeam = Team.Blue;
                            }
                        }
                        ClientManager.Singleton.ChangeTeamOnServer(clientId, targetTeam);
                    }

                    if (ClientManager.Singleton.GetClient(clientId).team == Team.Spectator) { continue; }

                    NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                    if (playerObject)
                    {
                        playerObject.GetComponent<Character>().disableActions.Value = !timerDisplay.enabled;
                        if (allPlayersSpawnedOnOwnerInstances)
                            allPlayersSpawnedOnOwnerInstances = playerObject.GetComponent<Player.NetworkPlayer>().IsSpawnedOnOwnerInstance() & playerObject.IsSpawned;
                    }
                    else
                    {
                        allPlayersSpawnedOnOwnerInstances = false;
                    }
                }

                if (!allPlayersSpawned.Value & allPlayersSpawnedOnOwnerInstances)
                {
                    foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                    {
                        if (ClientManager.Singleton.GetClient(clientId).team != Team.Spectator)
                        {
                            NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                            playerObject.GetComponent<Character>().characterLocomotion.SetAllowDirectionControlChanges(false, CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                        }
                    }
                }

                allPlayersSpawned.Value = allPlayersSpawnedOnOwnerInstances;

                // Only change timer values if all players are spawned
                if (allPlayersSpawned.Value)
                {
                    if (countdownTime.Value > 0)
                    {
                        foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                        {
                            if (ClientManager.Singleton.GetClient(clientId).team != Team.Spectator)
                            {
                                NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                                playerObject.GetComponent<GameCreator.Melee.CharacterMelee>().SetInvincibility(countdownTime.Value);
                            }
                        }

                        countdownTime.Value -= Time.deltaTime;
                        if (countdownTime.Value < 0) { countdownTime.Value = 0; }
                    }
                    else
                    {
                        roundTimeInSeconds.Value -= Time.deltaTime;
                        if (roundTimeInSeconds.Value < 0) { roundTimeInSeconds.Value = 0; }

                        // If a competitor disconnects during the match, end the game
                        int competitorCount = 0;
                        foreach (ClientData clientData in ClientManager.Singleton.GetClientDataDictionary().Values)
                        {
                            if (clientData.team != Team.Spectator) { competitorCount++; }
                        }

                        if (competitorCount != 2) { OnGameEnd(); }
                    }

                    if (roundTimeInSeconds.Value <= 0) { OnTimerEnd(); }
                }
            }

            if (countdownTime.Value > 0 | !allPlayersSpawned.Value)
            {
                countdownText.enabled = true;
                timerDisplay.enabled = false;
            }
            else
            {
                countdownText.enabled = false;
                timerDisplay.enabled = true;
            }

            countdownText.SetText(allPlayersSpawned.Value ? countdownTimeMessage.Value.ToString() + "\n" + countdownTime.Value.ToString("F0") : countdownTimeMessage.Value.ToString());
            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));

            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                if (clientPair.Value.team == Team.Red)
                    redScoreText.SetText(EllipsizeString(clientPair.Value.clientName, 5)  + ": " + redScore.Value.ToString());
                else if (clientPair.Value.team == Team.Blue)
                    blueScoreText.SetText(EllipsizeString(clientPair.Value.clientName, 5) + ": " + blueScore.Value.ToString());
            }
        }

        private string EllipsizeString(string input, int maxLength)
        {
            if (input.Length <= maxLength)
            {
                return input;
            }
            else
            {
                return input.Substring(0, maxLength - 1) + "...";
            }
        }

        private void OnRoundEnd(Team winningTeam, bool gameOver)
        {
            if (!IsServer) { return; }
            if (roundEndCountdownRunning) { return; }

            if (winningTeam == Team.Environment)
            {
                countdownTimeMessage.Value = "This round was a draw!";
            }
            else
            {
                foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
                {
                    if (clientPair.Value.team == winningTeam)
                    {
                        countdownTimeMessage.Value = clientPair.Value.clientName + " won the round!";
                        break;
                    }
                }
            }

            if (!gameOver)
            {
                countdownTime.Value = 3;
                StartCoroutine(WaitForRoundEndCountdown());
            }
        }

        private bool roundEndCountdownRunning;
        private IEnumerator WaitForRoundEndCountdown()
        {
            roundEndCountdownRunning = true;
            yield return new WaitUntil(() => countdownTime.Value <= 0);

            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                if (clientPair.Value.team == Team.Spectator) { continue; }

                Character playerChar = NetworkManager.Singleton.ConnectedClients[clientPair.Key].PlayerObject.GetComponent<Character>();
                playerChar.CancelAilment();

                GameCreator.Melee.CharacterMelee charMelee = playerChar.GetComponent<GameCreator.Melee.CharacterMelee>();
                charMelee.ResetHP();
                charMelee.ResetDefense();
                charMelee.ResetPoise();

                KeyValuePair<Vector3, Quaternion> spawnOrientation = GetSpawnOrientation(clientPair.Value.team);
                if (playerChar.TryGetComponent(out PlayerCharacterNetworkTransform networkTransform))
                    networkTransform.SetPosition(spawnOrientation.Key);
                else
                    playerChar.UpdatePositionClientRpc(spawnOrientation.Key, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerChar.OwnerClientId } } });

                playerChar.UpdateRotationClientRpc(spawnOrientation.Value, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerChar.OwnerClientId } } });
            }

            countdownTimeMessage.Value = "Ready!";
            countdownTime.Value = 3;
            roundTimeInSeconds.Value = roundTimeAmount;
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
                OnRoundEnd(winningTeam, false);

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
                countdownTimeMessage.Value = "Returning to lobby...";
                countdownTime.Value = 5;
                StartCoroutine(ReturnToLobby());
            }
        }

        private IEnumerator ReturnToLobby()
        {
            yield return new WaitUntil(() => countdownTime.Value <= 0);
            ClientManager.Singleton.ResetAllClientData();
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}