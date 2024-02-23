using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
                List<Attributes> teammates = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attributes.GetTeam());
                if (teammates.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 1)
                {
                    PlayerDataManager.Singleton.RevivePlayer(teammates[Random.Range(0, teammates.Count)]);
                    NetworkObject.Despawn(true);
                }
                else if (attributes.GetComponent<WeaponHandler>().IsAttacking)
                {
                    damageCircle.Shrink();
                    NetworkObject.Despawn(true);
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