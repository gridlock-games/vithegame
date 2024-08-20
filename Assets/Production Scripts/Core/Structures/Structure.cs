using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;

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
            PlayerDataManager.Singleton.AddStructure(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            HP.OnValueChanged -= OnHPChanged;
            PlayerDataManager.Singleton.RemoveStructure(this);
        }

        public override string GetName() { return name.Replace("(Clone)", ""); }
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

        [SerializeField] private Weapon.ArmorType armorType = Weapon.ArmorType.Metal;
        protected bool ProcessHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
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

            if (runtimeWeapon) { runtimeWeapon.AddHit(this); }

            RenderHit(attacker.NetworkObjectId, impactPosition, runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root);

            AddHP(HPDamage);
            return true;
        }

        protected void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, Weapon.WeaponBone weaponBone)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            CombatAgent attackingCombatAgent = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>();

            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(attackingCombatAgent.GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject,
                attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ActionClip.Ailment.None),
                impactPosition, Weapon.hitSoundEffectVolume);

            RenderHitClientRpc(attackerNetObjId, impactPosition, weaponBone);
        }

        [Rpc(SendTo.NotServer)]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, Weapon.WeaponBone weaponBone)
        {
            CombatAgent attackingCombatAgent = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>();

            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(attackingCombatAgent.GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject,
                attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ActionClip.Ailment.None),
                impactPosition, Weapon.hitSoundEffectVolume);
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return ProcessHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return ProcessHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        public override bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject) { return false; }
        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject) { return false; }
    }
}