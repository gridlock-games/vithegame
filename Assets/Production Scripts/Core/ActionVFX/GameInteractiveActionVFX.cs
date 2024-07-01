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
        [SerializeField] private bool shouldDestroyOnEnemyHit;

        protected Attributes attacker;
        protected ActionClip attack;

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    GameObject g = Instantiate(prefab.gameObject, transform.position, transform.rotation);
                    g.GetComponent<NetworkObject>().Spawn();
                    if (g.TryGetComponent(out FollowUpVFX vfx)) { vfx.Initialize(attacker, attack); }
                }
            }
        }

        public virtual void OnHit(Attributes attacker)
        {
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    ObjectPoolingManager.ReturnObjectToPool(gameObject);
                }
            }
        }
    }
}