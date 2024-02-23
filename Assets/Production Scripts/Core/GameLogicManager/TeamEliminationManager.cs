using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

namespace Vi.Core.GameModeManagers
{
    public class TeamEliminationManager : GameModeManager
    {
        [Header("Team Elimination Specific")]
        [SerializeField] private DamageCircle damageCirclePrefab;
        [SerializeField] private TeamEliminationViEssence viEssencePrefab;

        private DamageCircle damageCircleInstance;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            roundResultMessage.Value = "Team elimination starting! ";

            if (IsServer)
            {
                damageCircleInstance = Instantiate(damageCirclePrefab.gameObject).GetComponent<DamageCircle>();
                damageCircleInstance.NetworkObject.Spawn();
            }
        }

        private TeamEliminationViEssence viEssenceInstance;
        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);
            // TODO Change this to check if all players on the victim's team are dead
            int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));

            List<Attributes> victimTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(victim.GetTeam());
            if (victimTeam.TrueForAll(item => item.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death))
            {
                List<Attributes> killerTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(killer.GetTeam());
                List<int> winningPlayerIds = new List<int>();
                foreach (Attributes attributes in killerTeamPlayers)
                {
                    winningPlayerIds.Add(attributes.GetPlayerDataId());
                }

                OnRoundEnd(winningPlayerIds.ToArray());
            }
            else if (victimTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1) // If we are in a 1vX situation
            {
                viEssenceInstance = SpawnGameItem(viEssencePrefab).GetComponent<TeamEliminationViEssence>();
                viEssenceInstance.Initialize(damageCircleInstance);
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
            if (viEssenceInstance) { Destroy(viEssenceInstance.gameObject); }
            if (gameOver) { return; }
            string message = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team + " team has won the round! ";
            roundResultMessage.Value = message;
        }

        protected override void OnRoundTimerEnd()
        {
            List<PlayerDataManager.Team> uniqueTeamList = new List<PlayerDataManager.Team>();
            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                if (!uniqueTeamList.Contains(attributes.GetTeam())) { uniqueTeamList.Add(attributes.GetTeam()); }
            }

            Dictionary<PlayerDataManager.Team, int> deathCountByTeam = new Dictionary<PlayerDataManager.Team, int>();
            foreach (PlayerDataManager.Team team in uniqueTeamList)
            {
                deathCountByTeam.Add(team, PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(team).FindAll(item => item.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death).Count);
            }

            int highestDeaths = deathCountByTeam.Max(item => item.Value);
            PlayerDataManager.Team[] winningTeams = deathCountByTeam.Where(item => item.Value != highestDeaths).Select(item => item.Key).ToArray();
            
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
            else
            {
                roundTimer.Value = 30;
                overtime.Value = true;
            }
        }

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