using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.CombatAgents;
using Vi.Core.DynamicEnvironmentElements;
using Vi.ScriptableObjects;

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
            damageCircleInstance.NetworkObject.Spawn(true);
        }

        public override void OnEnvironmentKill(CombatAgent victim)
        {
            base.OnEnvironmentKill(victim);
            if (gameOver.Value) { return; }
            PlayerDataManager.Team opposingTeam = victim.GetTeam() == PlayerDataManager.Team.Light ? PlayerDataManager.Team.Corruption : PlayerDataManager.Team.Light;
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
        public override void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            base.OnPlayerKill(killer, victim);
            if (gameOver.Value) { return; }
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
            if (PlayerDataManager.Singleton.GetGameItemSpawnPoints().Length == 0) { return false; }
            if (IsViEssenceSpawned()) { return false; }
            return CanSpawnViEssenceGameLogicCondition();
        }

        private bool CanSpawnViEssenceGameLogicCondition()
        {
            List<Attributes> redTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Light);
            List<Attributes> blueTeam = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Corruption);

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
                roundResultMessage.Value = "Round Draw! ";
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
                deathCountByTeam.Add(team, PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(team).Count(item => item.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death));
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
            else if (!overtime.Value) // TODO only enable overtime if one team is on match point
            {
                roundTimer.Value = overtimeDuration;
                overtime.Value = true;
            }
            else // End of overtime
            {
                PlayerDataManager.Team winningTeam = PlayerDataManager.Team.Environment;
                Dictionary<PlayerDataManager.Team, int> aliveCountDict = new Dictionary<PlayerDataManager.Team, int>();
                foreach (PlayerDataManager.Team team in uniqueTeamList)
                {
                    int aliveCount = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(team).Count(item => item.GetAilment() != ActionClip.Ailment.Death);
                    aliveCountDict.Add(team, aliveCount);
                }

                List<int> aliveCounts = aliveCountDict.Values.ToList();
                if (aliveCounts.TrueForAll(item => item == aliveCounts.FirstOrDefault()))
                {
                    Debug.Log("Alive counts are equal");
                    float highestAverageHP = -1;
                    foreach (PlayerDataManager.Team team in uniqueTeamList)
                    {
                        float averageHP = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(team).Average(item => item.GetHP());
                        Debug.Log(team + " " + averageHP);
                        if (averageHP > highestAverageHP)
                        {
                            winningTeam = team;
                            highestAverageHP = averageHP;
                        }
                    }

                    if (winningTeam == PlayerDataManager.Team.Environment)
                    {
                        Debug.LogError("Winning team is environment! This should never happen!");
                        OnRoundEnd(new int[0]);
                    }
                    else
                    {
                        List<int> winningIds = new List<int>();
                        foreach (Attributes winningTeamPlayer in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(winningTeam))
                        {
                            winningIds.Add(winningTeamPlayer.GetPlayerDataId());
                        }
                        OnRoundEnd(winningIds.ToArray());
                    }
                }
                else // Alive counts are not equal
                {
                    Debug.Log("Alive counts are not equal");
                    if (aliveCountDict.Count == 0)
                    {
                        Debug.LogError("Death Count dictionary count is 0! This should never happen!");
                        OnRoundEnd(new int[0]);
                    }
                    else
                    {
                        foreach (var kvp in aliveCountDict)
                        {
                            Debug.Log(kvp.Key + " " + kvp.Value);
                        }

                        winningTeam = aliveCountDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                        Debug.Log("Winning team: " + winningTeam);
                        if (winningTeam == PlayerDataManager.Team.Environment)
                        {
                            Debug.LogError("Winning team is environment! This should never happen!");
                            OnRoundEnd(new int[0]);
                        }
                        else
                        {
                            List<int> winningIds = new List<int>();
                            foreach (Attributes winningTeamPlayer in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(winningTeam))
                            {
                                winningIds.Add(winningTeamPlayer.GetPlayerDataId());
                            }
                            OnRoundEnd(winningIds.ToArray());
                        }
                    }
                }
            }
        }

        public override string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                List<Attributes> lightTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Light);
                if (lightTeamPlayers.Count > 0)
                {
                    return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Light) + ": " + GetPlayerScore(lightTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                }
                else
                {
                    return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Light) + ": 0";
                }
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    List<Attributes> redTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Light);
                    if (redTeamPlayers.Count > 0)
                    {
                        return "Your Team: " + GetPlayerScore(redTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                    }
                    else
                    {
                        return "Your Team: 0";
                    }
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    List<Attributes> blueTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Corruption);
                    if (blueTeamPlayers.Count > 0)
                    {
                        return "Your Team: " + GetPlayerScore(blueTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                    }
                    else
                    {
                        return "Your Team: 0";
                    }
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
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Light; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Light;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return PlayerDataManager.Team.Light;
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return PlayerDataManager.Team.Corruption;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Light;
                }
            }
        }

        public override string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                List<Attributes> corruptionTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Corruption);
                if (corruptionTeamPlayers.Count > 0)
                {
                    return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Corruption) + ": " + GetPlayerScore(corruptionTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                }
                else
                {
                    return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Corruption) + ": 0";
                }
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    List<Attributes> blueTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Corruption);
                    if (blueTeamPlayers.Count > 0)
                    {
                        return "Enemy Team: " + GetPlayerScore(blueTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                    }
                    else
                    {
                        return "Enemy Team: 0";
                    }
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    List<Attributes> redTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(PlayerDataManager.Team.Light);
                    if (redTeamPlayers.Count > 0)
                    {
                        return "Enemy Team: " + GetPlayerScore(redTeamPlayers[0].GetPlayerDataId()).roundWins.ToString();
                    }
                    else
                    {
                        return "Enemy Team: 0";
                    }
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
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Corruption; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Corruption;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return PlayerDataManager.Team.Corruption;
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return PlayerDataManager.Team.Light;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Corruption;
                }
            }
        }
    }
}