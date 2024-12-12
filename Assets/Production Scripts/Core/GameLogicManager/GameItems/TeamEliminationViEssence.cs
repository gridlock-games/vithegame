using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.Core.DynamicEnvironmentElements;

namespace Vi.Core.GameModeManagers
{
    public class TeamEliminationViEssence : GameItem
    {
        [SerializeField] private AudioClip spawnSound;

        private TeamEliminationManager teamEliminationManager;
        private DamageCircle damageCircle;

        public void Initialize(TeamEliminationManager teamEliminationManager, DamageCircle damageCircle)
        {
            this.teamEliminationManager = teamEliminationManager;
            this.damageCircle = damageCircle;
        }

        public override void OnNetworkSpawn()
        {
            AudioManager.Singleton.PlayClipOnTransform(transform, spawnSound, true, gameItemVolume);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent is Attributes attributes)
                {
                    List<Attributes> teammates = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attributes.GetTeam(), attributes);
                    if (teammates.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count == 0)
                    {
                        PlayerDataManager.Singleton.RevivePlayer(teammates[Random.Range(0, teammates.Count)]);
                        teamEliminationManager.OnViEssenceActivation();
                        NetworkObject.Despawn(true);
                    }
                }
            }
        }

        protected override bool OnHit(CombatAgent attacker)
        {
            if (!IsServer) { Debug.LogError("TeamEliminationViEssence.OnHit() should only be called on the server!"); return false; }
            if (!IsSpawned) { return false; }

            List<Attributes> teammates = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attacker.GetTeam());
            // If the number of dead players on the attacker's team is greater than 1
            if (teammates.Where(item => item.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death).ToList().Count > 1)
            {
                damageCircle.Shrink();
                teamEliminationManager.OnViEssenceActivation();
                NetworkObject.Despawn(true);
                return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}