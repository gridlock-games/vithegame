using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class FreeForAllManager : GameModeManager
    {
        [SerializeField] private int killsToWin = 2;

        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);
            if (scoreList[scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()))].kills >= killsToWin)
            {
                OnGameEnd();
            }
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