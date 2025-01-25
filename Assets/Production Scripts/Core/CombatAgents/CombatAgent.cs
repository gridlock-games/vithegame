using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Vi.Core.CombatAgents;
using Vi.Core.GameModeManagers;
using Vi.Core.MovementHandlers;
using Vi.Core.VFX;
using Vi.Core.Weapons;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(AnimationHandler))]
    [RequireComponent(typeof(LoadoutManager))]
    [RequireComponent(typeof(SessionProgressionHandler))]
    public abstract class CombatAgent : HittableAgent
    {
        public bool ExcludeFromKillFeed { get { return excludeFromKillFeed; } }
        [SerializeField] private bool excludeFromKillFeed;

        public CombatAgent Master { get; private set; }
        public virtual void SetMaster(CombatAgent master)
        {
            Master = master;
            Master.slaves.Add(this);
        }

        public List<CombatAgent> GetSlaves() { return slaves.ToList(); }
        private List<CombatAgent> slaves = new List<CombatAgent>();

        protected NetworkVariable<float> stamina = new NetworkVariable<float>();
        protected NetworkVariable<float> spirit = new NetworkVariable<float>();
        protected NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetStamina() { return stamina.Value; }
        public float GetSpirit() { return spirit.Value; }
        public float GetRage() { return rage.Value; }

        public override float GetMaxHP() { return WeaponHandler.GetWeapon().GetMaxHP() + SessionProgressionHandler.MaxHPBonus; }
        public float GetMaxStamina() { return WeaponHandler.GetWeapon().GetMaxStamina() + SessionProgressionHandler.MaxStaminaBonus; }
        public float GetMaxSpirit() { return WeaponHandler.GetWeapon().GetMaxSpirit() + SessionProgressionHandler.MaxSpiritBonus; }
        public float GetMaxRage() { return WeaponHandler.GetWeapon().GetMaxRage(); }

        public void AddStamina(float amount, bool activateCooldown = true)
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

        protected virtual bool ShouldUseSpirit() { return true; }

        protected virtual bool ShouldUseRage() { return true; }

        public void AddSpirit(float amount)
        {
            if (!ShouldUseSpirit()) { return; }

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

        public void AddRage(float amount, bool clampPositive = true)
        {
            if (!ShouldUseRage()) { return; }

            if (amount > 0)
            {
                if (rage.Value < GetMaxRage())
                {
                    if (clampPositive)
                    {
                        rage.Value = Mathf.Clamp(rage.Value + amount, 0, GetMaxRage());
                    }
                    else
                    {
                        rage.Value += amount;
                    }
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
                if (GlowRenderer)
                {
                    GlowRenderer.RenderHeal();
                }
            }
        }

        public AnimationHandler AnimationHandler { get; private set; }
        public PhysicsMovementHandler MovementHandler { get; private set; }
        public WeaponHandler WeaponHandler { get; private set; }
        public LoadoutManager LoadoutManager { get; private set; }
        public GlowRenderer GlowRenderer { get; private set; }
        public SessionProgressionHandler SessionProgressionHandler { get; private set; }
        protected override void Awake()
        {
            base.Awake();
            AnimationHandler = GetComponent<AnimationHandler>();
            MovementHandler = GetComponent<PhysicsMovementHandler>();
            WeaponHandler = GetComponent<WeaponHandler>();
            LoadoutManager = GetComponent<LoadoutManager>();
            SessionProgressionHandler = GetComponent<SessionProgressionHandler>();

            if (TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.OnReturnToPool += OnReturnToPool;
            }
        }

        protected virtual void OnReturnToPool()
        {
            if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref rageAtMaxVFXInstance); }
            if (ragingVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref ragingVFXInstance); }
        }

        [SerializeField] private AudioClip rageStartAudio;
        protected virtual void OnIsRagingChanged(bool prev, bool current)
        {
            if (current)
            {
                if (!ragingVFXInstance)
                {
                    ragingVFXInstance = ObjectPoolingManager.SpawnObject(ragingVFXPrefab, AnimationHandler.LimbReferences.Hips);
                }

                AnimationHandler.ExecuteLogoEffects(rageStartSprite, rageStartVFXPrefab, rageStartAudio);

                if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref rageAtMaxVFXInstance); }
            }
            else
            {
                if (ragingVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ref ragingVFXInstance); }
            }
        }

        private const float rageEndPercent = 0.01f;

        [SerializeField] private PooledObject rageAtMaxVFXPrefab;
        private PooledObject rageAtMaxVFXInstance;
        private void OnRageChanged(float prev, float current)
        {
            float currentRagePercent = GetRage() / GetMaxRage();
            if (currentRagePercent >= 1)
            {
                if (!rageAtMaxVFXInstance) { rageAtMaxVFXInstance = ObjectPoolingManager.SpawnObject(rageAtMaxVFXPrefab, AnimationHandler.LimbReferences.Hips); }
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

        [SerializeField] private PooledObject worldSpaceLabelPrefab;
        protected PooledObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            ailment.OnValueChanged += OnAilmentChanged;
            HP.OnValueChanged += OnHPChanged;
            rage.OnValueChanged += OnRageChanged;

            LastAliveStartTime = Time.time;

            if (!IsLocalPlayer & !ExcludeFromKillFeed) { worldSpaceLabelInstance = ObjectPoolingManager.SpawnObject(worldSpaceLabelPrefab, transform); }

            PlayerDataManager.Singleton.AddCombatAgent(this);

            isRaging.OnValueChanged += OnIsRagingChanged;

            if (IsClient) { StartCoroutine(WaitForAnimator()); }

            if (IsServer)
            {
                StartCoroutine(InitStats());
            }

            NetworkCollider.OnNetworkSpawn();
        }

        private IEnumerator WaitForAnimator()
        {
            yield return new WaitUntil(() => AnimationHandler.Animator);
            OnAilmentChanged(ActionClip.Ailment.Death, ailment.Value);
        }

        private IEnumerator InitStats()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("You should onyl call Attributes.InitStats() on the server!"); yield break; }
            yield return new WaitUntil(() => IsSpawned);
            yield return new WaitUntil(() => WeaponHandler.WeaponInitialized);
            HP.Value = GetMaxHP();
            spirit.Value = GetMaxSpirit();
        }

        public override void OnNetworkDespawn()
        {
            ailment.OnValueChanged -= OnAilmentChanged;
            HP.OnValueChanged -= OnHPChanged;
            rage.OnValueChanged -= OnRageChanged;

            if (worldSpaceLabelInstance) { ObjectPoolingManager.ReturnObjectToPool(ref worldSpaceLabelInstance); }

            PlayerDataManager.Singleton.RemoveCombatAgent(this);

            if (IsServer)
            {
                SetInviniciblity(0);
                SetUninterruptable(0);
                ResetAilment();
            }

            isRaging.OnValueChanged -= OnIsRagingChanged;

            NetworkCollider.OnNetworkDespawn();
        }

        protected virtual void OnEnable()
        {
            GlowRenderer = GetComponentInChildren<GlowRenderer>();
            UpdateActivePlayersList();
        }

        [SerializeField] private PooledObject ragingVFXPrefab;
        [SerializeField] private PooledObject rageStartVFXPrefab;
        [SerializeField] private Sprite rageStartSprite;
        private PooledObject ragingVFXInstance;
        protected virtual void OnDisable()
        {
            ResetColliderRadiusPredicted = default;

            incapacitatedReviveTimeTracker.Clear();
            wasIncapacitatedThisLife = default;

            lastRecoveryFixedTime = Mathf.NegativeInfinity;

            GlowRenderer = null;

            invincibilityEndTime = default;
            uninterruptableEndTime = default;

            ClearDamageMapping();

            knockupHitCounter = default;
            lastAttackingCombatAgent = default;

            wasStaggeredThisFrame = default;

            shouldShake = default;
            hitFreezeStartTime = Mathf.NegativeInfinity;

            staminaDelayCooldown = default;
            rageDelayCooldown = default;

            Master = null;
            slaves.Clear();
        }

        public virtual CharacterReference.RaceAndGender GetRaceAndGender() { return CharacterReference.RaceAndGender.Universal; }

        public NetworkCollider NetworkCollider { get; private set; }

        public void SetNetworkCollider(NetworkCollider networkCollider) { NetworkCollider = networkCollider; }

        public virtual bool IsInvincible { get { return isInvincible.Value; } }

        private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>();
        private float invincibilityEndTime;
        public void SetInviniciblity(float duration)
        {
            invincibilityEndTime = Time.time + duration;
            if (IsSpawned & IsServer)
            {
                isInvincible.Value = Time.time <= invincibilityEndTime;
            }
        }

        public bool IsUninterruptable { get { return isUninterruptable.Value; } }

        private NetworkVariable<bool> isUninterruptable = new NetworkVariable<bool>();
        private float uninterruptableEndTime;
        public void SetUninterruptable(float duration)
        {
            uninterruptableEndTime = Time.time + duration;
            if (IsSpawned & IsServer)
            {
                isUninterruptable.Value = Time.time <= uninterruptableEndTime;
            }
        }

        protected NetworkVariable<ulong> grabAssailantDataId = new NetworkVariable<ulong>();
        protected NetworkVariable<ulong> grabVictimDataId = new NetworkVariable<ulong>();
        protected NetworkVariable<FixedString64Bytes> grabAttackClipName = new NetworkVariable<FixedString64Bytes>();
        protected NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>();
        protected NetworkVariable<bool> isGrabbing = new NetworkVariable<bool>();

        public void SetGrabVictim(ulong grabVictimNetworkObjectId) { grabVictimDataId.Value = grabVictimNetworkObjectId; }

        public bool IsGrabbed { get { return isGrabbed.Value; } }

        public bool IsGrabbing { get { return isGrabbing.Value; } }

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

        public ActionClip GetGrabReactionClip()
        {
            CombatAgent grabAssailant = GetGrabAssailant();
            if (!grabAssailant) { Debug.LogError("No Grab Assailant Found!"); return null; }
            ActionClip grabAttackClip = grabAssailant.WeaponHandler.GetWeapon().GetActionClipByName(grabAttackClipName.Value.ToString());
            return grabAttackClip;
        }

        private void CancelGrab()
        {
            if (!IsSpawned) { Debug.LogError("CombatAgent.CancelGrab() should only be called when spawned!"); return; }
            if (!IsServer) { Debug.LogError("CombatAgent.CancelGrab() should only be called on the server!"); return; }

            if (IsGrabbed | IsGrabbing)
            {
                if (grabResetCoroutine != null) { StopCoroutine(grabResetCoroutine); }
                isGrabbed.Value = false;
                isGrabbing.Value = false;
                grabAssailantDataId.Value = default;
                grabVictimDataId.Value = default;
            }

            if (AnimationHandler.IsGrabAttacking())
            {
                AnimationHandler.CancelAllActions(0.15f, false);
            }
        }

        private void StopGrabSequence()
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("CombatAgent.EndGrabSequence() should only be called on the server!"); return; }

            if (IsGrabbed)
            {
                CombatAgent attacker = GetGrabAssailant();
                if (attacker)
                {
                    attacker.CancelGrab();
                }
            }

            if (IsGrabbing)
            {
                CombatAgent victim = GetGrabVictim();
                if (victim)
                {
                    victim.CancelGrab();
                }
            }

            if (IsSpawned) { CancelGrab(); }
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

        protected NetworkVariable<ulong> pullAssailantFallbackNetworkObjectId = new NetworkVariable<ulong>();
        protected NetworkVariable<ulong> pullAssailantNetworkObjectId = new NetworkVariable<ulong>();
        protected NetworkVariable<bool> isPulled = new NetworkVariable<bool>();

        public bool IsPulled { get { return isPulled.Value; } }

        public Vector3 GetPullAssailantPosition()
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(pullAssailantNetworkObjectId.Value, out NetworkObject networkObject))
            {
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(pullAssailantFallbackNetworkObjectId.Value, out networkObject))
                {
                    Debug.LogWarning("Could not find pull assailant! " + pullAssailantNetworkObjectId.Value + " " + pullAssailantFallbackNetworkObjectId.Value);
                    return MovementHandler.GetPosition();
                }
            }

            if (networkObject.TryGetComponent(out MovementHandler movementHandler))
            {
                return movementHandler.GetPosition();
            }
            else if (networkObject.TryGetComponent(out NetworkTransform networkTransform))
            {
                return networkTransform.GetSpaceRelativePosition(true);
            }
            else if (networkObject.TryGetComponent(out Rigidbody rigidbody))
            {
                return rigidbody.position;
            }
            else
            {
                return transform.position;
            }
        }

        public List<Attributes> ActivePlayers { get; private set; } = new List<Attributes>();
        private void UpdateActivePlayersList() { ActivePlayers = PlayerDataManager.Singleton.GetActivePlayerObjects(); }

        private Dictionary<Attributes, float> incapacitatedReviveTimeTracker = new Dictionary<Attributes, float>();

        private bool CanReviveTeammateFromIncapacitation()
        {
            if (IsGrabbed) { return false; }
            return GetAilment() == ActionClip.Ailment.None;
        }

        protected virtual void FixedUpdate()
        {
            if (!IsSpawned) { return; }

            if (GetAilment() == ActionClip.Ailment.Incapacitated)
            {
                float maxProgress = 0;
                foreach (Attributes attributes in ActivePlayers)
                {
                    if (attributes == this) { continue; }

                    if (Time.fixedTime - attributes.lastRenderHitFixedTime < 0.1f)
                    {
                        if (incapacitatedReviveTimeTracker.ContainsKey(attributes)) { incapacitatedReviveTimeTracker.Remove(attributes); }
                        continue;
                    }

                    if (!attributes.CanReviveTeammateFromIncapacitation())
                    {
                        if (incapacitatedReviveTimeTracker.ContainsKey(attributes)) { incapacitatedReviveTimeTracker.Remove(attributes); }
                        continue;
                    }

                    if (PlayerDataManager.Singleton.CanHit(attributes, this))
                    {
                        if (incapacitatedReviveTimeTracker.ContainsKey(attributes)) { incapacitatedReviveTimeTracker.Remove(attributes); }
                        continue;
                    }

                    Vector3 a = attributes.NetworkCollider.GetClosestPoint(MovementHandler.GetPosition());
                    Vector3 b = NetworkCollider.GetClosestPoint(attributes.MovementHandler.GetPosition());

                    if (Vector3.Distance(a, b) < 1)
                    {
                        if (incapacitatedReviveTimeTracker.ContainsKey(attributes))
                        {
                            incapacitatedReviveTimeTracker[attributes] += Time.fixedDeltaTime;
                        }
                        else
                        {
                            incapacitatedReviveTimeTracker.Add(attributes, Time.fixedDeltaTime);
                        }

                        if (incapacitatedReviveTimeTracker[attributes] / 3 > maxProgress)
                        {
                            maxProgress = incapacitatedReviveTimeTracker[attributes] / 3;
                        }

                        if (IsServer)
                        {
                            if (incapacitatedReviveTimeTracker[attributes] >= 3)
                            {
                                ResetStats(0.25f, false, false, false);
                                ResetAilment();
                                break;
                            }
                        }
                    }
                    else if (incapacitatedReviveTimeTracker.ContainsKey(attributes))
                    {
                        incapacitatedReviveTimeTracker.Remove(attributes);
                    }
                }
                AnimationHandler.SetReviveImageProgress(maxProgress);
            }
            else
            {
                AnimationHandler.DisableReviveImage();
            }
        }

        protected virtual void Update()
        {
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }

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

            bool isInvincibleThisFrame = IsInvincible;
            if (!isInvincibleThisFrame)
            {
                if (!IsLocalPlayer)
                {
                    if (AnimationHandler.IsGrabAttacking())
                    {
                        isInvincibleThisFrame = true;
                    }
                    else if (IsGrabbed)
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
                GlowRenderer.RenderUninterruptable(IsUninterruptable);
            }

            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            bool canRegenStats = true;
            if (GameModeManager.Singleton)
            {
                canRegenStats = !GameModeManager.Singleton.WaitingToPlayGame();
            }

            if (GetAilment() == ActionClip.Ailment.Incapacitated)
            {
                canRegenStats = false;
            }

            if (canRegenStats)
            {
                UpdateStamina();
                UpdateRage();
            }
        }

        private float staminaDelayCooldown;
        private void UpdateStamina()
        {
            staminaDelayCooldown = Mathf.Max(0, staminaDelayCooldown - Time.deltaTime);
            if (staminaDelayCooldown > 0) { return; }
            AddStamina(WeaponHandler.GetWeapon().GetStaminaRecoveryRate() * Time.deltaTime, false);
        }

        private float rageDepletionRate { get { return GetMaxRage() / 60; } }
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

        public void OnActivateRage()
        {
            if (!CanActivateRage()) { return; }
            ActivateRage();
        }

        public bool CanTryActivateRageOrPotions()
        {
            if (GetAilment() == ActionClip.Ailment.Knockdown
                | GetAilment() == ActionClip.Ailment.Knockup
                | GetAilment() == ActionClip.Ailment.Stun
                | IsGrabbed
                | GetAilment() == ActionClip.Ailment.Incapacitated
                | GetAilment() == ActionClip.Ailment.Death)
            {
                return false;
            }
            return true;
        }

        public const float ragingStaminaCostMultiplier = 1.25f;
        public bool IsRaging { get { return isRaging.Value; } }
        protected NetworkVariable<bool> isRaging = new NetworkVariable<bool>();
        private void ActivateRage()
        {
            if (!IsSpawned) { Debug.LogError("Calling CombatAgent.ActivateRage() before this object is spawned!"); return; }

            if (!CanTryActivateRageOrPotions()) { return; }

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

        public void ActivateRageWithoutCheckingRageParam()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { Debug.LogError("Calling CombatAgent.ActivateRageWithoutCheckingRageParam() when you're not the server!"); return; }
            isRaging.Value = true;
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

        private Dictionary<CombatAgent, float> damageMappingThisLifeFromAliveAgents = new Dictionary<CombatAgent, float>();

        private static Dictionary<CombatAgent, HashSet<CombatAgent>> damageMappingCrosswalk = new Dictionary<CombatAgent, HashSet<CombatAgent>>();

        private Dictionary<CombatAgent, float> damageMappingThisLife = new Dictionary<CombatAgent, float>();

        public Dictionary<CombatAgent, float> GetDamageMappingThisLife()
        {
            if (damageMappingThisLife.Count(item => !item.Key.IsSpawned) > 0)
            {
                //Debug.LogWarning("Damage mapping this life has keys that aren't spawned in it, this should never happen");
                return damageMappingThisLife.Where(item => item.Key.IsSpawned).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                return damageMappingThisLife;
            }
        }

        public Dictionary<CombatAgent, float> GetDamageMappingThisLifeFromAliveAgents()
        {
            if (damageMappingThisLifeFromAliveAgents.Count(item => !item.Key.IsSpawned) > 0)
            {
                //Debug.LogWarning("Damage mapping this life from alive agents has keys that aren't spawned in it, this should never happen");
                return damageMappingThisLifeFromAliveAgents.Where(item => item.Key.IsSpawned).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                return damageMappingThisLifeFromAliveAgents;
            }
        }

        private void AddDamageToMapping(CombatAgent attacker, float damage)
        {
            if (damageMappingThisLife.ContainsKey(attacker))
            {
                damageMappingThisLife[attacker] += damage;
            }
            else
            {
                damageMappingThisLife.Add(attacker, damage);
            }

            if (damageMappingThisLifeFromAliveAgents.ContainsKey(attacker))
            {
                damageMappingThisLifeFromAliveAgents[attacker] += damage;
            }
            else
            {
                damageMappingThisLifeFromAliveAgents.Add(attacker, damage);
            }

            if (damageMappingCrosswalk.ContainsKey(attacker))
            {
                damageMappingCrosswalk[attacker].Add(this);
            }
            else
            {
                damageMappingCrosswalk.Add(attacker, new HashSet<CombatAgent>() { this });
            }
        }

        private void RemoveDamageMapping(CombatAgent attacker)
        {
            if (!attacker.IsSpawned)
            {
                damageMappingThisLife.Remove(attacker);
            }

            damageMappingThisLifeFromAliveAgents.Remove(attacker);
        }

        private void ClearDamageMapping()
        {
            damageMappingThisLife.Clear();
            damageMappingThisLifeFromAliveAgents.Clear();

            if (damageMappingCrosswalk.TryGetValue(this, out HashSet<CombatAgent> victimSet))
            {
                foreach (CombatAgent combatAgent in victimSet)
                {
                    combatAgent.RemoveDamageMapping(this);
                }
                damageMappingCrosswalk.Remove(this);
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
        protected NetworkVariable<ulong> killerNetObjId = new NetworkVariable<ulong>();

        public CombatAgent GetLastAttackingCombatAgent()
        {
            if (!lastAttackingCombatAgent) { return null; }
            if (!lastAttackingCombatAgent.IsSpawned) { return null; }
            if (lastAttackingCombatAgent.GetAilment() == ActionClip.Ailment.Death) { return null; }
            return lastAttackingCombatAgent;
        }

        protected void SetKiller(CombatAgent killer)
        {
            if (!IsServer | !IsSpawned) { Debug.LogError("Calling SetKiller when not the server! This is not allowed!"); return; }
            killerNetObjId.Value = killer.NetworkObjectId;

            if (killer.IsLocalPlayer)
            {
                if (killer.onKillAudioClip)
                {
                    AudioManager.Singleton.Play2DClip(gameObject, killer.onKillAudioClip, 0.75f);
                }
            }
            else if (killer.NetworkObject.IsPlayerObject)
            {
                killer.PlayKillerSoundEffectRpc(RpcTarget.Single(killer.OwnerClientId, RpcTargetUse.Temp));
            }
        }

        [SerializeField] private AudioClip onKillAudioClip;

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Unreliable)]
        private void PlayKillerSoundEffectRpc(RpcParams rpcParams)
        {
            if (onKillAudioClip)
            {
                AudioManager.Singleton.Play2DClip(gameObject, onKillAudioClip, 0.75f);
            }
        }

        public bool TryGetKiller(out NetworkObject killer)
        {
            killer = null;
            if (ailment.Value != ActionClip.Ailment.Death) { Debug.LogError("Trying to get killer while not dead!"); return false; }

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(killerNetObjId.Value))
                killer = NetworkManager.SpawnManager.SpawnedObjects[killerNetObjId.Value];

            return killer != null;
        }

        private NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>(Quaternion.Euler(0, 0, 0)); // Don't remove the Quaternion.Euler() call, for some reason it's necessary BLACK MAGIC
        public bool ShouldApplyAilmentRotation() { return ailment.Value != ActionClip.Ailment.None & ailment.Value != ActionClip.Ailment.Pull; }
        public Quaternion GetAilmentRotation() { return ailmentRotation.Value; }

        protected void EvaluateAilment(ActionClip.Ailment attackAilment, bool applyAilmentRegardless, Vector3 hitSourcePosition, CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, ActionClip hitReaction)
        {
            foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTargetOnHit)
            {
                if (status.associatedWithCurrentWeapon)
                {
                    Debug.LogWarning("Adding a status to a target on hit but it's associated with its current weapon " + attack + " " + attacker.WeaponHandler.GetWeapon() + " " + status.status);
                }
                StatusAgent.TryAddStatus(status);
            }

            // Ailments
            if (attackAilment != ailment.Value | applyAilmentRegardless)
            {
                bool shouldApplyAilment = false;
                if (attackAilment != ActionClip.Ailment.None)
                {
                    Vector3 startPos = transform.position;
                    Vector3 endPos = hitSourcePosition;
                    startPos.y = 0;
                    endPos.y = 0;
                    Vector3 rel = endPos - startPos;
                    ailmentRotation.Value = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel, Vector3.up);

                    shouldApplyAilment = true;

                    if (attackAilment == ActionClip.Ailment.Pull)
                    {
                        pullAssailantFallbackNetworkObjectId.Value = attacker.NetworkObjectId;
                        pullAssailantNetworkObjectId.Value = attackingNetworkObject.NetworkObjectId;
                        isPulled.Value = true;
                    }
                    else if (attackAilment == ActionClip.Ailment.Grab)
                    {
                        if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
                        ailment.Value = ActionClip.Ailment.None;
                    }
                    else
                    {
                        ailment.Value = attackAilment;
                        if (ailment.Value == ActionClip.Ailment.Death)
                        {
                            if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(attacker, this); }
                            SetKiller(attacker);
                        }
                    }
                }
                else // If this attack's ailment is none
                {
                    if (ailment.Value == ActionClip.Ailment.Stun | ailment.Value == ActionClip.Ailment.Stagger)
                    {
                        ailment.Value = ActionClip.Ailment.None;
                    }
                }

                // If we started a new ailment on this attack, we want to start a reset coroutine
                if (shouldApplyAilment)
                {
                    switch (ailment.Value)
                    {
                        case ActionClip.Ailment.Knockdown:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockdownDuration + ActionClip.HitStopEffectDuration, true, true));
                            break;
                        case ActionClip.Ailment.Knockup:
                            knockupHitCounter = 0;
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockupDuration + ActionClip.HitStopEffectDuration, false, true));
                            break;
                        case ActionClip.Ailment.Stun:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(stunDuration + ActionClip.HitStopEffectDuration, false, false));
                            break;
                        case ActionClip.Ailment.Stagger:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays(hitReaction));
                            break;
                        case ActionClip.Ailment.Pull:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays(hitReaction));
                            break;
                        case ActionClip.Ailment.Death:
                            break;
                        case ActionClip.Ailment.Incapacitated:
                            SetInviniciblity(1);
                            break;
                        default:
                            if (attackAilment != ActionClip.Ailment.Pull & attackAilment != ActionClip.Ailment.Grab) { Debug.LogWarning(attackAilment + " has not been implemented yet!"); }
                            break;
                    }

                    if (attackAilment == ActionClip.Ailment.Pull) { pullResetCoroutine = StartCoroutine(ResetPullAfterAnimationPlays(hitReaction)); }
                    if (attackAilment == ActionClip.Ailment.Grab) { grabResetCoroutine = StartCoroutine(ResetGrabAfterAnimationPlays(attack, hitReaction)); }
                }
            }

            if (ailment.Value == ActionClip.Ailment.Knockup)
            {
                knockupHitCounter++;
                if (knockupHitCounter >= knockupHitLimit)
                {
                    if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
                    SetInviniciblity(recoveryTimeInvincibilityBuffer + ActionClip.HitStopEffectDuration);
                    ailment.Value = ActionClip.Ailment.None;
                }
            }
        }

        public float lastRecoveryFixedTime { get; private set; } = Mathf.NegativeInfinity;
        public const float recoveryTimeInvincibilityBuffer = 1;
        private IEnumerator ResetAilmentAfterDuration(float duration, bool shouldMakeInvincible, bool shouldMakeInvincibleDuringRecovery)
        {
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
            if (shouldMakeInvincible) { SetInviniciblity(duration + recoveryTimeInvincibilityBuffer); }
            yield return new WaitForSeconds(duration);
            if (shouldMakeInvincibleDuringRecovery) { SetInviniciblity(recoveryTimeInvincibilityBuffer); }
            ailment.Value = ActionClip.Ailment.None;
        }

        private IEnumerator ResetAilmentAfterAnimationPlays(ActionClip hitReaction)
        {
            yield return new WaitForSeconds(AnimationHandler.GetTotalActionClipLengthInSeconds(hitReaction));
            ailment.Value = ActionClip.Ailment.None;
        }

        private Coroutine pullResetCoroutine;
        private IEnumerator ResetPullAfterAnimationPlays(ActionClip hitReaction)
        {
            if (pullResetCoroutine != null) { StopCoroutine(pullResetCoroutine); }
            yield return new WaitForSeconds(AnimationHandler.GetTotalActionClipLengthInSeconds(hitReaction));
            isPulled.Value = false;
        }

        protected NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        public ActionClip.Ailment GetAilment() { return ailment.Value; }
        public void ResetAilment()
        {
            if (!IsServer) { Debug.LogError("CombatAgent.ResetAilment() should only be called on the server!"); return; }

            StopGrabSequence();
            isPulled.Value = false;
            ailment.Value = ActionClip.Ailment.None;
        }

        public static bool IgnorePlayerCollisionsDuringAilment(ActionClip.Ailment ailmentToCheck)
        {
            return ailmentToCheck == ActionClip.Ailment.Knockdown;
        }

        private Coroutine colliderRadiusResetCoroutine;
        public bool ResetColliderRadiusPredicted { get; private set; }
        private IEnumerator ResetColliderRadius()
        {
            float timeElapsed = 0;
            while (true)
            {
                if (timeElapsed >= (NetworkManager.LocalTime.TimeAsFloat - NetworkManager.ServerTime.TimeAsFloat) + knockdownDuration + ActionClip.HitStopEffectDuration)
                {
                    break;
                }
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            ResetColliderRadiusPredicted = true;
            lastRecoveryFixedTime = Time.fixedTime;
        }

        protected Coroutine ailmentResetCoroutine;
        protected virtual void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            AnimationHandler.Animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);

            if (!IsServer)
            {
                if (IgnorePlayerCollisionsDuringAilment(current))
                {
                    colliderRadiusResetCoroutine = StartCoroutine(ResetColliderRadius());
                }
            }
            
            if (IgnorePlayerCollisionsDuringAilment(prev))
            {
                ResetColliderRadiusPredicted = false;

                if (colliderRadiusResetCoroutine != null)
                {
                    StopCoroutine(colliderRadiusResetCoroutine);
                    lastRecoveryFixedTime = Time.fixedTime;
                }

                if (IsServer)
                {
                    lastRecoveryFixedTime = Time.fixedTime;
                }
            }

            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }

            if (current == ActionClip.Ailment.Death)
            {
                StartCoroutine(ClearDamageMappingAfter1Frame());
                WeaponHandler.OnDeath();
                AnimationHandler.OnDeath();
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
                if (IsServer)
                {
                    isRaging.Value = false;
                    StopGrabSequence();
                }
                OnDeath();
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                AnimationHandler.OnRevive();
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
                OnAlive();
                wasIncapacitatedThisLife = false;
                incapacitatedReviveTimeTracker.Clear();
            }

            if (IsServer)
            {
                foreach (OnHitActionVFX onHitActionVFX in ailmentOnHitActionVFXList.Where(item => item.ailment == current))
                {
                    if (onHitActionVFX.actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        GameObject instance = WeaponHandler.SpawnActionVFX(WeaponHandler.CurrentActionClip, onHitActionVFX.actionVFX, null, transform);
                        PersistentLocalObjects.Singleton.StartCoroutine(DestroyVFXAfterAilmentIsDone(current, instance));
                    }
                }
            }
        }

        private IEnumerator DestroyVFXAfterAilmentIsDone(ActionClip.Ailment vfxAilment, GameObject vfxInstance)
        {
            yield return new WaitUntil(() => ailment.Value != vfxAilment | IsGrabbed | IsPulled);
            if (vfxInstance)
            {
                if (vfxInstance.TryGetComponent(out NetworkObject networkObject))
                {
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn(true);
                    }
                    else if (vfxInstance.TryGetComponent(out PooledObject pooledObject))
                    {
                        if (pooledObject.IsSpawned)
                        {
                            ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                        }
                    }
                    else
                    {
                        Destroy(vfxInstance);
                    }
                }
                else if (vfxInstance.TryGetComponent(out PooledObject pooledObject))
                {
                    if (pooledObject.IsSpawned)
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    }
                }
                else
                {
                    Destroy(vfxInstance);
                }
            }
        }

        [System.Serializable]
        private struct OnHitActionVFX
        {
            public ActionClip.Ailment ailment;
            public ActionVFX actionVFX;
        }

        [SerializeField] private List<OnHitActionVFX> ailmentOnHitActionVFXList = new List<OnHitActionVFX>();

#if UNITY_EDITOR
        public List<PooledObject> GetPooledObjectDependencies()
        {
            List<PooledObject> returnedList = new List<PooledObject>();
            foreach (OnHitActionVFX onHitActionVFX in ailmentOnHitActionVFXList)
            {
                returnedList.Add(onHitActionVFX.actionVFX.GetComponent<PooledObject>());
            }
            returnedList.Add(rageAtMaxVFXPrefab);
            returnedList.Add(ragingVFXPrefab);
            returnedList.Add(rageStartVFXPrefab);
            if (deathVFX)
            {
                returnedList.Add(deathVFX.GetComponent<PooledObject>());
            }
            returnedList.RemoveAll(item => item == null);
            return returnedList;
        }
#endif

        private void OnValidate()
        {
            List<ActionVFX> actionVFXToRemove = new List<ActionVFX>();
            foreach (OnHitActionVFX onHitActionVFX in ailmentOnHitActionVFXList)
            {
                if (!onHitActionVFX.actionVFX) { continue; }
                if (onHitActionVFX.actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnHit) { actionVFXToRemove.Add(onHitActionVFX.actionVFX); }
            }

            foreach (ActionVFX actionVFX in actionVFXToRemove)
            {
                ailmentOnHitActionVFXList.RemoveAll(item => item.actionVFX == actionVFX);
            }
        }

        private enum DeathBehavior
        {
            Ragdoll,
            Explode
        }

        public float LastAliveStartTime { get; private set; }
        protected virtual void OnAlive()
        {
            LastAliveStartTime = Time.time;
            switch (deathBehavior)
            {
                case DeathBehavior.Ragdoll:
                    AnimationHandler.SetRagdollActive(false);
                    break;
                case DeathBehavior.Explode:
                    AnimationHandler.RemoveExplosion();
                    break;
                default:
                    Debug.LogError("Unsure how to handle death behavior in OnAlive() " + deathBehavior + " " + this);
                    break;
            }
        }

        [SerializeField] private DeathBehavior deathBehavior = DeathBehavior.Ragdoll;
        [SerializeField] private float deathExplosionDelay;
        [SerializeField] private ActionVFX deathVFX;
        [SerializeField] private ActionClip deathVFXAttack;
        protected virtual void OnDeath()
        {
            if (IsServer)
            {
                if (deathVFX)
                {
                    GameObject vfxInstance;
                    (SpawnPoints.TransformData orientation, Transform parent) = WeaponHandler.GetActionVFXOrientation(deathVFXAttack, deathVFX, false, transform);
                    if (deathVFX.TryGetComponent(out PooledObject pooledObject))
                    {
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, orientation.position, orientation.rotation, parent).gameObject;
                    }
                    else
                    {
                        vfxInstance = Instantiate(pooledObject, orientation.position, orientation.rotation, parent).gameObject;
                        Debug.LogError("ActionVFX doesn't have a pooled object! " + deathVFX);
                    }

                    if (vfxInstance)
                    {
                        if (!IsServer) { Debug.LogError("You can only spawn action VFX on server!"); return; }

                        if (vfxInstance.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
                        {
                            gameInteractiveActionVFX.InitializeVFX(this, deathVFXAttack);
                        }

                        if (vfxInstance.TryGetComponent(out NetworkObject netObj))
                        {
                            if (netObj.IsSpawned)
                            {
                                Debug.LogError("Trying to spawn an action VFX instance that is already spawned " + vfxInstance);
                            }
                            else
                            {
                                netObj.Spawn(true);
                            }
                        }
                        else
                        {
                            Debug.LogError("VFX Instance doesn't have a network object component! " + vfxInstance);
                        }
                    }
                    else if (deathVFX.transformType != ActionVFX.TransformType.ConformToGround)
                    {
                        Debug.LogError("No vfx instance spawned for this prefab! " + deathVFX);
                    }
                }
            }

            switch (deathBehavior)
            {
                case DeathBehavior.Ragdoll:
                    AnimationHandler.SetRagdollActive(true);
                    AnimationHandler.HideRenderers();
                    break;
                case DeathBehavior.Explode:
                    AnimationHandler.Explode(deathExplosionDelay);
                    break;
                default:
                    Debug.LogError("Unsure how to handle death behavior in OnDeath() " + deathBehavior + " " + this);
                    break;
            }

            if (Master)
            {
                Master.slaves.Remove(this);
            }
        }

        protected bool CanProcessHit(bool isMeleeHit, CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon = null)
        {
            if (!attack.IsAttack()) { Debug.LogError("Trying to process a hit with an action clip that isn't an attack! " + attack); return false; }

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

            // If we hit someone and we are already grabbing but the person we're hitting isn't the attacker's grab victim
            if (attacker.IsGrabbing)
            {
                CombatAgent grabVictim = attacker.GetGrabVictim();
                if (grabVictim)
                {
                    if (grabVictim != this) { return false; }
                }
            }

            if (IsGrabbing) { return false; }

            // Don't let grab attack hit players that aren't grabbed
            if (attack.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                if (attacker.GetGrabVictim() != this) { return false; }
            }
            return true;
        }

        protected bool CanHit(bool isMeleeHit, CombatAgent attacker, ActionClip attack)
        {
            if (attack.maxHitLimit == 0) { return false; }

            if (IsInvincible) { return false; }

            if (isMeleeHit)
            {
                if (attacker.wasStaggeredThisFrame) { return false; }
            }

            return true;
        }

        protected enum HitResult
        {
            False,
            Block,
            True
        }

        protected bool CastHitResultToBoolean(HitResult hitResult)
        {
            switch (hitResult)
            {
                case HitResult.False:
                    return false;
                case HitResult.Block:
                    return true;
                case HitResult.True:
                    return true;
                default:
                    Debug.LogError("Unsure how to cast hit result to boolean " + hitResult);
                    return false;
            }
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessMeleeHit() should only be called on the server!"); return false; }
            return CastHitResultToBoolean(ProcessHit(true, attacker, attackingNetworkObject, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon));
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, NetworkObject attackingNetworkObject, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessProjectileHit() should only be called on the server!"); return false; }
            return CastHitResultToBoolean(ProcessHit(false, attacker, attackingNetworkObject, attack, impactPosition, hitSourcePosition, hitCounter, runtimeWeapon, damageMultiplier));
        }

        private bool wasIncapacitatedThisLife;

        [SerializeField] private bool canBeIncapacitated;
        [SerializeField] private bool disableHitReactions;
        protected HitResult ProcessHit(bool isMeleeHit, CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
        {
            if (!CanProcessHit(isMeleeHit, attacker, attack, runtimeWeapon)) { return HitResult.False; }

            if (!PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    if (status.associatedWithCurrentWeapon)
                    {
                        Debug.LogWarning("Adding a status to an ally on hit but it's associated with its current weapon " + attack + " " + attacker.WeaponHandler.GetWeapon() + " " + status.status);
                    }
                    StatusAgent.TryAddStatus(status);
                }
                return HitResult.True;
            }

            if (!CanHit(isMeleeHit, attacker, attack)) { return HitResult.False; }

            (bool applyAilmentRegardless, ActionClip.Ailment attackAilment) = GetAttackAilment(attack, hitCounter);

            if (IsUninterruptable)
            {
                attackAilment = ActionClip.Ailment.None;
            }
            else
            {
                wasStaggeredThisFrame = true;
                StartCoroutine(ResetStaggerBool());
            }

            if (attackAilment == ActionClip.Ailment.Grab) { hitSourcePosition = attacker.MovementHandler.GetPosition(); }

            AddStamina(-attack.staminaDamage);
            if (!attacker.IsRaging) { attacker.AddRage(attackerRageToBeAddedOnHit); }
            if (!IsRaging) { AddRage(victimRageToBeAddedOnHit); }

            float attackAngle = Vector3.SignedAngle(transform.forward, (hitSourcePosition - transform.position).normalized, Vector3.up);
            ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, WeaponHandler.IsBlocking, attackAilment, ailment.Value, applyAilmentRegardless);

            float HPDamage = -(attack.damage + SessionProgressionHandler.BaseDamageBonus);
            HPDamage *= attacker.StatusAgent.DamageMultiplier;
            HPDamage *= damageMultiplier;

            bool shouldPlayHitReaction;
            if (ShouldUseSpirit())
            {
                shouldPlayHitReaction = false;
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
                                hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, false, attackAilment, ailment.Value, false);
                            }
                            shouldPlayHitReaction = true;
                        }
                        break;
                    default:
                        Debug.LogError("Unsure how to process hit for hit reaction type " + hitReaction.GetHitReactionType());
                        break;
                }
            }
            else
            {
                shouldPlayHitReaction = attack.shouldPlayHitReaction;
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
            if (GetAilment() == ActionClip.Ailment.Incapacitated)
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
            else if (AddHPWithoutApply(HPDamage) <= 0)
            {
                if (GameModeManager.Singleton.IncapacitatedPlayerStateEnabled & canBeIncapacitated & !wasIncapacitatedThisLife)
                {
                    attackAilment = ActionClip.Ailment.Incapacitated;
                    hitReaction = WeaponHandler.GetWeapon().GetIncapacitatedReaction();

                    if (IsGrabbed)
                    {
                        if (GetGrabAssailant()) { GetGrabAssailant().CancelGrab(); }
                        CancelGrab();
                    }

                    AnimationHandler.PlayAction(hitReaction);
                    hitReactionWasPlayed = true;

                    wasIncapacitatedThisLife = true;
                }
                else
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
                    if (!IsGrabbed & !IsRaging & !disableHitReactions)
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

                EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attackingNetworkObject, attack, hitReaction);
            }

            if (runtimeWeapon)
            {
                foreach (ActionVFX actionVFX in attack.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        if (WeaponHandler.SpawnActionVFX(attack, actionVFX, attacker.transform, transform).TryGetComponent(out ActionVFXParticleSystem actionVFXParticleSystem))
                        {
                            if (!hitCounter.ContainsKey(this))
                            {
                                hitCounter.Add(this, new(1, Time.time));
                            }
                            else
                            {
                                hitCounter[this] = new(hitCounter[this].hitNumber + 1, Time.time);
                            }
                            actionVFXParticleSystem.AddToHitCounter(hitCounter);
                        }
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

            if (attacker is Attributes attributes) { attributes.AddHitToComboCounter(); }

            lastAttackingCombatAgent = attacker;
            return hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking ? HitResult.Block : HitResult.True;
        }

        private IEnumerator ClearDamageMappingAfter1Frame()
        {
            yield return null;
            ClearDamageMapping();
            lastAttackingCombatAgent = null;
        }

        [HideInInspector] public bool wasStaggeredThisFrame;
        protected IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        protected virtual (bool, ActionClip.Ailment) GetAttackAilment(ActionClip attack, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter)
        {
            if (StatusAgent.GetActiveStatuses().Contains(ActionClip.Status.immuneToAilments))
            {
                return (false, ActionClip.Ailment.None);
            }

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

            if (!Mathf.Approximately(damage, 0)) { RenderHitGlowOnly(); }
            AddHP(damage);
            return true;
        }

        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamageWithHitReaction() should only be called on the server!"); return false; }
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
            else
            {
                ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReactionByDirection(Weapon.HitLocation.Front);
                AnimationHandler.PlayAction(hitReaction);
            }

            RenderHit(attackingNetworkObject.NetworkObjectId, transform.position, GetArmorType(), Weapon.WeaponBone.Root, ActionClip.Ailment.None);
            AddHP(damage);
            return true;
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

        private float lastRenderHitFixedTime = -5;

        protected void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            lastRenderHitFixedTime = Time.fixedTime;

            GlowRenderer.RenderHit();
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetObjId, out NetworkObject attacker))
            {
                if (attacker.TryGetComponent(out CombatAgent attackingCombatAgent))
                {
                    PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
                    AudioManager.Singleton.PlayClipAtPoint(gameObject,
                        attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ailment),
                        impactPosition, Weapon.hitSoundEffectVolume);

                    RenderHitClientRpc(attackerNetObjId, impactPosition, armorType, weaponBone, ailment);
                }
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            lastRenderHitFixedTime = Time.fixedTime;

            // This check is for late joining clients
            if (!GlowRenderer) { return; }
            GlowRenderer.RenderHit();

            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetObjId, out NetworkObject attacker))
            {
                if (attacker.TryGetComponent(out CombatAgent attackingCombatAgent))
                {
                    PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
                    AudioManager.Singleton.PlayClipAtPoint(gameObject,
                        attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ailment),
                        impactPosition, Weapon.hitSoundEffectVolume);
                }
            }
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
