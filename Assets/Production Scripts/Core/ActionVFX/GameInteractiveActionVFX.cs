using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    public class GameInteractiveActionVFX : ActionVFX
    {
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;
        [SerializeField] private bool shouldBlockProjectiles;
        [SerializeField] private bool shouldDestroyOnEnemyHit;

        public bool ShouldBlockProjectiles() { return shouldBlockProjectiles; }

        protected Attributes attacker;
        protected ActionClip attack;

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    NetworkObject netObj = Instantiate(prefab.gameObject, transform.position, transform.rotation).GetComponent<NetworkObject>();
                    netObj.Spawn();
                    if (netObj.TryGetComponent(out FollowUpVFX vfx)) { vfx.Initialize(attacker, attack); }
                    PersistentLocalObjects.Singleton.StartCoroutine(WeaponHandler.DespawnVFXAfterPlaying(netObj));
                }
            }
        }

        public virtual void OnHit(Attributes attacker)
        {
            if (!IsSpawned) { return; }
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    NetworkObject.Despawn(true);
                }
            }
        }
    }
}