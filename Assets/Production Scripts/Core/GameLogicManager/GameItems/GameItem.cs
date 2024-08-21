using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;

namespace Vi.Core.GameModeManagers
{
    [RequireComponent(typeof(PooledObject))]
    public class GameItem : NetworkBehaviour, IHittable
    {
        protected const float gameItemVolume = 1;

        protected virtual bool OnHit(CombatAgent attacker)
        {
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