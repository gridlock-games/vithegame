using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.CombatAgents;
using Unity.Netcode;
using Vi.Core.Structures;
using System.Linq;

namespace Vi.Core.GameModeManagers
{
    public class HordeModeManager : GameModeManager
    {
        [SerializeField] private Wave[] waves = new Wave[0];
        
        [System.Serializable]
        private class Wave
        {
            public Mob[] mobPrefabs;
        }

        private readonly PlayerDataManager.Team mobTeam = PlayerDataManager.Team.Corruption;

        private new void Awake()
        {
            base.Awake();
            numberOfRoundsWinsToWinGame = waves.Length;

            roundAboutToStartPrefix = "Wave ";
            roundAboutToStartSuffix = " Incoming. ";
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                roundResultMessage.Value = "Entering Corrupted Abyss! ";
            }
        }

        List<Mob> currentlySpawnedMobs = new List<Mob>();
        protected override void OnRoundStart()
        {
            base.OnRoundStart();

            List<Mob> mobsToRemove = new List<Mob>();
            foreach (Mob mob in currentlySpawnedMobs)
            {
                if (mob.IsSpawned) { mob.NetworkObject.Despawn(true); }
                mobsToRemove.Add(mob);
            }
            currentlySpawnedMobs.RemoveAll(item => mobsToRemove.Contains(item));

            foreach (Mob mob in waves[GetRoundCount() - 1].mobPrefabs)
            {
                currentlySpawnedMobs.Add(SpawnMob(mob, mobTeam, true));
            }
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            if (winningPlayersDataIds.Length == 0) // Mobs killed all players
            {
                roundResultMessage.Value = "The Corruption Consumes You. ";
                OnGameEnd(winningPlayersDataIds);
            }
            else // Players won
            {
                roundResultMessage.Value = gameOver.Value ? "Corruption Cleared! " : "Wave Defeated. ";
                wavesCompleted.Value++;

                foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                {
                    attributes.SessionProgressionHandler.AddExperience(waveCompletionExperienceReward);
                }
            }
        }

        private const float waveCompletionExperienceReward = 100;

        private NetworkVariable<int> wavesCompleted = new NetworkVariable<int>();
        public int GetWavesCompleted() { return wavesCompleted.Value; }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            gameEndMessage.Value = "Game Over! ";
        }

        protected override void OnGameOverChanged(bool prev, bool current)
        {
            base.OnGameOverChanged(prev, current);
            if (!current) { return; }

            if (IsClient)
            {
                if (PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator)
                {
                    PlayerScore localPlayerScore = GetPlayerScore(PlayerDataManager.Singleton.LocalPlayerData.id);

                    PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.LeaderboardManager.SendHordeModeLeaderboardResult(
                        PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(),
                        PlayerDataManager.Singleton.LocalPlayerData.character.name.ToString(),
                        PlayerDataManager.Singleton.GetGameMode(),
                        roundTimer.Value, GetWavesCompleted(),
                        localPlayerScore.cumulativeDamageDealt));
                }
            }

            ViEssenceEarnedFromMatch = GetWavesCompleted() * 2;
        }

        public override void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            base.OnPlayerKill(killer, victim);
            if (gameOver.Value) { return; }
            if (victim.GetTeam() != PlayerDataManager.Team.Corruption) { return; }
            List<CombatAgent> killerTeam = PlayerDataManager.Singleton.GetCombatAgentsOnTeam(killer.GetTeam());
            List<CombatAgent> victimTeam = PlayerDataManager.Singleton.GetCombatAgentsOnTeam(victim.GetTeam());
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
        }

        public override void OnEnvironmentKill(CombatAgent victim)
        {
            base.OnEnvironmentKill(victim);
            if (gameOver.Value) { return; }
            PlayerDataManager.Team opposingTeam = victim.GetTeam() == PlayerDataManager.Team.Light ? PlayerDataManager.Team.Corruption : PlayerDataManager.Team.Light;
            List<CombatAgent> victimTeam = PlayerDataManager.Singleton.GetCombatAgentsOnTeam(victim.GetTeam());
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
        }

        public override void OnStructureKill(CombatAgent killer, Structure structure)
        {
            base.OnStructureKill(killer, structure);
            if (gameOver.Value) { return; }
            PlayerDataManager.Team opposingTeam = PlayerDataManager.Team.Corruption;
            List<Attributes> opposingTeamPlayers = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(opposingTeam);
            List<int> winningPlayerIds = new List<int>();
            foreach (Attributes attributes in opposingTeamPlayers)
            {
                winningPlayerIds.Add(attributes.GetPlayerDataId());
            }
            OnRoundEnd(winningPlayerIds.ToArray());
        }

        public bool ShouldDisplayEssenceUI { get; private set; }

        protected override void OnNextGameActionTimerThreeFourths()
        {
            base.OnNextGameActionTimerThreeFourths();
            if (!gameOver.Value & GetRoundCount() > 0)
            {
                ShouldDisplayEssenceUI = true;
            }
        }

        protected override void OnNextGameActionTimerOneFourth()
        {
            base.OnNextGameActionTimerOneFourth();
            ShouldDisplayEssenceUI = false;
        }
    }
}