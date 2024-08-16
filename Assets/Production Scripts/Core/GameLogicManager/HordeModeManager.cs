using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.CombatAgents;

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

        private readonly PlayerDataManager.Team mobTeam = PlayerDataManager.Team.Environment;

        private new void Awake()
        {
            base.Awake();
            roundDuration = 180;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            roundResultMessage.Value = "Entering Corrupted Abyss! ";
        }

        protected override void OnRoundStart()
        {
            base.OnRoundStart();
            foreach (Mob mob in waves[GetRoundCount()-1].mobPrefabs)
            {
                SpawnMob(mob, mobTeam);
            }
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);

            if (gameOver.Value) { return; }

            if (winningPlayersDataIds.Length == 0) // Mobs killed all players
            {
                roundResultMessage.Value = "The Corruption Consumes You. ";
            }
            else // Players won
            {
                roundResultMessage.Value = "Wave Defeated. ";
            }
        }
    }
}