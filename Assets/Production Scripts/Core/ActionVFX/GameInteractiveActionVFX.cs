using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Unity.Netcode.Components;

namespace Vi.Core
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public class GameInteractiveActionVFX : ActionVFX
    {
        public enum SpellType
        {
            NotASpell,
            GroundSpell,
            AerialSpell
        }

        public SpellType GetSpellType() { return spellType; }

        [SerializeField] protected SpellType spellType = SpellType.NotASpell;
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;
        [SerializeField] private bool shouldBlockProjectiles;
        [SerializeField] private bool shouldDestroyOnEnemyHit;

        public bool ShouldBlockProjectiles() { return shouldBlockProjectiles; }

        protected Attributes attacker;
        protected ActionClip attack;

        public Attributes GetAttacker() { return attacker; }
        public ActionClip GetAttack() { return attack; }

        public virtual void InitializeVFX(Attributes attacker, ActionClip attack)
        {
            this.attacker = attacker;
            this.attack = attack;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    NetworkObject netObj = Instantiate(prefab.gameObject, transform.position, transform.rotation).GetComponent<NetworkObject>();
                    netObj.SpawnWithOwnership(OwnerClientId, true);
                    if (netObj.TryGetComponent(out FollowUpVFX vfx)) { vfx.InitializeVFX(attacker, attack); }
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