using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class Mob : CombatAgent
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public void SetTeam(PlayerDataManager.Team team)
        {
            this.team.Value = team;
        }

        [SerializeField] private float maxHP = 100;

        public override float GetMaxHP() { return maxHP; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                HP.Value = maxHP;
            }
        }

        public override PlayerDataManager.Team GetTeam() { return team.Value; }

        public override string GetName() { return name.Replace("(Clone)", ""); }

        public override Color GetRelativeTeamColor()
        {
            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;

            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.GetTeamColor(GetTeam());
            }
            else
            {
                return localTeam == GetTeam() ? Color.cyan : Color.red;
            }
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            throw new System.NotImplementedException();
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            throw new System.NotImplementedException();
        }
    }
}