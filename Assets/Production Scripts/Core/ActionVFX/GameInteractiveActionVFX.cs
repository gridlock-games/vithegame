using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core.VFX
{
    public class GameInteractiveActionVFX : ActionVFX, IHittable
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

        private CombatAgent attacker;
        private ActionClip attack;
        private NetworkVariable<ulong> attackerNetworkObjectId = new NetworkVariable<ulong>();

        public CombatAgent GetAttacker()
        {
            if (attacker)
            {
                return attacker;
            }
            else
            {
                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetworkObjectId.Value, out NetworkObject networkObject))
                {
                    if (networkObject) { return networkObject.GetComponent<CombatAgent>(); }
                }
            }
            return null;
        }

        public ActionClip GetAttack()
        {
            if (!IsServer) { Debug.LogError("GameInteractiveActionVFX.GetAttack() should only be called on the server!"); }
            return attack;
        }

        public virtual void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("GameInteractiveActionVFX.InitializeVFX() should only be called on the server!"); }
            this.attacker = attacker;
            this.attack = attack;
            attackerNetworkObjectId.Value = attacker.NetworkObjectId;
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

            attacker = null;
            attack = null;
            if (IsServer) { attackerNetworkObjectId.Value = default; }
        }

        protected virtual bool OnHit(CombatAgent attacker)
        {
            if (!IsSpawned) { return false; }
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    NetworkObject.Despawn(true);
                    return true;
                }
            }
            return false;
        }

        public bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return OnHit(attacker);
        }

        public bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return OnHit(attacker);
        }

        public bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject) { return false; }
        public bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject) { return false; }
    }
}