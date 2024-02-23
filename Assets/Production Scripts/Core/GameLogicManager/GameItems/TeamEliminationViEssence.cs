using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Core.GameModeManagers
{
    public class TeamEliminationViEssence : GameItem
    {
        private DamageCircle damageCircle;

        public void Initialize(DamageCircle damageCircle)
        {
            this.damageCircle = damageCircle;
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log(Time.time + " vi essence spawned " + transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }

            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                if (PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attributes.GetTeam()).Count == 1)
                {
                    Debug.Log(Time.time + " Revive");
                }
                else if (attributes.GetComponent<WeaponHandler>().IsAttacking)
                {
                    damageCircle.Shrink();
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}