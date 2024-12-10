using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.CombatAgents;

namespace Vi.Core.GameModeManagers
{
    public class EssenceWarManager : GameModeManager
    {
        [Header("Essence War Specific")]
        [SerializeField] private GameObject ancientBossCorruptPrefab;
        [SerializeField] private GameObject ancientBossLightPrefab;
        [SerializeField] private GameObject ancientBossNeutralPrefab;
        [SerializeField] private GameObject ancientBossNeutralSlavePrefab;
        [SerializeField] private EssenceWarViEssence viEssencePrefab;

        private const float neutralAncientLogicThresholdDuration = 60;
        private const float neutralAncientRespawnDuration = 10;
        private const float neutralAncientRespawnDurationNoBlessed = 15;
        private const float viEssenceItemDuration = 30;
        private const int neutralAncientSlaveCount = 3;
        private const float neutralAncientRoamRadius = 5;

        private GameObject instance;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                roundResultMessage.Value = "Essence war starting! ";
                StartCoroutine(SpawnAncients());
            }
        }

        private IEnumerator SpawnAncients()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.HasPlayerSpawnPoints());
            SpawnPoints playerSpawnPoints = PlayerDataManager.Singleton.GetPlayerSpawnPoints();
            //Instantiate(ancientBossCorruptPrefab, playerSpawnPoints.ancientBossCorruptSpawnPoint.position, playerSpawnPoints.ancientBossCorruptSpawnPoint.rotation).GetComponent<NetworkObject>().Spawn(true);
            //Instantiate(ancientBossLightPrefab, playerSpawnPoints.ancientBossLightSpawnPoint.position, playerSpawnPoints.ancientBossLightSpawnPoint.rotation).GetComponent<NetworkObject>().Spawn(true);
            //Instantiate(ancientBossNeutralPrefab, playerSpawnPoints.ancientBossNeutralSpawnPoint.position, playerSpawnPoints.ancientBossNeutralSpawnPoint.rotation).GetComponent<NetworkObject>().Spawn(true);
        }

        public bool IsViEssenceSpawned()
        {
            return false;
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

