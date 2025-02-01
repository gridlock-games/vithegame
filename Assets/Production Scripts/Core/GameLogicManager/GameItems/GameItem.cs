using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;
using Vi.Core.Weapons;

namespace Vi.Core.GameModeManagers
{
    [RequireComponent(typeof(PooledObject))]
    [RequireComponent(typeof(ObjectiveHandler))]
    public class GameItem : NetworkBehaviour, IHittable
    {
        protected const float gameItemVolume = 0.7f;

        public ObjectiveHandler ObjectiveHandler { get; private set; }

        protected virtual void Awake()
        {
            ObjectiveHandler = GetComponent<ObjectiveHandler>();
        }

        protected virtual bool OnHit(CombatAgent attacker)
        {
            return false;
        }

        public bool ProcessMeleeHit(CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return OnHit(attacker);
        }

        public bool ProcessProjectileHit(CombatAgent attacker, NetworkObject attackingNetworkObject, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return OnHit(attacker);
        }

        public bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject, bool ignoresArmor = false) { return false; }
        public bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject) { return false; }
    }
}