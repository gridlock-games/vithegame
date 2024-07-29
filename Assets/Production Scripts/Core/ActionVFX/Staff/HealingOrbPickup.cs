using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.VFX;

namespace Vi.Core.VFX.Staff
{
    public class HealingOrbPickup : GameInteractiveActionVFX
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.enabled = IsServer;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (!PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, attacker))
                {
                    networkCollider.Attributes.AddHP(networkCollider.Attributes.GetMaxHP());
                    NetworkObject.Despawn();
                }
            }
        }
    }
}