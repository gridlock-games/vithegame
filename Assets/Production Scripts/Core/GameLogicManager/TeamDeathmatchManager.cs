using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class TeamDeathmatchManager : GameModeManager
    {
        [Header("Team Deathmatch Specific")]
        [SerializeField] private int killsToWinRound = 21;

        public int GetKillsToWinRound() { return killsToWinRound; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { roundResultMessage.Value = "Team deathmatch starting! "; }
        }

        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);

            List<int> killerTeamIds = new List<int>();
            foreach (Attributes killerTeamPlayer in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(killer.GetTeam()))
            {
                killerTeamIds.Add(killerTeamPlayer.GetPlayerDataId());
            }

            int killerTeamScore = 0;
            foreach (int killerTeamId in killerTeamIds)
            {
                int index = scoreList.IndexOf(new PlayerScore(killerTeamId));
                killerTeamScore += scoreList[index].kills;
            }

            if (killerTeamScore >= killsToWinRound)
            {
                OnRoundEnd(killerTeamIds.ToArray());
            }
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            roundResultMessage.Value = "Game over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team + " team wins the match!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            if (gameOver) { return; }

            if (winningPlayersDataIds.Length == 0)
            {
                string message = "Round draw! ";
                roundResultMessage.Value = message;
            }
            else
            {
                string message = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team + " team has won the round! ";
                roundResultMessage.Value = message;
            }
        }

        protected override void OnRoundTimerEnd()
        {
            List<PlayerDataManager.Team> uniqueTeamList = new List<PlayerDataManager.Team>();
            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                if (!uniqueTeamList.Contains(attributes.GetTeam())) { uniqueTeamList.Add(attributes.GetTeam()); }
            }

            Dictionary<PlayerDataManager.Team, int> killCountByTeam = new Dictionary<PlayerDataManager.Team, int>();
            foreach (PlayerDataManager.Team team in uniqueTeamList)
            {
                int killSum = 0;
                foreach (Attributes attributes in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(team))
                {
                    int index = scoreList.IndexOf(new PlayerScore(attributes.GetPlayerDataId()));
                    killSum += scoreList[index].kills;
                }
                killCountByTeam.Add(team, killSum);
            }

            int highestKills = killCountByTeam.Max(item => item.Value);
            PlayerDataManager.Team[] winningTeams = killCountByTeam.Where(item => item.Value == highestKills).Select(item => item.Key).ToArray();

            if (winningTeams.Length == 1)
            {
                List<Attributes> winningTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(winningTeams[0]);
                List<int> winningPlayerIds = new List<int>();
                foreach (Attributes attributes in winningTeamPlayers)
                {
                    winningPlayerIds.Add(attributes.GetPlayerDataId());
                }
                OnRoundEnd(winningPlayerIds.ToArray());
            }
            else if (!overtime.Value)
            {
                roundTimer.Value = 30;
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

        public override string GetRightScoreString()
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