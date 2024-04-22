using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            roundResultMessage.Value = "Essence war starting! ";

            if (IsServer)
            {

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

