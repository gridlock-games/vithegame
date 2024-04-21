using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core.GameModeManagers
{
    public class EssenceWarManager : GameModeManager
    {
        public string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return "Red Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red)[0].GetPlayerDataId()).roundWins.ToString();
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Red)
                {
                    return "Your Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red)[0].GetPlayerDataId()).roundWins.ToString();
                }
                else if (localTeam == PlayerDataManager.Team.Blue)
                {
                    return "Your Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue)[0].GetPlayerDataId()).roundWins.ToString();
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return string.Empty;
                }
            }
        }

        public string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return "Blue Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue)[0].GetPlayerDataId()).roundWins.ToString();
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Red)
                {
                    return "Enemy Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue)[0].GetPlayerDataId()).roundWins.ToString();
                }
                else if (localTeam == PlayerDataManager.Team.Blue)
                {
                    return "Enemy Team: " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red)[0].GetPlayerDataId()).roundWins.ToString();
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return string.Empty;
                }
            }
        }
    }
}

