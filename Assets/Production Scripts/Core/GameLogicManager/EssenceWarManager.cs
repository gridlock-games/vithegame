using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.CombatAgents;
using Vi.Core.Structures;
using Vi.Utility;

namespace Vi.Core.GameModeManagers
{
    public class EssenceWarManager : GameModeManager
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                roundResultMessage.Value = "Essence War Starting! ";
            }
        }

        private float lastWaveSpawnTime = Mathf.NegativeInfinity;
        private float lastOgreSpawnEventTime = Mathf.NegativeInfinity;
        protected override void Update()
        {
            base.Update();
            if (!IsSpawned) { return; }

            if (IsServer)
            {
                if (!ShouldDisplayNextGameAction() & !IsGameOver())
                {
                    if (Time.time - lastWaveSpawnTime > 30)
                    {
                        if (SpawnWave())
                        {
                            lastWaveSpawnTime = Time.time;
                        }
                    }

                    if (!IsViEssenceSpawned())
                    {
                        if (Time.time - lastOgreSpawnEventTime > 40)
                        {
                            SpawnMob(kingOgreMob, PlayerDataManager.Team.Environment, false);
                            lastOgreSpawnEventTime = Time.time;
                        }
                    }
                }
            }
        }

        [Header("Essence War Specific")]
        [SerializeField] private Mob kingOgreMob;
        [SerializeField] private Mob spiderMinionMob;
        private bool SpawnWave()
        {
            if (!PlayerDataManager.Singleton.HasPlayerSpawnPoints()) { return false; }

            for (int i = 0; i < 5; i++)
            {
                SpawnMob(spiderMinionMob, PlayerDataManager.Team.Light, false);
                SpawnMob(spiderMinionMob, PlayerDataManager.Team.Corruption, false);
            }

            return true;
        }

        [SerializeField] private EssenceWarViEssence viEssencePrefab;
        public bool IsViEssenceSpawned() { return NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value); }

        private EssenceWarViEssence GetViEssenceInstance()
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value)) { return null; }
            return NetworkManager.SpawnManager.SpawnedObjects[viEssenceNetObjId.Value].GetComponent<EssenceWarViEssence>();
        }

        private NetworkVariable<ulong> viEssenceNetObjId = new NetworkVariable<ulong>();
        private void SpawnViEssence(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) { Debug.LogError("SpawnViEssence should only be called on the server!"); return; }
            EssenceWarViEssence viEssence = ObjectPoolingManager.SpawnObject(viEssencePrefab.GetComponent<PooledObject>(), position + new Vector3(0, 1, 0), rotation).GetComponent<EssenceWarViEssence>();
            viEssence.Initialize(this);
            viEssence.NetworkObject.Spawn(true);
            viEssenceNetObjId.Value = viEssence.NetworkObjectId;
        }

        public void OnViEssenceActivation(Attributes newBearer)
        {
            lastOgreSpawnEventTime = Time.time;
        }

        public override void OnEnvironmentKill(CombatAgent victim)
        {
            base.OnEnvironmentKill(victim);

            if (victim.GetName().ToUpper().Contains("KING OGRE"))
            {
                SpawnViEssence(victim.MovementHandler.GetPosition(), victim.MovementHandler.GetRotation());
            }
        }

        public override void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            base.OnPlayerKill(killer, victim);

            if (victim.GetName().ToUpper().Contains("KING OGRE"))
            {
                SpawnViEssence(victim.MovementHandler.GetPosition(), victim.MovementHandler.GetRotation());
            }
        }

        public override void OnStructureKill(CombatAgent killer, Structure structure)
        {
            base.OnStructureKill(killer, structure);
            if (gameOver.Value) { return; }

            if (structure.GetName().ToUpper().Contains("TOTEM"))
            {
                PlayerDataManager.Team winningTeam;
                if (structure.GetTeam() == PlayerDataManager.Team.Light)
                {
                    winningTeam = PlayerDataManager.Team.Corruption;
                }
                else if (structure.GetTeam() == PlayerDataManager.Team.Corruption)
                {
                    winningTeam = PlayerDataManager.Team.Light;
                }
                else
                {
                    Debug.LogWarning("Unsure how to handle structure team " + structure.GetTeam());
                    winningTeam = PlayerDataManager.Team.Environment;
                }

                List<int> winningIdList = new List<int>();
                foreach (Attributes player in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(winningTeam))
                {
                    winningIdList.Add(player.GetPlayerDataId());
                }
                OnRoundEnd(winningIdList.ToArray());
            }
            else
            {
                Debug.LogWarning("Unsure how to handle structure kill " + structure.GetName());
            }
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);

            // Despawn vi essence and other mobs here?

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

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            roundResultMessage.Value = "Game Over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team) + " Wins the Match!";
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

