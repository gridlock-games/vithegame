using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class FreeForAllManager : GameModeManager
    {
        [SerializeField] private int killsToWinRound = 2;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            roundResultMessage.Value = "Free for all starting! ";
        }

        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);
            int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));
            if (scoreList[killerIndex].kills >= killsToWinRound)
            {
                OnRoundEnd(new int[] { killer.GetPlayerDataId() });
            }
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            roundResultMessage.Value = "Game over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).playerName + " wins the free for all!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            if (gameOver) { return; }
            string message;
            if (winningPlayersDataIds.Length > 1)
            {
                message = winningPlayersDataIds.Length.ToString() + " players are tied for first place! ";
            }
            else
            {
                message = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).playerName + " has won the round! ";
            }
            roundResultMessage.Value = message;
        }

        public string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            int localIndex = scoreList.IndexOf(new PlayerScore(PlayerDataManager.Singleton.GetLocalPlayer().Key));
            return PlayerDataManager.Singleton.GetPlayerData(PlayerDataManager.Singleton.GetLocalPlayer().Key).playerName + ": " + scoreList[localIndex].kills;
        }

        public string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            List<PlayerScore> scoreList = new List<PlayerScore>();
            PlayerScore localPlayerScore;
            foreach (PlayerScore playerScore in this.scoreList)
            {
                if (playerScore.id == PlayerDataManager.Singleton.GetLocalPlayer().Key)
                {
                    localPlayerScore = playerScore;
                }
                else
                {
                    scoreList.Add(playerScore);
                }
            }
            // Find player score with highest kills
            scoreList = scoreList.OrderByDescending(item => item.kills).ToList();
            return PlayerDataManager.Singleton.GetPlayerData(scoreList[0].id).playerName + ": " + scoreList[0].kills.ToString();
        }
    }
}