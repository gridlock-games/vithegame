using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;
using Unity.Collections;
using Vi.Core.MovementHandlers;

namespace Vi.Core
{
    [RequireComponent(typeof(StatusAgent))]
    [RequireComponent(typeof(AnimationHandler))]
    [RequireComponent(typeof(LoadoutManager))]
    public abstract class CombatAgent : HittableAgent
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
        public AnimationHandler AnimationHandler { get; private set; }
        public PhysicsMovementHandler MovementHandler { get; private set; }
        public WeaponHandler WeaponHandler { get; private set; }
        public LoadoutManager LoadoutManager { get; private set; }
        public GlowRenderer GlowRenderer { get; private set; }
        protected virtual void Awake()
        {
            StatusAgent = GetComponent<StatusAgent>();
            AnimationHandler = GetComponent<AnimationHandler>();
            MovementHandler = GetComponent<PhysicsMovementHandler>();
            WeaponHandler = GetComponent<WeaponHandler>();
            LoadoutManager = GetComponent<LoadoutManager>();
        }

        [SerializeField] private PooledObject worldSpaceLabelPrefab;
        protected PooledObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            ailment.OnValueChanged += OnAilmentChanged;
            HP.OnValueChanged += OnHPChanged;

            if (!IsLocalPlayer) { worldSpaceLabelInstance = ObjectPoolingManager.SpawnObject(worldSpaceLabelPrefab, transform); }

            PlayerDataManager.Singleton.AddCombatAgent(this);
        }

        public override void OnNetworkDespawn()
        {
            ailment.OnValueChanged -= OnAilmentChanged;
            HP.OnValueChanged -= OnHPChanged;

            if (worldSpaceLabelInstance) { ObjectPoolingManager.ReturnObjectToPool(ref worldSpaceLabelInstance); }

            PlayerDataManager.Singleton.RemoveCombatAgent(this);

            if (IsServer)
            {
                SetInviniciblity(0);
                SetUninterruptable(0);
                ResetAilment();
            }
        }

        protected virtual void OnEnable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
            GlowRenderer = GetComponentInChildren<GlowRenderer>();
        }

        protected virtual void OnDisable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
            GlowRenderer = null;

            invincibilityEndTime = default;
            uninterruptableEndTime = default;

            damageMappingThisLife.Clear();

            knockupHitCounter = default;
            lastAttackingCombatAgent = default;

            wasStaggeredThisFrame = default;

            shouldShake = default;
            hitFreezeStartTime = Mathf.NegativeInfinity;
        }

        public virtual CharacterReference.RaceAndGender GetRaceAndGender() { return CharacterReference.RaceAndGender.Universal; }

        public NetworkCollider NetworkCollider { get; private set; }

        public void SetNetworkCollider(NetworkCollider networkCollider) { NetworkCollider = networkCollider; }

        public virtual bool IsInvincible() { return isInvincible.Value; }

        private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>();
        private float invincibilityEndTime;
        public void SetInviniciblity(float duration) { invincibilityEndTime = Time.time + duration; }

        public bool IsUninterruptable() { return isUninterruptable.Value; }

        private NetworkVariable<bool> isUninterruptable = new NetworkVariable<bool>();
        private float uninterruptableEndTime;
        public void SetUninterruptable(float duration) { uninterruptableEndTime = Time.time + duration; }

        protected NetworkVariable<ulong> grabAssailantDataId = new NetworkVariable<ulong>();
        protected NetworkVariable<ulong> grabVictimDataId = new NetworkVariable<ulong>();
        protected NetworkVariable<FixedString64Bytes> grabAttackClipName = new NetworkVariable<FixedString64Bytes>();
        protected NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>();
        protected NetworkVariable<bool> isGrabbing = new NetworkVariable<bool>();

        public void SetGrabVictim(ulong grabVictimNetworkObjectId) { grabVictimDataId.Value = grabVictimNetworkObjectId; }

        public bool IsGrabbed() { return isGrabbed.Value; }

        public bool IsGrabbing() { return isGrabbing.Value; }

        public void SetIsGrabbingToTrue() { isGrabbing.Value = true; }

        public CombatAgent GetGrabAssailant()
        {
            NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(grabAssailantDataId.Value, out NetworkObject networkObject);
            if (!networkObject) { return null; }
            return networkObject.GetComponent<CombatAgent>();
        }

        public CombatAgent GetGrabVictim()
        {
            NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(grabVictimDataId.Value, out NetworkObject networkObject);
            if (!networkObject) { return null; }
            return networkObject.GetComponent<CombatAgent>();
        }

        public AnimationClip GetGrabReactionClip()
        {
            CombatAgent grabAssailant = GetGrabAssailant();
            if (!grabAssailant) { Debug.LogError("No Grab Assailant Found!"); return null; }

            ActionClip grabAttackClip = grabAssailant.WeaponHandler.GetWeapon().GetActionClipByName(grabAttackClipName.Value.ToString());

            if (!grabAttackClip.grabVictimClip) { Debug.LogError("Couldn't find grab reaction clip!"); }
            return grabAttackClip.grabVictimClip;
        }

        public void CancelGrab()
        {
            if (IsGrabbed() | IsGrabbing())
            {
                if (grabResetCoroutine != null) { StopCoroutine(grabResetCoroutine); }
                isGrabbed.Value = false;
                isGrabbing.Value = false;
                grabAssailantDataId.Value = default;
                grabVictimDataId.Value = default;
            }

            if (AnimationHandler.IsGrabAttacking())
            {
                AnimationHandler.CancelAllActions(0.15f);
            }
        }

        protected Coroutine grabResetCoroutine;
        protected IEnumerator ResetGrabAfterAnimationPlays(ActionClip attack, ActionClip hitReaction)
        {
            if (hitReaction.ailment != ActionClip.Ailment.Grab) { Debug.LogError("Attributes.ResetGrabAfterAnimationPlays() should only be called with a grab hit reaction clip!"); yield break; }
            if (grabResetCoroutine != null) { StopCoroutine(grabResetCoroutine); }

            float durationLeft = attack.grabVictimClip.length + hitReaction.transitionTime;
            while (true)
            {
                durationLeft -= Time.deltaTime;
                yield return null;
                if (durationLeft <= 0) { break; }
            }
            if (GetGrabAssailant()) { GetGrabAssailant().CancelGrab(); }
            CancelGrab();
        }

        protected NetworkVariable<ulong> pullAssailantDataId = new NetworkVariable<ulong>();
        protected NetworkVariable<bool> isPulled = new NetworkVariable<bool>();

        public bool IsPulled() { return isPulled.Value; }

        public CombatAgent GetPullAssailant()
        {
            NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(pullAssailantDataId.Value, out NetworkObject networkObject);
            if (!networkObject) { Debug.LogError("Could not find pull assailant! " + pullAssailantDataId.Value); return null; }
            return networkObject.GetComponent<CombatAgent>();
        }

        protected virtual void Update()
        {
            if (IsServer & IsSpawned)
            {
                bool evaluateInvinicibility = true;
                bool evaluateUninterruptability = true;
                if (AnimationHandler.IsActionClipPlaying(WeaponHandler.CurrentActionClip))
                {
                    if (WeaponHandler.CurrentActionClip.isUninterruptable) { isUninterruptable.Value = true; evaluateUninterruptability = false; }
                    if (WeaponHandler.CurrentActionClip.isInvincible) { isInvincible.Value = true; evaluateInvinicibility = false; }
                }

                if (evaluateInvinicibility) { isInvincible.Value = Time.time <= invincibilityEndTime; }
                if (evaluateUninterruptability) { isUninterruptable.Value = Time.time <= uninterruptableEndTime; }
            }

            bool isInvincibleThisFrame = IsInvincible();
            if (!isInvincibleThisFrame)
            {
                if (!IsLocalPlayer)
                {
                    if (AnimationHandler.IsGrabAttacking())
                    {
                        isInvincibleThisFrame = true;
                    }
                    else if (IsGrabbed())
                    {
                        CombatAgent grabAssailant = GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (!grabAssailant.IsLocalPlayer) { isInvincibleThisFrame = true; }
                        }
                    }
                }
            }

            if (!GlowRenderer) { GlowRenderer = GetComponentInChildren<GlowRenderer>(); }

            if (GlowRenderer)
            {
                GlowRenderer.RenderInvincible(isInvincibleThisFrame);
                GlowRenderer.RenderUninterruptable(IsUninterruptable());
            }
        }

        public void OnActivateRage()
        {
            if (!CanActivateRage()) { return; }
            ActivateRage();
        }

        public const float ragingStaminaCostMultiplier = 1.25f;
        public bool IsRaging() { return isRaging.Value; }
        protected NetworkVariable<bool> isRaging = new NetworkVariable<bool>();
        private void ActivateRage()
        {
            if (!IsSpawned) { Debug.LogError("Calling Attributes.ActivateRage() before this object is spawned!"); return; }

            if (IsServer)
            {
                if (!CanActivateRage()) { return; }
                isRaging.Value = true;
            }
            else
            {
                ActivateRageServerRpc();
            }
        }

        public bool CanActivateRage() { return GetRage() / GetMaxRage() >= 1 & ailment.Value != ActionClip.Ailment.Death; }

        [Rpc(SendTo.Server)] private void ActivateRageServerRpc() { ActivateRage(); }

        public void ResetStats(float hpPercentage, bool resetStamina, bool resetSpirit, bool resetRage)
        {
            damageMappingThisLife.Clear();
            HP.Value = GetMaxHP() * hpPercentage;
            if (resetStamina)
                stamina.Value = 0;
            if (resetSpirit)
                spirit.Value = GetMaxSpirit();
            if (resetRage)
                rage.Value = 0;
        }

        protected Dictionary<CombatAgent, float> damageMappingThisLife = new Dictionary<CombatAgent, float>();

        public Dictionary<CombatAgent, float> GetDamageMappingThisLife() { return damageMappingThisLife; }

        protected void AddDamageToMapping(CombatAgent attacker, float damage)
        {
            if (damageMappingThisLife.ContainsKey(attacker))
            {
                damageMappingThisLife[attacker] += damage;
            }
            else
            {
                damageMappingThisLife.Add(attacker, damage);
            }
        }

        public const float minStaminaPercentageToBeAbleToBlock = 0.3f;

        protected const float notBlockingSpiritHitReactionPercentage = 0.4f;
        protected const float blockingSpiritHitReactionPercentage = 0.5f;

        protected const float rageDamageMultiplier = 1.15f;

        protected int knockupHitCounter;
        protected const int knockupHitLimit = 5;

        protected const float stunDuration = 3;
        protected const float knockdownDuration = 2;
        protected const float knockupDuration = 4;
        protected const float attackerRageToBeAddedOnHit = 2;
        protected const float victimRageToBeAddedOnHit = 1;

        protected CombatAgent lastAttackingCombatAgent;

        protected NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>(Quaternion.Euler(0, 0, 0)); // Don't remove the Quaternion.Euler() call, for some reason it's necessary BLACK MAGIC
        public bool ShouldApplyAilmentRotation() { return (ailment.Value != ActionClip.Ailment.None & ailment.Value != ActionClip.Ailment.Pull) | IsGrabbed(); }
        public Quaternion GetAilmentRotation() { return ailmentRotation.Value; }

        protected abstract void EvaluateAilment(ActionClip.Ailment attackAilment, bool applyAilmentRegardless, Vector3 hitSourcePosition, CombatAgent attacker, ActionClip attack, ActionClip hitReaction);

        protected NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        public ActionClip.Ailment GetAilment() { return ailment.Value; }
        public void ResetAilment() { ailment.Value = ActionClip.Ailment.None; }

        protected Coroutine ailmentResetCoroutine;
        protected virtual void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            AnimationHandler.Animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }

            if (current == ActionClip.Ailment.Death)
            {
                StartCoroutine(ClearDamageMappingAfter1Frame());
                WeaponHandler.OnDeath();
                AnimationHandler.OnDeath();
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
                if (IsServer) { isRaging.Value = false; }
                AnimationHandler.SetRagdollActive(true);
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                AnimationHandler.OnRevive();
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
                AnimationHandler.SetRagdollActive(false);
            }
        }

        private IEnumerator ClearDamageMappingAfter1Frame()
        {
            yield return null;
            damageMappingThisLife.Clear();
            lastAttackingCombatAgent = null;
        }

        [HideInInspector] public bool wasStaggeredThisFrame;
        protected IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        protected (bool, ActionClip.Ailment) GetAttackAilment(ActionClip attack, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter)
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
            if (!IsServer) { Debug.LogError("CombatAgent.StartHitStop() should only be called on the server!"); return; }

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

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void StartHitStopClientRpc(ulong attackerNetObjId)
        {
            CombatAgent attacker = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<CombatAgent>();

            shouldShake = true;
            attacker.shouldShake = false;

            hitFreezeStartTime = Time.time;
            attacker.hitFreezeStartTime = Time.time;
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void StartHitStopClientRpc()
        {
            shouldShake = true;
            hitFreezeStartTime = Time.time;
        }

        public PooledObject GetHitVFXPrefab() { return WeaponHandler.GetWeapon().hitVFXPrefab; }
        protected PooledObject GetBlockVFXPrefab() { return WeaponHandler.GetWeapon().blockVFXPrefab; }
        public AudioClip GetHitSoundEffect(Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment) { return WeaponHandler.GetWeapon().GetInflictHitSoundEffect(armorType, weaponBone, ailment); }
        protected AudioClip GetBlockingHitSoundEffect(Weapon.WeaponMaterial attackingWeaponMaterial) { return WeaponHandler.GetWeapon().GetBlockingHitSoundEffect(attackingWeaponMaterial); }

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

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
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

            if (GlowRenderer)
            {
                GlowRenderer.RenderHit();
            }
            else
            {
                Debug.LogError("No Glow Renderer! " + this);
                return;
            }
            RenderHitGlowOnlyClientRpc();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void RenderHitGlowOnlyClientRpc()
        {
            if (GlowRenderer)
            {
                GlowRenderer.RenderHit();
            }
            else
            {
                Debug.LogError("No Glow Renderer! " + this);
            }
        }

        protected void RenderBlock(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            if (GlowRenderer)
            {
                GlowRenderer.RenderBlock();
            }
            else
            {
                Debug.LogError("No Glow Renderer! " + this);
            }
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetBlockVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);

            RenderBlockClientRpc(impactPosition, attackingWeaponMaterial);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void RenderBlockClientRpc(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            if (GlowRenderer)
            {
                GlowRenderer.RenderBlock();
            }
            else
            {
                Debug.LogError("No Glow Renderer! " + this);
            }
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetBlockVFXPrefab(), impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);
        }
    }
}
