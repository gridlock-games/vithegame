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

        public string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            int localIndex = scoreList.IndexOf(new PlayerScore(PlayerDataManager.Singleton.GetLocalPlayer().Key));
            return PlayerDataManager.Singleton.GetPlayerData(PlayerDataManager.Singleton.GetLocalPlayer().Key).playerName + ": " + scoreList[localIndex].kills;
        }

        public string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            int localIndex = scoreList.IndexOf(new PlayerScore(PlayerDataManager.Singleton.GetLocalPlayer().Key));

            foreach (PlayerScore playerScore in scoreList)
            {
                if (playerScore.kills > scoreList[localIndex].kills) { return PlayerDataManager.Singleton.GetPlayerData(playerScore.id).playerName + ": " + playerScore.kills.ToString(); }
            }

            return "";
        }
    }
}