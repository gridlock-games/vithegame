using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.Core
{
    public interface IHittable
    {
        public abstract bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition);
        public abstract bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1);
        public abstract bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject);
        public abstract bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject);

        public virtual bool ShouldBlockProjectiles() { return true; }
    }
}