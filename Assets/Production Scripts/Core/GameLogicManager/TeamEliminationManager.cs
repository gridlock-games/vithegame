using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                StartCoroutine(CreateDamageCircle());
            }
        }

        private IEnumerator CreateDamageCircle()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.PlayerSpawnPoints());
            damageCircleInstance = Instantiate(damageCirclePrefab.gameObject).GetComponent<DamageCircle>();
            damageCircleInstance.NetworkObject.Spawn();
        }

        public override void OnEnvironmentKill(Attributes victim)
        {
            base.OnEnvironmentKill(victim);

            PlayerDataManager.Team opposingTeam = victim.GetTeam() == PlayerDataManager.Team.Red ? PlayerDataManager.Team.Blue : PlayerDataManager.Team.Red;
            List<Attributes> victimTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(victim.GetTeam());
            if (victimTeam.TrueForAll(item => item.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death))
            {
                List<Attributes> opposingTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(opposingTeam);
                List<int> winningPlayerIds = new List<int>();
                foreach (Attributes attributes in opposingTeamPlayers)
                {
                    winningPlayerIds.Add(attributes.GetPlayerDataId());
                }

                OnRoundEnd(winningPlayerIds.ToArray());
            }
            else if (CanSpawnViEssence()) // If we are in a 1vX situation
            {
                Debug.Log("Spawning vi essence");
                viEssenceInstance = SpawnGameItem(viEssencePrefab).GetComponent<TeamEliminationViEssence>();
                viEssenceInstance.Initialize(this, damageCircleInstance);
            }
            else // If we cannot spawn vi essence, destroy it if it exists
            {
                if (viEssenceInstance & IsServer) { viEssenceInstance.NetworkObject.Despawn(true); }
                if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
            }
        }

        private TeamEliminationViEssence viEssenceInstance;
        public override void OnPlayerKill(Attributes killer, Attributes victim)
        {
            base.OnPlayerKill(killer, victim);
            int killerIndex = scoreList.IndexOf(new PlayerScore(killer.GetPlayerDataId()));

            List<Attributes> killerTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(killer.GetTeam());
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
            else if (CanSpawnViEssence()) // If we are in a 1vX situation
            {
                viEssenceInstance = SpawnGameItem(viEssencePrefab).GetComponent<TeamEliminationViEssence>();
                viEssenceInstance.Initialize(this, damageCircleInstance);
            }
            else // If we cannot spawn vi essence, destroy it if it exists
            {
                if (viEssenceInstance & IsServer) { viEssenceInstance.NetworkObject.Despawn(true); }
                if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
            }
        }

        private Coroutine viEssenceSpawningCoroutine;
        public void OnViEssenceActivation()
        {
            if (CanSpawnViEssence()) { viEssenceSpawningCoroutine = StartCoroutine(SpawnNewViEssenceAfterDelay()); }
        }

        private IEnumerator SpawnNewViEssenceAfterDelay()
        {
            yield return new WaitForSeconds(5);

            if (CanSpawnViEssence())
            {
                viEssenceInstance = SpawnGameItem(viEssencePrefab).GetComponent<TeamEliminationViEssence>();
                viEssenceInstance.Initialize(this, damageCircleInstance);
            }
        }

        private bool CanSpawnViEssence()
        {
            if (viEssenceInstance) { return false; }

            List<Attributes> redTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red);
            List<Attributes> blueTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue);

            return (redTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1 & blueTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count > 1)
                | (redTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count > 1 & blueTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1);
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            damageCircleInstance.NetworkObject.Despawn(true);
            roundResultMessage.Value = "Game over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team + " team wins the match!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            damageCircleInstance.ResetDamageCircle();
            if (viEssenceInstance & IsServer) { if (viEssenceInstance.IsSpawned) { viEssenceInstance.NetworkObject.Despawn(true); } }
            if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
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