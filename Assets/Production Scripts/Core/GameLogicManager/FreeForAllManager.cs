using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class FreeForAllManager : GameModeManager
    {
        [Header("Free for all specific")]
        [SerializeField] private int killsToWinRound = 2;

        public int GetKillsToWinRound() { return killsToWinRound; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { roundResultMessage.Value = "Free for All Starting! "; }
        }

        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);
            int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));
            if (scoreList[killerIndex].killsThisRound >= killsToWinRound)
            {
                OnRoundEnd(new int[] { killer.GetPlayerDataId() });
            }
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            roundResultMessage.Value = "Game Over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).character.name + " Wins the Free for All!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            if (gameOver.Value) { return; }
            string message;
            if (winningPlayersDataIds.Length > 1)
            {
                message = winningPlayersDataIds.Length.ToString() + " Players are Tied for First Place! ";
            }
            else if (winningPlayersDataIds.Length == 0)
            {
                message = "Round Draw! ";
            }
            else
            {
                message = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).character.name + " Has Won the Round! ";
            }
            roundResultMessage.Value = message;
        }

        protected override void OnRoundTimerEnd()
        {
            List<int> highestKillIdList = new List<int>();
            foreach (PlayerScore playerScore in GetHighestKillPlayersThisRound())
            {
                highestKillIdList.Add(playerScore.id);
            }

            if (highestKillIdList.Count == 1)
            {
                OnRoundEnd(highestKillIdList.ToArray());
            }
            else if (!overtime.Value)
            {
                roundTimer.Value = overtimeDuration;
                overtime.Value = true;
            }
            else
            {
                OnRoundEnd(new int[0]);
            }
        }

        public override string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            int localPlayerKey = PlayerDataManager.Singleton.GetLocalPlayerObject().Key;
            int localIndex = scoreList.IndexOf(new PlayerScore(localPlayerKey));
            if (localIndex == -1)
            {
                // If we're a spectator
                List<PlayerScore> scoreList = new List<PlayerScore>();
                PlayerScore localPlayerScore;
                foreach (PlayerScore playerScore in this.scoreList)
                {
                    if (playerScore.id == PlayerDataManager.Singleton.GetLocalPlayerObject().Key)
                    {
                        localPlayerScore = playerScore;
                    }
                    else
                    {
                        scoreList.Add(playerScore);
                    }
                }
                // Find player score with second highest kills
                scoreList = scoreList.OrderByDescending(item => item.killsThisRound).ToList();
                if (scoreList.Count > 1)
                    return PlayerDataManager.Singleton.GetPlayerData(scoreList[1].id).character.name + ": " + scoreList[1].killsThisRound.ToString();
                else
                    return string.Empty;
            }
            else
            {
                return PlayerDataManager.Singleton.GetPlayerData(localPlayerKey).character.name + ": " + scoreList[localIndex].killsThisRound;
            }
        }

        public override string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            List<PlayerScore> scoreList = new List<PlayerScore>();
            PlayerScore localPlayerScore;
            foreach (PlayerScore playerScore in this.scoreList)
            {
                if (playerScore.id == PlayerDataManager.Singleton.GetLocalPlayerObject().Key)
                {
                    localPlayerScore = playerScore;
                }
                else
                {
                    scoreList.Add(playerScore);
                }
            }
            // Find player score with highest kills
            scoreList = scoreList.OrderByDescending(item => item.killsThisRound).ToList();
            if (scoreList.Count > 0)
                return PlayerDataManager.Singleton.GetPlayerData(scoreList[0].id).character.name + ": " + scoreList[0].killsThisRound.ToString();
            else
                return string.Empty;
        }
    }
}