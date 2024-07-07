using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

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
            if (IsServer)
            {
                roundResultMessage.Value = "Team Elimination Starting! ";
                StartCoroutine(CreateDamageCircle());
            }
        }

        private IEnumerator CreateDamageCircle()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.HasPlayerSpawnPoints());
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
            else if (CanSpawnViEssenceGameLogicCondition()) // If we are in a 1vX situation
            {
                if (CanSpawnViEssence())
                {
                    //var possibleSpawnPoints = GetGameItemSpawnPoints().Where(item => damageCircleInstance.IsPointInsideDamageCircleBounds(item.position));
                    viEssenceNetObjId.Value = SpawnGameItem(viEssencePrefab).NetworkObjectId;
                    GetViEssenceInstance().Initialize(this, damageCircleInstance);
                }
            }
            else // If we cannot spawn vi essence, destroy it if it exists
            {
                if (IsViEssenceSpawned() & IsServer)
                {
                    GetViEssenceInstance().NetworkObject.Despawn(true);
                }
                if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
            }
        }

        public bool IsViEssenceSpawned() { return NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value); }

        private TeamEliminationViEssence GetViEssenceInstance()
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value)) { return null; }
            return NetworkManager.SpawnManager.SpawnedObjects[viEssenceNetObjId.Value].GetComponent<TeamEliminationViEssence>();
        }

        private NetworkVariable<ulong> viEssenceNetObjId = new NetworkVariable<ulong>();
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
            else if (CanSpawnViEssenceGameLogicCondition()) // If we are in a 1vX situation
            {
                if (CanSpawnViEssence())
                {
                    viEssenceNetObjId.Value = SpawnGameItem(viEssencePrefab).NetworkObjectId;
                    GetViEssenceInstance().Initialize(this, damageCircleInstance);
                }
            }
            else // If we cannot spawn vi essence, destroy it if it exists
            {
                if (GetViEssenceInstance() & IsServer)
                {
                    GetViEssenceInstance().NetworkObject.Despawn(true);
                }
                if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
            }
        }

        private Coroutine viEssenceSpawningCoroutine;
        public void OnViEssenceActivation()
        {
            if (CanSpawnViEssenceGameLogicCondition()) { viEssenceSpawningCoroutine = StartCoroutine(SpawnNewViEssenceAfterDelay()); }
        }

        private IEnumerator SpawnNewViEssenceAfterDelay()
        {
            yield return new WaitForSeconds(5);

            if (CanSpawnViEssence())
            {
                viEssenceNetObjId.Value = SpawnGameItem(viEssencePrefab).NetworkObjectId;
                GetViEssenceInstance().Initialize(this, damageCircleInstance);
            }
        }

        private bool CanSpawnViEssence()
        {
            if (IsViEssenceSpawned()) { return false; }
            return CanSpawnViEssenceGameLogicCondition();
        }

        private bool CanSpawnViEssenceGameLogicCondition()
        {
            List<Attributes> redTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red);
            List<Attributes> blueTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue);

            return (redTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1 & blueTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count > 1)
                | (redTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count > 1 & blueTeam.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1);
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            damageCircleInstance.NetworkObject.Despawn(true);
            roundResultMessage.Value = "Game Over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team) + " Wins the Match!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            damageCircleInstance.ResetDamageCircle();
            if (IsViEssenceSpawned() & IsServer)
            {
                GetViEssenceInstance().NetworkObject.Despawn(true);
            }
            if (viEssenceSpawningCoroutine != null) { StopCoroutine(viEssenceSpawningCoroutine); }
            if (gameOver.Value) { return; }

            if (winningPlayersDataIds.Length == 0)
            {
                string message = "Round Draw! ";
                roundResultMessage.Value = message;
            }
            else
            {
                string message = PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team) + " Secured Round " + GetRoundCount().ToString() + " ";
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

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Red) + ": " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Red)[0].GetPlayerDataId()).roundWins.ToString();
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

        public PlayerDataManager.Team GetLeftScoreTeam()
        {
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Red; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Red;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Red)
                {
                    return PlayerDataManager.Team.Red;
                }
                else if (localTeam == PlayerDataManager.Team.Blue)
                {
                    return PlayerDataManager.Team.Blue;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Red;
                }
            }
        }

        public override string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Blue) + ": " + GetPlayerScore(PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Blue)[0].GetPlayerDataId()).roundWins.ToString();
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

        public PlayerDataManager.Team GetRightScoreTeam()
        {
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Blue; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Blue;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Red)
                {
                    return PlayerDataManager.Team.Blue;
                }
                else if (localTeam == PlayerDataManager.Team.Blue)
                {
                    return PlayerDataManager.Team.Red;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Blue;
                }
            }
        }
    }
}