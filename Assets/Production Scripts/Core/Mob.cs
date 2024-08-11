using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class Mob : NetworkBehaviour
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public void SetTeam(PlayerDataManager.Team team)
        {
            this.team.Value = team;
        }

        [SerializeField] private float maxHP;
        private NetworkVariable<float> HP = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }

        public float GetMaxHP() { return maxHP; }

        private void AddHP(float amount)
        {
            if (amount > 0)
            {
                if (HP.Value < GetMaxHP())
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (HP.Value > GetMaxHP())
                {
                    HP.Value += amount;
                }
                else
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HP.Value = maxHP;
            }
        }
    }
}