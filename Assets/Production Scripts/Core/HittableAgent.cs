using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Core.Weapons;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusAgent))]
    public abstract class HittableAgent : NetworkBehaviour, IHittable
    {
        public abstract bool ProcessMeleeHit(CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition);
        public abstract bool ProcessProjectileHit(CombatAgent attacker, NetworkObject attackingNetworkObject, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1);
        public abstract bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject, bool ignoresArmor = false);
        public abstract bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject);

        public abstract string GetName();
        public abstract PlayerDataManager.Team GetTeam();
        public abstract Weapon.ArmorType GetArmorType();

        public StatusAgent StatusAgent { get; private set; }
        protected virtual void Awake()
        {
            StatusAgent = GetComponent<StatusAgent>();
        }

        protected NetworkVariable<float> HP = new NetworkVariable<float>();
        public float GetHP() { return HP.Value; }

        public abstract float GetMaxHP();

        public void AddHP(float amount)
        {
            if (amount < 0) { amount *= StatusAgent.DamageReceivedMultiplier / StatusAgent.DamageReductionMultiplier; }
            if (amount > 0) { amount *= StatusAgent.HealingMultiplier; }

            if (amount > 0)
            {
                if (HP.Value < GetMaxHP())
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (HP.Value > GetMaxHP())
                {
                    HP.Value += amount;
                }
                else
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
        }

        protected float AddHPWithoutApply(float amount)
        {
            if (amount < 0) { amount *= StatusAgent.DamageReceivedMultiplier / StatusAgent.DamageReductionMultiplier; }
            if (amount > 0) { amount *= StatusAgent.HealingMultiplier; }

            if (amount > 0)
            {
                if (HP.Value < GetMaxHP())
                {
                    return Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (HP.Value > GetMaxHP())
                {
                    return HP.Value + amount;
                }
                else
                {
                    return Mathf.Clamp(HP.Value + amount, 0, GetMaxHP());
                }
            }
            return HP.Value;
        }
    }
}