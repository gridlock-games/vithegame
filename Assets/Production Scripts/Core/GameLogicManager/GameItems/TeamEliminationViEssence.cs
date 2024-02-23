using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Core.GameModeManagers
{
    public class TeamEliminationViEssence : GameItem
    {
        private DamageCircle damageCircle;
        private PlayerDataManager.Team losingTeam;
        private PlayerDataManager.Team winningTeam;

        public void Initialize(PlayerDataManager.Team losingTeam, PlayerDataManager.Team winningTeam, DamageCircle damageCircle)
        {
            this.damageCircle = damageCircle;
            this.losingTeam = losingTeam;
            this.winningTeam = winningTeam;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                Debug.Log(attributes.GetTeam() + " " + winningTeam + " " + losingTeam);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}