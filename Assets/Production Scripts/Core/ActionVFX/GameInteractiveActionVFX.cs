using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core.VFX
{
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

        protected CombatAgent attacker;
        protected ActionClip attack;

        public CombatAgent GetAttacker() { return attacker; }
        public ActionClip GetAttack() { return attack; }

        public virtual void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            this.attacker = attacker;
            this.attack = attack;
        }

        protected new void OnDisable()
        {
            base.OnDisable();
            attacker = null;
            attack = null;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    NetworkObject netObj = ObjectPoolingManager.SpawnObject(prefab.GetComponent<PooledObject>(), transform.position, transform.rotation).GetComponent<NetworkObject>();
                    netObj.Spawn(true);
                    if (netObj.TryGetComponent(out FollowUpVFX vfx)) { vfx.InitializeVFX(attacker, attack); }
                }
            }
        }

        public virtual void OnHit(CombatAgent attacker)
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