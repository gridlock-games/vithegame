using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.VFX;

namespace Vi.Core.VFX.Staff
{
    public class HealingOrbPickup : GameInteractiveActionVFX
    {
        private float serverSpawnTime;
        private const float healingOrbDuration = 3;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { serverSpawnTime = Time.time; }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (!PlayerDataManager.Singleton.CanHit(networkCollider.CombatAgent, attacker))
                {
                    if (!Mathf.Approximately(networkCollider.CombatAgent.GetHP(), networkCollider.CombatAgent.GetMaxHP()))
                    {
                        networkCollider.CombatAgent.AddHP(networkCollider.CombatAgent.GetMaxHP() * 0.05f);
                        NetworkObject.Despawn();
                    }
                }
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                if (Time.time - serverSpawnTime > healingOrbDuration) { NetworkObject.Despawn(true); }
            }
        }
    }
}