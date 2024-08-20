using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core.Structures
{
    public class Structure : HittableAgent
    {
        [SerializeField] private float maxHP = 100;
        [SerializeField] private PlayerDataManager.Team team = PlayerDataManager.Team.Competitor;

        private NetworkVariable<float> HP = new NetworkVariable<float>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            HP.OnValueChanged += OnHPChanged;
            if (IsServer)
            {
                HP.Value = maxHP;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            HP.OnValueChanged -= OnHPChanged;
        }

        public float GetHP() { return HP.Value; }
        public float GetMaxHP() { return maxHP; }

        public void AddHP(float amount)
        {
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

        private void OnHPChanged(float prev, float current)
        {
            Debug.Log(current + " " + prev + " STRUCTURE");
            if (prev > 0 & Mathf.Approximately(current, 0))
            {

            }
        }

        protected bool OnHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            float HPDamage = -attack.damage;
            HPDamage *= attacker.StatusAgent.DamageMultiplier;

            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                HPDamage *= attacker.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            AddHP(HPDamage);
            return true;
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return OnHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return OnHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        public override bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject) { return false; }
        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject) { return false; }
    }
}