using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Core.GameModeManagers
{
    public class TeamEliminationViEssence : MonoBehaviour
    {
        public void Initialize(PlayerDataManager.Team losingTeam, PlayerDataManager.Team winningTeam)
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 1);
        }
    }
}