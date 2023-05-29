using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using GameCreator.Characters;

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
            if (!IsClient) { return; }

            if (prev > 0 & current <= 0)
            {
                NetworkObject localPlayer = NetworkManager.LocalClient.PlayerObject;
                if (localPlayer)
                {
                    if (changeLocomotionControlCoroutine != null)
                        StopCoroutine(changeLocomotionControlCoroutine);
                    changeLocomotionControlCoroutine = StartCoroutine(ChangeLocomotionControlOnAilmentReset(localPlayer.GetComponent<Character>(), CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true));
                }
            }
            else if (prev <= 0 & current > 0)
            {
                NetworkObject localPlayer = NetworkManager.LocalClient.PlayerObject;
                if (localPlayer)
                {
                    if (changeLocomotionControlCoroutine != null)
                        StopCoroutine(changeLocomotionControlCoroutine);
                    changeLocomotionControlCoroutine = StartCoroutine(ChangeLocomotionControlOnAilmentReset(localPlayer.GetComponent<Character>(), CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false));
                }
            }
        }

        Coroutine changeLocomotionControlCoroutine;
        private IEnumerator ChangeLocomotionControlOnAilmentReset(Character playerChar, CharacterLocomotion.OVERRIDE_FACE_DIRECTION newFaceDirection, bool isControllable)
        {
            yield return new WaitUntil(() => playerChar.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None & playerChar.resetDefaultStateRunning == false);
            yield return null;
            playerChar.characterLocomotion.UpdateDirectionControl(newFaceDirection, isControllable);
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
                        playerObject.GetComponent<Character>().disableActions.Value = !timerDisplay.enabled;
                    else
                        allPlayersSpawned.Value = false;
                }

                if (!allPlayersSpawned.Value) { return; }

                if (countdownTime.Value > 0)
                {
                    foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
                    {
                        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                        playerObject.GetComponent<GameCreator.Melee.CharacterMelee>().SetInvincibility(countdownTime.Value);
                    }

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

            countdownText.SetText(countdownTimeMessage.Value.ToString() + "\n" + countdownTime.Value.ToString("F0"));
            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));

            foreach (KeyValuePair<ulong, ClientData> clientPair in ClientManager.Singleton.GetClientDataDictionary())
            {
                if (clientPair.Value.team == Team.Red)
                    redScoreText.SetText(clientPair.Value.clientName + ": " + redScore.Value.ToString());
                else if (clientPair.Value.team == Team.Blue)
                    blueScoreText.SetText(clientPair.Value.clientName + ": " + blueScore.Value.ToString());
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
                Character playerChar = NetworkManager.Singleton.ConnectedClients[clientPair.Key].PlayerObject.GetComponent<Character>();
                playerChar.CancelAilment();

                GameCreator.Melee.CharacterMelee charMelee = playerChar.GetComponent<GameCreator.Melee.CharacterMelee>();
                charMelee.ResetHP();
                charMelee.ResetDefense();
                charMelee.ResetPoise();

                foreach (TeamSpawnPoint teamSpawnPoint in spawnPoints)
                {
                    if (teamSpawnPoint.team == clientPair.Value.team)
                    {
                        playerChar.transform.position = teamSpawnPoint.spawnPosition;
                        playerChar.UpdateRotationClientRpc(Quaternion.Euler(teamSpawnPoint.spawnRotation), new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerChar.OwnerClientId } } });
                        break;
                    }
                }
            }

            countdownTimeMessage.Value = "Get ready for the next round!";
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
                countdownTime.Value = 10;
                StartCoroutine(ReturnToLobby());
            }
        }

        private IEnumerator ReturnToLobby()
        {
            yield return new WaitUntil(() => countdownTime.Value <= 0);
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}