using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Vi.Core.GameModeManagers;
using Vi.ScriptableObjects;
using Vi.Utility;
using System.Linq;

namespace Vi.Core.CombatAgents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponHandler))]
    public class Attributes : CombatAgent
    {
        private NetworkVariable<int> playerDataId = new NetworkVariable<int>();
        public int GetPlayerDataId() { return playerDataId.Value; }
        public void SetPlayerDataId(int id) { playerDataId.Value = id; name = PlayerDataManager.Singleton.GetPlayerData(id).character.name.ToString(); }
        public override PlayerDataManager.Team GetTeam() { return CachedPlayerData.team; }

        public override string GetName() { return CachedPlayerData.character.name.ToString(); }

        public override CharacterReference.RaceAndGender GetRaceAndGender() { return CachedPlayerData.character.raceAndGender; }

        private NetworkVariable<bool> spawnedOnOwnerInstance = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsSpawnedOnOwnerInstance() { return spawnedOnOwnerInstance.Value; }

        public PlayerDataManager.PlayerData CachedPlayerData { get; private set; }

        public void SetCachedPlayerData(PlayerDataManager.PlayerData playerData)
        {
            if (playerData.id != GetPlayerDataId()) { Debug.LogError("Player data doesn't have the same id!"); return; }
            CachedPlayerData = playerData;
        }

        public override float GetMaxHP() { return WeaponHandler.GetWeapon().GetMaxHP(); }
        public override float GetMaxStamina() { return WeaponHandler.GetWeapon().GetMaxStamina(); }
        public override float GetMaxSpirit() { return WeaponHandler.GetWeapon().GetMaxSpirit(); }
        public override float GetMaxRage() { return WeaponHandler.GetWeapon().GetMaxRage(); }

        public override void AddStamina(float amount, bool activateCooldown = true)
        {
            if (activateCooldown)
                staminaDelayCooldown = WeaponHandler.GetWeapon().GetStaminaDelay();

            if (amount > 0)
            {
                if (stamina.Value < GetMaxStamina())
                {
                    stamina.Value = Mathf.Clamp(stamina.Value + amount, 0, GetMaxStamina());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (stamina.Value > GetMaxStamina())
                {
                    stamina.Value += amount;
                }
                else
                {
                    stamina.Value = Mathf.Clamp(stamina.Value + amount, 0, GetMaxStamina());
                }
            }
        }

        private void AddSpirit(float amount)
        {
            if (amount < 0) { amount *= StatusAgent.SpiritReductionMultiplier; }
            if (amount > 0) { amount *= StatusAgent.SpiritIncreaseMultiplier; }

            if (amount > 0)
            {
                if (spirit.Value < GetMaxSpirit())
                {
                    spirit.Value = Mathf.Clamp(spirit.Value + amount, 0, GetMaxSpirit());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (spirit.Value > GetMaxSpirit())
                {
                    spirit.Value += amount;
                }
                else
                {
                    spirit.Value = Mathf.Clamp(spirit.Value + amount, 0, GetMaxSpirit());
                }
            }
        }

        public override void AddRage(float amount)
        {
            if (amount > 0)
            {
                if (rage.Value < GetMaxRage())
                {
                    rage.Value = Mathf.Clamp(rage.Value + amount, 0, GetMaxRage());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (rage.Value > GetMaxRage())
                {
                    rage.Value += amount;
                }
                else
                {
                    rage.Value = Mathf.Clamp(rage.Value + amount, 0, GetMaxRage());
                }
            }
        }

        [SerializeField] private PooledObject teamIndicatorPrefab;
        private PooledObject teamIndicatorInstance;

        public override void OnNetworkSpawn()
        {
            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));
            base.OnNetworkSpawn();

            if (NetworkManager.Singleton.IsServer)
            {
                UpdateNetworkVisiblity();
                StartCoroutine(InitStats());
            }

            StartCoroutine(AddPlayerObjectToPlayerDataManager());

            spirit.OnValueChanged += OnSpiritChanged;
            rage.OnValueChanged += OnRageChanged;
            isRaging.OnValueChanged += OnIsRagingChanged;
            comboCounter.OnValueChanged += OnComboCounterChange;

            if (IsOwner) { spawnedOnOwnerInstance.Value = true; }

            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
            {
                foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                {
                    foreach (Collider col in NetworkCollider.Colliders)
                    {
                        foreach (Collider otherCol in attributes.NetworkCollider.Colliders)
                        {
                            Physics.IgnoreCollision(col, otherCol, true);
                        }
                    }
                }
            }
            RefreshStatus();

            teamIndicatorInstance = ObjectPoolingManager.SpawnObject(teamIndicatorPrefab, transform);
        }

        public void UpdateNetworkVisiblity()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Attributes.UpdateNetworkVisibility() should only be called on the server!"); return; }
            if (!gameObject.activeInHierarchy) { return; }
            StartCoroutine(SetNetworkVisibilityAfterSpawn());
        }

        private IEnumerator SetNetworkVisibilityAfterSpawn()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Attributes.SetNetworkVisibilityAfterSpawn() should only be called on the server!"); yield break; }
            yield return null;
            if (!IsSpawned) { yield return new WaitUntil(() => IsSpawned); }

            if (!NetworkObject.IsNetworkVisibleTo(OwnerClientId)) { NetworkObject.NetworkShow(OwnerClientId); }

            PlayerDataManager.PlayerData thisPlayerData = PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId());
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
            {
                ulong networkId = playerData.id >= 0 ? (ulong)playerData.id : 0;
                if (networkId == 0) { continue; }
                if (networkId == OwnerClientId) { continue; }

                if (playerData.channel == thisPlayerData.channel)
                {
                    if (!NetworkObject.IsNetworkVisibleTo(networkId))
                    {
                        NetworkObject.NetworkShow(networkId);
                    }
                }
                else
                {
                    if (NetworkObject.IsNetworkVisibleTo(networkId))
                    {
                        NetworkObject.NetworkHide(networkId);
                    }
                }
            }
        }

        private IEnumerator InitStats()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("You should onyl call Attributes.InitStats() on the server!"); yield break; }
            yield return new WaitUntil(() => IsSpawned);
            yield return new WaitUntil(() => WeaponHandler.WeaponInitialized);
            HP.Value = WeaponHandler.GetWeapon().GetMaxHP();
            spirit.Value = WeaponHandler.GetWeapon().GetMaxSpirit();
        }

        private IEnumerator AddPlayerObjectToPlayerDataManager()
        {
            yield return new WaitUntil(() => IsSpawned);
            if (!(IsHost & IsLocalPlayer)) { yield return new WaitUntil(() => GetPlayerDataId() != (int)NetworkManager.ServerClientId); }
            PlayerDataManager.Singleton.AddPlayerObject(GetPlayerDataId(), this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            spirit.OnValueChanged -= OnSpiritChanged;
            rage.OnValueChanged -= OnRageChanged;
            isRaging.OnValueChanged -= OnIsRagingChanged;
            comboCounter.OnValueChanged -= OnComboCounterChange;

            PlayerDataManager.Singleton.RemovePlayerObject(GetPlayerDataId());

            ObjectPoolingManager.ReturnObjectToPool(ref teamIndicatorInstance);
        }

        [SerializeField] private AudioClip heartbeatSoundEffect;
        private const float heartbeatVolume = 1;
        private const float heartbeatHPPercentageThreshold = 0.1f;

        protected override void OnHPChanged(float prev, float current)
        {
            base.OnHPChanged(prev, current);
            if (IsLocalPlayer)
            {
                if (current / GetMaxHP() < heartbeatHPPercentageThreshold)
                {
                    if (!heartbeatSoundIsPlaying) { StartCoroutine(PlayHeartbeatSound()); }
                }
            }
        }

        private bool heartbeatSoundIsPlaying;
        private IEnumerator PlayHeartbeatSound()
        {
            heartbeatSoundIsPlaying = true;
            AudioSource audioSource = AudioManager.Singleton.Play2DClip(gameObject, heartbeatSoundEffect, heartbeatVolume);

            while (true)
            {
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                if (GetAilment() == ActionClip.Ailment.Death) { break; }
                if (GetHP() / GetMaxHP() >= heartbeatHPPercentageThreshold) { break; }
                yield return null;
            }

            if (audioSource) { if (audioSource.isPlaying) { audioSource.Stop(); } }
            heartbeatSoundIsPlaying = false;
        }

        private void OnSpiritChanged(float prev, float current)
        {
            if (Mathf.Approximately(current, 0))
            {
                spiritRegenActivateTime = Time.time;
            }
        }

        public void StartSpiritRegen()
        {
            if (!IsServer) { Debug.LogError("Attributes.StartSpiritRegen() should only be called on the server!"); return; }
            spiritRegenActivateTime = Time.time;
        }

        private const float rageEndPercent = 0.01f;

        [SerializeField] private PooledObject rageAtMaxVFXPrefab;
        [SerializeField] private PooledObject ragingVFXPrefab;
        private PooledObject rageAtMaxVFXInstance;
        private PooledObject ragingVFXInstance;
        private void OnRageChanged(float prev, float current)
        {
            float currentRagePercent = GetRage() / GetMaxRage();
            if (currentRagePercent >= 1)
            {
                if (!rageAtMaxVFXInstance) { rageAtMaxVFXInstance = ObjectPoolingManager.SpawnObject(rageAtMaxVFXPrefab, AnimationHandler.Animator.GetBoneTransform(HumanBodyBones.Hips)); }
            }
            else
            {
                if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref rageAtMaxVFXInstance); }
            }

            if (IsServer)
            {
                if (currentRagePercent < rageEndPercent)
                {
                    isRaging.Value = false;
                }
            }
        }

        private void OnIsRagingChanged(bool prev, bool current)
        {
            if (current)
            {
                if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref rageAtMaxVFXInstance); }
                if (!ragingVFXInstance) { ragingVFXInstance = ObjectPoolingManager.SpawnObject(ragingVFXPrefab, AnimationHandler.Animator.GetBoneTransform(HumanBodyBones.Hips)); }
            }
            else
            {
                if (ragingVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref ragingVFXInstance); }
            }
        }

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        protected override void Awake()
        {
            base.Awake();
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CachedPlayerData = default;

            if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref rageAtMaxVFXInstance); }
            if (ragingVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref ragingVFXInstance); }

            lastComboCounterChangeTime = default;
            lastBlockTime = Mathf.NegativeInfinity;

            staminaDelayCooldown = default;

            spiritRegenActivateTime = Mathf.NegativeInfinity;

            rageDelayCooldown = default;

            IsRespawning = false;
            isWaitingForSpawnPoint = false;
            respawnSelfCalledTime = default;
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessMeleeHit() should only be called on the server!"); return false; }
            return ProcessHit(true, attacker, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessProjectileHit() should only be called on the server!"); return false; }
            return ProcessHit(false, attacker, attack, impactPosition, hitSourcePosition, hitCounter, runtimeWeapon, damageMultiplier);
        }

        public override bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamage() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                ailment.Value = ActionClip.Ailment.Death;
                AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingCombatAgent)
                {
                    SetKiller(lastAttackingCombatAgent);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(lastAttackingCombatAgent, this); }
                }
                else
                {
                    killerNetObjId.Value = attackingNetworkObject ? attackingNetworkObject.NetworkObjectId : 0;
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            RenderHitGlowOnly();
            AddHP(damage);
            return true;
        }

        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamageWithHitReaction() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            ActionClip.Ailment attackAilment = ActionClip.Ailment.None;
            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                attackAilment = ActionClip.Ailment.Death;
                ailment.Value = ActionClip.Ailment.Death;
                AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingCombatAgent)
                {
                    SetKiller(lastAttackingCombatAgent);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(lastAttackingCombatAgent, this); }
                }
                else
                {
                    killerNetObjId.Value = attackingNetworkObject ? attackingNetworkObject.NetworkObjectId : 0;
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            else
            {
                ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReactionByDirection(Weapon.HitLocation.Front);
                AnimationHandler.PlayAction(hitReaction);
            }

            RenderHit(attackingNetworkObject.NetworkObjectId, transform.position, AnimationHandler.GetArmorType(), Weapon.WeaponBone.Root, attackAilment);
            AddHP(damage);
            return true;
        }

        public void AddHitToComboCounter() { comboCounter.Value++; }

        private const float comboCounterResetTime = 3;

        private NetworkVariable<int> comboCounter = new NetworkVariable<int>();
        private float lastComboCounterChangeTime;

        private void OnComboCounterChange(int prev, int current)
        {
            lastComboCounterChangeTime = Time.time;
        }

        public int GetComboCounter() { return comboCounter.Value; }

        public void ResetComboCounter()
        {
            if (!IsServer) { Debug.LogError("Reset combo counter should only be called on the server!"); return; }
            comboCounter.Value = 0;
        }

        private float lastBlockTime = Mathf.NegativeInfinity;
        public bool BlockedRecently()
        {
            if (!IsServer) { Debug.LogError("Attributes.BlockedRecently will not be evaluated properly if we aren't the server!"); return false; }
            return Time.time - lastBlockTime <= 0.25f;
        }

        private bool ProcessHit(bool isMeleeHit, CombatAgent attacker, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
        {
            if (isMeleeHit)
            {
                if (!runtimeWeapon) { Debug.LogError("When processing a melee hit, you need to pass in a runtime weapon!"); return false; }
                if (GetAilment() == ActionClip.Ailment.Death | attacker.GetAilment() == ActionClip.Ailment.Death) { return false; }
            }

            // Make grab people invinicible to all attacks except for the grab hits
            if (IsGrabbed)
            {
                if (attack.GetClipType() != ActionClip.ClipType.GrabAttack) { return false; }
                if (attacker != GetGrabAssailant()) { return false; }
            }
            if (AnimationHandler.IsGrabAttacking()) { return false; }

            // Don't let grab attack hit players that aren't grabbed
            if (attack.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                if (attacker.GetGrabVictim() != this) { return false; }
            }

            if (!PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
                }
                return false;
            }

            if (attack.maxHitLimit == 0) { return false; }

            if (IsInvincible) { return false; }
            if (isMeleeHit)
            {
                if (attacker.wasStaggeredThisFrame) { return false; }

                if (!IsUninterruptable)
                {
                    wasStaggeredThisFrame = true;
                    StartCoroutine(ResetStaggerBool());
                }
            }

            (bool applyAilmentRegardless, ActionClip.Ailment attackAilment) = GetAttackAilment(attack, hitCounter);

            if (IsUninterruptable) { attackAilment = ActionClip.Ailment.None; }

            if (attackAilment == ActionClip.Ailment.Grab) { hitSourcePosition = attacker.MovementHandler.GetPosition(); }

            AddStamina(-attack.staminaDamage);
            if (!attacker.IsRaging) { attacker.AddRage(attackerRageToBeAddedOnHit); }
            if (!IsRaging) { AddRage(victimRageToBeAddedOnHit); }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, WeaponHandler.IsBlocking, attackAilment, ailment.Value);
            hitReaction.SetHitReactionRootMotionMultipliers(attack);

            float HPDamage = -attack.damage;
            HPDamage *= attacker.StatusAgent.DamageMultiplier;
            HPDamage *= damageMultiplier;

            bool shouldPlayHitReaction = false;
            switch (hitReaction.GetHitReactionType())
            {
                case ActionClip.HitReactionType.Normal:
                    if ((GetSpirit() + HPDamage * 0.7f) / GetMaxSpirit() >= notBlockingSpiritHitReactionPercentage)
                    {
                        AddSpirit(HPDamage * 0.7f);
                        HPDamage *= 0.7f;
                    }
                    else if ((GetSpirit() + HPDamage * 0.7f) / GetMaxSpirit() > 0)
                    {
                        AddSpirit(HPDamage * 0.7f);
                        shouldPlayHitReaction = true;
                        HPDamage *= 0.7f;
                    }
                    else // Spirit is at 0
                    {
                        AddSpirit(HPDamage);
                        shouldPlayHitReaction = true;
                    }
                    break;
                case ActionClip.HitReactionType.Blocking:
                    lastBlockTime = Time.time;
                    if ((GetSpirit() + HPDamage * 0.7f) / GetMaxSpirit() >= blockingSpiritHitReactionPercentage) // If spirit is greater than or equal to 50%
                    {
                        AddSpirit(HPDamage * 0.5f);
                        HPDamage = 0;
                    }
                    else if ((GetSpirit() + HPDamage * 0.7f) / GetMaxSpirit() > 0) // If spirit is greater than 0% and less than 50%
                    {
                        AddSpirit(-GetMaxSpirit());
                        AddStamina(-GetMaxStamina() * 0.3f);
                        shouldPlayHitReaction = true;
                        HPDamage *= 0.7f;
                    }
                    else // Spirit is at 0
                    {
                        AddStamina(-GetMaxStamina() * 0.3f);
                        AddSpirit(HPDamage);
                        if (GetStamina() <= 0)
                        {
                            if (attackAilment == ActionClip.Ailment.None) { attackAilment = ActionClip.Ailment.Stagger; }
                            hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, false, attackAilment, ailment.Value);
                            hitReaction.SetHitReactionRootMotionMultipliers(attack);
                        }
                        shouldPlayHitReaction = true;
                    }
                    break;
                default:
                    Debug.Log("Unsure how to process hit for hit reaction type " + hitReaction.GetHitReactionType());
                    break;
            }

            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                HPDamage *= attacker.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (IsRaging) { HPDamage *= rageDamageMultiplier; }

            bool hitReactionWasPlayed = false;
            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                hitReaction = WeaponHandler.GetWeapon().GetDeathReaction();

                if (IsGrabbed)
                {
                    if (GetGrabAssailant()) { GetGrabAssailant().CancelGrab(); }
                    CancelGrab();
                }

                AnimationHandler.PlayAction(hitReaction);
                hitReactionWasPlayed = true;
            }
            else if (!IsUninterruptable)
            {
                if (hitReaction.ailment == ActionClip.Ailment.Grab)
                {
                    grabAttackClipName.Value = attack.name;
                    grabAssailantDataId.Value = attacker.NetworkObjectId;
                    attacker.SetGrabVictim(NetworkObjectId);
                    isGrabbed.Value = true;
                    attacker.SetIsGrabbingToTrue();
                    attacker.AnimationHandler.PlayAction(attacker.WeaponHandler.GetWeapon().GetGrabAttackClip(attack));
                }

                if (hitReaction.ailment == ActionClip.Ailment.None)
                {
                    if (!IsGrabbed & !IsRaging)
                    {
                        if (attack.shouldPlayHitReaction
                            | ailment.Value != ActionClip.Ailment.None // For knockup follow up attacks
                            | AnimationHandler.IsCharging()
                            | shouldPlayHitReaction) // For spirit logic
                        {
                            AnimationHandler.PlayAction(hitReaction);
                            hitReactionWasPlayed = true;
                        }
                    }
                }
                else // Hit reaction ailment isn't None
                {
                    AnimationHandler.PlayAction(hitReaction);
                    hitReactionWasPlayed = true;
                }
            }

            if (runtimeWeapon) { runtimeWeapon.AddHit(this); }

            StartHitStop(attacker, isMeleeHit);

            if (hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                RenderBlock(impactPosition, runtimeWeapon ? runtimeWeapon.GetWeaponMaterial() : Weapon.WeaponMaterial.Metal);
                float prevHP = GetHP();
                AddHP(HPDamage);
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                AddDamageToMapping(attacker, prevHP - GetHP());
            }
            else // Not blocking
            {
                if (!Mathf.Approximately(HPDamage, 0))
                {
                    RenderHit(attacker.NetworkObjectId, impactPosition, AnimationHandler.GetArmorType(), runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root, attackAilment);
                    float prevHP = GetHP();
                    AddHP(HPDamage);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                    AddDamageToMapping(attacker, prevHP - GetHP());
                }

                EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attack, hitReaction);
            }

            if (attacker is Attributes attributes) { attributes.AddHitToComboCounter(); }

            if (IsServer)
            {
                foreach (ActionVFX actionVFX in attack.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        WeaponHandler.SpawnActionVFX(WeaponHandler.CurrentActionClip, actionVFX, attacker.transform, transform);
                    }
                }
            }

            if (!IsUninterruptable)
            {
                if (attack.shouldFlinch | IsRaging)
                {
                    if (!hitReactionWasPlayed & !IsGrabbed)
                    {
                        AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetFlinchClip(attackAngle));
                        MovementHandler.Flinch(attack.GetFlinchAmount());
                    }
                }
            }

            lastAttackingCombatAgent = attacker;
            return true;
        }

        public ulong GetRoundTripTime() { return roundTripTime.Value; }

        private NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        protected void RefreshStatus()
        {
            if (IsSpawned & IsOwner)
            {
                pingEnabled.Value = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");
            }
        }

        private NetworkVariable<bool> pingEnabled = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected override void Update()
        {
            base.Update();

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (Time.time - lastComboCounterChangeTime >= comboCounterResetTime) { comboCounter.Value = 0; }

            bool canRegenStats = true;
            if (GameModeManager.Singleton)
            {
                canRegenStats = !GameModeManager.Singleton.WaitingToPlayGame();
            }

            if (canRegenStats)
            {
                UpdateStamina();
                UpdateRage();

                // Regen for 50 seconds
                if (Time.time - spiritRegenActivateTime <= 50 & !WeaponHandler.IsBlocking) { UpdateSpirit(); }
            }

            if (pingEnabled.Value) { roundTripTime.Value = networkTransport.GetCurrentRtt(OwnerClientId); }
        }

        private float staminaDelayCooldown;
        private void UpdateStamina()
        {
            staminaDelayCooldown = Mathf.Max(0, staminaDelayCooldown - Time.deltaTime);
            if (staminaDelayCooldown > 0) { return; }
            AddStamina(WeaponHandler.GetWeapon().GetStaminaRecoveryRate() * Time.deltaTime, false);
        }

        private float spiritRegenActivateTime = Mathf.NegativeInfinity;
        private const float spiritRegenRate = 3;
        private void UpdateSpirit()
        {
            AddSpirit(spiritRegenRate * Time.deltaTime);
        }

        private const float rageDepletionRate = 1;
        private float rageDelayCooldown;
        private void UpdateRage()
        {
            if (IsRaging)
            {
                AddRage(-rageDepletionRate * Time.deltaTime);
            }

            rageDelayCooldown = Mathf.Max(0, rageDelayCooldown - Time.deltaTime);
            if (rageDelayCooldown > 0) { return; }
            AddRage(WeaponHandler.GetWeapon().GetRageRecoveryRate() * Time.deltaTime);
        }

        protected override void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            base.OnAilmentChanged(prev, current);
            if (current == ActionClip.Ailment.Death)
            {
                spiritRegenActivateTime = Mathf.NegativeInfinity;
                respawnCoroutine = StartCoroutine(RespawnSelf());
                AnimationHandler.Animator.enabled = false;
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                if (respawnCoroutine != null)
                {
                    IsRespawning = false;
                    StopCoroutine(respawnCoroutine);
                }
                AnimationHandler.Animator.enabled = true;
            }
        }

        public float GetRespawnTime() { return Mathf.Clamp(GameModeManager.Singleton.GetRespawnTime() - (Time.time - respawnSelfCalledTime), 0, GameModeManager.Singleton.GetRespawnTime()); }
        public float GetRespawnTimeAsPercentage()
        {
            if (GetRespawnTime() <= 5)
            {
                if (GameModeManager.Singleton.GetRespawnTime() <= 5)
                {
                    return 1 - (GetRespawnTime() / GameModeManager.Singleton.GetRespawnTime());
                }
                else
                {
                    return 1 - (GetRespawnTime() / 5);
                }
            }
            else
            {
                return 0;
            }
        }

        public bool IsRespawning { get; private set; }
        [HideInInspector] public bool isWaitingForSpawnPoint;
        private Coroutine respawnCoroutine;
        private float respawnSelfCalledTime;
        private IEnumerator RespawnSelf()
        {
            if (!GameModeManager.Singleton) { yield break; }
            if (GameModeManager.Singleton.GetRespawnTime() <= 0) { yield break; }
            IsRespawning = true;
            respawnSelfCalledTime = Time.time;
            yield return new WaitForSeconds(GameModeManager.Singleton.GetRespawnTime());
            if (IsServer & !GameModeManager.Singleton.IsGameOver())
            {
                yield return PlayerDataManager.Singleton.RespawnPlayer(this);
            }
            yield return new WaitUntil(() => ailment.Value != ActionClip.Ailment.Death);
            IsRespawning = false;
        }
    }
}