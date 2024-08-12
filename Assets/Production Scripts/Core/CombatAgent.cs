using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(StatusAgent))]
    public abstract class CombatAgent : NetworkBehaviour
    {
        protected NetworkVariable<float> HP = new NetworkVariable<float>();
        protected NetworkVariable<float> stamina = new NetworkVariable<float>();
        protected NetworkVariable<float> spirit = new NetworkVariable<float>();
        protected NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }
        public float GetStamina() { return stamina.Value; }
        public float GetSpirit() { return spirit.Value; }
        public float GetRage() { return rage.Value; }

        public abstract float GetMaxHP();
        public abstract float GetMaxStamina();
        public abstract float GetMaxSpirit();
        public abstract float GetMaxRage();

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

        public virtual void AddStamina(float amount, bool activateCooldown = true) { }

        public virtual void AddRage(float amount) { }

        protected virtual void OnHPChanged(float prev, float current)
        {
            if (current < prev)
            {
                if (current <= 0)
                {
                    // Death
                    //ailment.Value = ActionClip.Ailment.Death;
                }
            }
            else if (current > prev)
            {
                GlowRenderer.RenderHeal();
            }
        }

        public StatusAgent StatusAgent { get; private set; }
        protected void Awake()
        {
            StatusAgent = GetComponent<StatusAgent>();
            GlowRenderer = GetComponent<GlowRenderer>();
        }

        public GlowRenderer GlowRenderer { get; private set; }
        protected void OnTransformChildrenChanged()
        {
            if (!GlowRenderer) { GlowRenderer = GetComponentInChildren<GlowRenderer>(); }
        }

        [SerializeField] private PooledObject worldSpaceLabelPrefab;
        protected PooledObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            ailment.OnValueChanged += OnAilmentChanged;
            HP.OnValueChanged += OnHPChanged;

            if (!IsLocalPlayer) { worldSpaceLabelInstance = ObjectPoolingManager.SpawnObject(worldSpaceLabelPrefab, transform); }
        }

        public override void OnNetworkDespawn()
        {
            ailment.OnValueChanged -= OnAilmentChanged;
            HP.OnValueChanged -= OnHPChanged;

            if (worldSpaceLabelInstance) { ObjectPoolingManager.ReturnObjectToPool(worldSpaceLabelInstance); }
        }

        protected void OnEnable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
        }

        protected void OnDisable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
        }

        public abstract string GetName();
        public abstract PlayerDataManager.Team GetTeam();

        public abstract Color GetRelativeTeamColor();

        public NetworkCollider NetworkCollider { get; private set; }

        public void SetNetworkCollider(NetworkCollider networkCollider) { NetworkCollider = networkCollider; }

        public abstract void SetInviniciblity(float duration);

        public abstract void SetUninterruptable(float duration);


        protected CombatAgent lastAttackingCombatAgent;
        public abstract bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition);

        public abstract bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1);

        public abstract bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject);

        public abstract bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject);

        protected NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        public ActionClip.Ailment GetAilment() { return ailment.Value; }
        public void ResetAilment() { ailment.Value = ActionClip.Ailment.None; }
        protected virtual void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current) { }

        public virtual bool IsInvincible() { return false; }
        public virtual bool IsUninterruptable() { return false; }

        [HideInInspector] public bool wasStaggeredThisFrame;
        protected IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        protected (bool, ActionClip.Ailment) GetAttackAilment(ActionClip attack, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter)
        {
            // Combination ailment logic here
            bool applyAilmentRegardless = false;
            ActionClip.Ailment attackAilment;
            // These hit numbers are BEFORE the hit has been added to the weapon
            if (hitCounter.ContainsKey(this))
            {
                if (attack.ailmentHitDefinition.Length > hitCounter[this].hitNumber)
                {
                    if (attack.ailmentHitDefinition[hitCounter[this].hitNumber]) // If we are in the ailment hit definition and it is true
                    {
                        attackAilment = attack.ailment;
                    }
                    else // If we are in the ailment hit definition, but it is false
                    {
                        attackAilment = ActionClip.Ailment.None;
                    }
                }
                else // If we are out of the range of the ailment hit array
                {
                    attackAilment = attack.ailment;
                }
            }
            else // First hit
            {
                if (attack.ailmentHitDefinition.Length > 0)
                {
                    if (attack.ailmentHitDefinition[0]) // If we are in the ailment hit definition and it is true
                    {
                        attackAilment = attack.ailment;
                    }
                    else // If we are in the ailment hit definition, but it is false
                    {
                        attackAilment = ActionClip.Ailment.None;
                    }
                }
                else // If the ailment hit definition array is empty
                {
                    attackAilment = attack.ailment;
                }
            }

            if (ailment.Value == ActionClip.Ailment.Stun & attack.isFollowUpAttack) { attackAilment = ActionClip.Ailment.Stagger; }
            if (ailment.Value == ActionClip.Ailment.Stun & attackAilment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Stun & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockup; }

            if (ailment.Value == ActionClip.Ailment.Stagger & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }

            if (ailment.Value == ActionClip.Ailment.Knockup & attackAilment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attack.GetClipType() == ActionClip.ClipType.FlashAttack) { attackAilment = ActionClip.Ailment.Knockup; applyAilmentRegardless = true; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attack.isFollowUpAttack) { attackAilment = ActionClip.Ailment.Knockup; applyAilmentRegardless = true; }

            return (applyAilmentRegardless, attackAilment);
        }

        public bool ShouldPlayHitStop()
        {
            return Time.time - hitFreezeStartTime < ActionClip.HitStopEffectDuration;
        }

        public bool ShouldShake()
        {
            return (Time.time - hitFreezeStartTime < ActionClip.HitStopEffectDuration) & shouldShake;
        }

        public const float ShakeAmount = 10;

        private bool shouldShake;
        private float hitFreezeStartTime = Mathf.NegativeInfinity;
        protected void StartHitStop(CombatAgent attacker, bool isMeleeHit)
        {
            if (!IsServer) { Debug.LogError("Attributes.StartHitStop() should only be called on the server!"); return; }

            if (isMeleeHit)
            {
                shouldShake = true;
                attacker.shouldShake = false;

                hitFreezeStartTime = Time.time;
                attacker.hitFreezeStartTime = Time.time;

                StartHitStopClientRpc(attacker.NetworkObjectId);
            }
            else
            {
                shouldShake = true;
                hitFreezeStartTime = Time.time;

                StartHitStopClientRpc();
            }
        }

        [Rpc(SendTo.NotServer)]
        private void StartHitStopClientRpc(ulong attackerNetObjId)
        {
            CombatAgent attacker = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>();

            shouldShake = true;
            attacker.shouldShake = false;

            hitFreezeStartTime = Time.time;
            attacker.hitFreezeStartTime = Time.time;
        }

        [Rpc(SendTo.NotServer)]
        private void StartHitStopClientRpc()
        {
            shouldShake = true;
            hitFreezeStartTime = Time.time;
        }

        protected abstract PooledObject GetHitVFXPrefab();
        protected abstract PooledObject GetBlockVFXPrefab();
        protected abstract AudioClip GetHitSoundEffect(Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment);
        protected abstract AudioClip GetBlockingHitSoundEffect(Weapon.WeaponMaterial attackingWeaponMaterial);

        protected void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            GlowRenderer.RenderHit();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject,
                NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>().GetHitSoundEffect(armorType, weaponBone, ailment),
                impactPosition, Weapon.hitSoundEffectVolume);

            RenderHitClientRpc(attackerNetObjId, impactPosition, armorType, weaponBone, ailment);
        }

        [Rpc(SendTo.NotServer)]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            GlowRenderer.RenderHit();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject,
                NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>().GetHitSoundEffect(armorType, weaponBone, ailment),
                impactPosition, Weapon.hitSoundEffectVolume);
        }

        protected void RenderHitGlowOnly()
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHitGlowOnly() should only be called from the server"); return; }

            GlowRenderer.RenderHit();

            RenderHitGlowOnlyClientRpc();
        }

        [Rpc(SendTo.NotServer)]
        private void RenderHitGlowOnlyClientRpc()
        {
            GlowRenderer.RenderHit();
        }

        protected void RenderBlock(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            GlowRenderer.RenderBlock();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetBlockVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);

            RenderBlockClientRpc(impactPosition, attackingWeaponMaterial);
        }

        [Rpc(SendTo.NotServer)]
        private void RenderBlockClientRpc(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            GlowRenderer.RenderBlock();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetBlockVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);
        }
    }
}
