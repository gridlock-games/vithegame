using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Vi.Core.GameModeManagers;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(WeaponHandler))]
    public class Attributes : NetworkBehaviour
    {
        [SerializeField] private PooledObject worldSpaceLabelPrefab;

        private NetworkVariable<int> playerDataId = new NetworkVariable<int>();
        public int GetPlayerDataId() { return playerDataId.Value; }
        public void SetPlayerDataId(int id) { playerDataId.Value = id; name = PlayerDataManager.Singleton.GetPlayerData(id).character.name.ToString(); }
        public PlayerDataManager.Team GetTeam() { return CachedPlayerData.team; }

        public CharacterReference.RaceAndGender GetRaceAndGender() { return CachedPlayerData.character.raceAndGender; }

        private NetworkVariable<bool> spawnedOnOwnerInstance = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsSpawnedOnOwnerInstance() { return spawnedOnOwnerInstance.Value; }

        public Color GetRelativeTeamColor()
        {
            if (!PlayerDataManager.Singleton.ContainsId(GetPlayerDataId())) { return Color.black; }

            if (!IsClient) { return PlayerDataManager.GetTeamColor(GetTeam()); }
            else if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return Color.black; }
            else if (PlayerDataManager.Singleton.LocalPlayerData.team == PlayerDataManager.Team.Spectator) { return PlayerDataManager.GetTeamColor(GetTeam()); }
            else if (IsLocalPlayer) { return Color.white; }
            else if (PlayerDataManager.CanHit(PlayerDataManager.Singleton.LocalPlayerData.team, CachedPlayerData.team)) { return Color.red; }
            else { return Color.cyan; }
        }

        public PlayerDataManager.PlayerData CachedPlayerData { get; private set; }

        public void SetCachedPlayerData(PlayerDataManager.PlayerData playerData)
        {
            if (playerData.id != GetPlayerDataId()) { Debug.LogError("Player data doesn't have the same id!"); return; }
            CachedPlayerData = playerData;
        }

        public float GetMaxHP() { return weaponHandler.GetWeapon().GetMaxHP(); }
        public float GetMaxStamina() { return weaponHandler.GetWeapon().GetMaxStamina(); }
        public float GetMaxSpirit() { return weaponHandler.GetWeapon().GetMaxSpirit(); }
        public float GetMaxRage() { return weaponHandler.GetWeapon().GetMaxRage(); }

        private NetworkVariable<float> HP = new NetworkVariable<float>();
        private NetworkVariable<float> stamina = new NetworkVariable<float>();
        private NetworkVariable<float> spirit = new NetworkVariable<float>();
        private NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }
        public float GetStamina() { return stamina.Value; }
        public float GetSpirit() { return spirit.Value; }
        public float GetRage() { return rage.Value; }

        public void ResetStats(float hpPercentage, bool resetRage)
        {
            damageMappingThisLife.Clear();
            HP.Value = weaponHandler.GetWeapon().GetMaxHP() * hpPercentage;
            spirit.Value = weaponHandler.GetWeapon().GetMaxSpirit();
            stamina.Value = 0;
            if (resetRage)
                rage.Value = 0;
        }

        private void AddHP(float amount)
        {
            if (amount < 0) { amount *= damageReceivedMultiplier / damageReductionMultiplier; }
            if (amount > 0) { amount *= healingMultiplier; }

            if (amount > 0)
            {
                if (HP.Value < weaponHandler.GetWeapon().GetMaxHP())
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, weaponHandler.GetWeapon().GetMaxHP());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (HP.Value > weaponHandler.GetWeapon().GetMaxHP())
                {
                    HP.Value += amount;
                }
                else
                {
                    HP.Value = Mathf.Clamp(HP.Value + amount, 0, weaponHandler.GetWeapon().GetMaxHP());
                }
            }
        }

        private float AddHPWithoutApply(float amount)
        {
            if (amount < 0) { amount *= damageReceivedMultiplier / damageReductionMultiplier; }
            if (amount > 0) { amount *= healingMultiplier; }

            if (amount > 0)
            {
                if (HP.Value < weaponHandler.GetWeapon().GetMaxHP())
                {
                    return Mathf.Clamp(HP.Value + amount, 0, weaponHandler.GetWeapon().GetMaxHP());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (HP.Value > weaponHandler.GetWeapon().GetMaxHP())
                {
                    return HP.Value + amount;
                }
                else
                {
                    return Mathf.Clamp(HP.Value + amount, 0, weaponHandler.GetWeapon().GetMaxHP());
                }
            }
            return HP.Value;
        }

        public void AddStamina(float amount, bool activateCooldown = true)
        {
            if (activateCooldown)
                staminaDelayCooldown = weaponHandler.GetWeapon().GetStaminaDelay();

            if (amount > 0)
            {
                if (stamina.Value < weaponHandler.GetWeapon().GetMaxStamina())
                {
                    stamina.Value = Mathf.Clamp(stamina.Value + amount, 0, weaponHandler.GetWeapon().GetMaxStamina());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (stamina.Value > weaponHandler.GetWeapon().GetMaxStamina())
                {
                    stamina.Value += amount;
                }
                else
                {
                    stamina.Value = Mathf.Clamp(stamina.Value + amount, 0, weaponHandler.GetWeapon().GetMaxStamina());
                }
            }
        }

        public void AddSpirit(float amount)
        {
            if (amount < 0) { amount *= spiritReductionMultiplier; }
            if (amount > 0) { amount *= spiritIncreaseMultiplier; }

            if (amount > 0)
            {
                if (spirit.Value < weaponHandler.GetWeapon().GetMaxSpirit())
                {
                    spirit.Value = Mathf.Clamp(spirit.Value + amount, 0, weaponHandler.GetWeapon().GetMaxSpirit());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (spirit.Value > weaponHandler.GetWeapon().GetMaxSpirit())
                {
                    spirit.Value += amount;
                }
                else
                {
                    spirit.Value = Mathf.Clamp(spirit.Value + amount, 0, weaponHandler.GetWeapon().GetMaxSpirit());
                }
            }
        }

        public void AddRage(float amount)
        {
            if (amount > 0)
            {
                if (rage.Value < weaponHandler.GetWeapon().GetMaxRage())
                {
                    rage.Value = Mathf.Clamp(rage.Value + amount, 0, weaponHandler.GetWeapon().GetMaxRage());
                }
            }
            else // Delta is less than or equal to zero
            {
                if (rage.Value > weaponHandler.GetWeapon().GetMaxRage())
                {
                    rage.Value += amount;
                }
                else
                {
                    rage.Value = Mathf.Clamp(rage.Value + amount, 0, weaponHandler.GetWeapon().GetMaxRage());
                }
            }
        }

        PooledObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));

            if (IsServer)
            {
                StartCoroutine(InitStats());
                StartCoroutine(SetNetworkVisibilityAfterSpawn());
            }

            HP.OnValueChanged += OnHPChanged;
            spirit.OnValueChanged += OnSpiritChanged;
            rage.OnValueChanged += OnRageChanged;
            isRaging.OnValueChanged += OnIsRagingChanged;
            ailment.OnValueChanged += OnAilmentChanged;
            statuses.OnListChanged += OnStatusChange;
            activeStatuses.OnListChanged += OnActiveStatusChange;
            comboCounter.OnValueChanged += OnComboCounterChange;

            if (!IsLocalPlayer) { worldSpaceLabelInstance = ObjectPoolingManager.SpawnObject(worldSpaceLabelPrefab, transform); }
            StartCoroutine(AddPlayerObjectToPlayerDataManager());

            if (IsOwner)
            {
                spawnedOnOwnerInstance.Value = true;
                RefreshStatus();
            }
        }

        public void UpdateNetworkVisiblity()
        {
            if (!IsServer) { Debug.LogError("Attributes.UpdateNetworkVisibility() should only be called on the server!"); return; }
            StartCoroutine(SetNetworkVisibilityAfterSpawn());
        }

        private IEnumerator SetNetworkVisibilityAfterSpawn()
        {
            if (!IsServer) { Debug.LogError("Attributes.SetNetworkVisibilityAfterSpawn() should only be called on the server!"); yield break; }
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
            PlayerDataManager.Singleton.UpdateIgnoreCollisionsMatrix();
        }

        private IEnumerator InitStats()
        {
            yield return new WaitUntil(() => weaponHandler.GetWeapon() != null);
            HP.Value = weaponHandler.GetWeapon().GetMaxHP();
            spirit.Value = weaponHandler.GetWeapon().GetMaxSpirit();
        }

        private IEnumerator AddPlayerObjectToPlayerDataManager()
        {
            if (!(IsHost & IsLocalPlayer)) { yield return new WaitUntil(() => GetPlayerDataId() != (int)NetworkManager.ServerClientId); }
            PlayerDataManager.Singleton.AddPlayerObject(GetPlayerDataId(), this);
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHPChanged;
            spirit.OnValueChanged -= OnSpiritChanged;
            rage.OnValueChanged -= OnRageChanged;
            isRaging.OnValueChanged -= OnIsRagingChanged;
            ailment.OnValueChanged -= OnAilmentChanged;
            statuses.OnListChanged -= OnStatusChange;
            activeStatuses.OnListChanged -= OnActiveStatusChange;
            comboCounter.OnValueChanged -= OnComboCounterChange;

            if (worldSpaceLabelInstance) { ObjectPoolingManager.ReturnObjectToPool(worldSpaceLabelInstance); }
            PlayerDataManager.Singleton.RemovePlayerObject(GetPlayerDataId());
        }

        [SerializeField] private AudioClip heartbeatSoundEffect;
        private const float heartbeatVolume = 1;
        private const float heartbeatHPPercentageThreshold = 0.1f;

        private void OnHPChanged(float prev, float current)
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
                if (!rageAtMaxVFXInstance) { rageAtMaxVFXInstance = ObjectPoolingManager.SpawnObject(rageAtMaxVFXPrefab, animationHandler.Animator.GetBoneTransform(HumanBodyBones.Hips)); }
            }
            else
            {
                if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(rageAtMaxVFXInstance); }
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
                if (rageAtMaxVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(rageAtMaxVFXInstance); }
                if (!ragingVFXInstance) { ragingVFXInstance = ObjectPoolingManager.SpawnObject(ragingVFXPrefab, animationHandler.Animator.GetBoneTransform(HumanBodyBones.Hips)); }
            }
            else
            {
                if (ragingVFXInstance) { ObjectPoolingManager.ReturnObjectToPool(ragingVFXInstance); }
            }
        }

        public GlowRenderer GlowRenderer { get; private set; }
        private void OnTransformChildrenChanged()
        {
            GlowRenderer = GetComponentInChildren<GlowRenderer>();
        }

        public NetworkCollider NetworkCollider { get; private set; }

        public void SetNetworkCollider(NetworkCollider networkCollider) { NetworkCollider = networkCollider; }

        private WeaponHandler weaponHandler;
        private AnimationHandler animationHandler;
        private MovementHandler movementHandler;
        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private void Awake()
        {
            statuses = new NetworkList<ActionClip.StatusPayload>();
            activeStatuses = new NetworkList<int>();
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            movementHandler = GetComponent<MovementHandler>();
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            RefreshStatus();

            SetCachedPlayerData(PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()));
        }

        [SerializeField] private PooledObject teamIndicatorPrefab;
        private PooledObject teamIndicatorInstance;
        private void Start()
        {
            teamIndicatorInstance = ObjectPoolingManager.SpawnObject(teamIndicatorPrefab, transform);
        }

        private void OnEnable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
        }

        private void OnDisable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
        }

        public bool IsInvincible() { return isInvincible.Value; }
        private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>();
        private float invincibilityEndTime;
        public void SetInviniciblity(float duration) { invincibilityEndTime = Time.time + duration; }

        public bool IsUninterruptable() { return isUninterruptable.Value; }
        private NetworkVariable<bool> isUninterruptable = new NetworkVariable<bool>();
        private float uninterruptableEndTime;
        public void SetUninterruptable(float duration) { uninterruptableEndTime = Time.time + duration; }

        private bool wasStaggeredThisFrame;
        public bool ProcessMeleeHit(Attributes attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessMeleeHit() should only be called on the server!"); return false; }

            return ProcessHit(true, attacker, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon);
        }

        private IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        public bool ProcessProjectileHit(Attributes attacker, RuntimeWeapon runtimeWeapon, Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessProjectileHit() should only be called on the server!"); return false; }

            return ProcessHit(false, attacker, attack, impactPosition, hitSourcePosition, hitCounter, runtimeWeapon, damageMultiplier);
        }

        private Attributes lastAttackingAttributes;
        public bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamage() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                ailment.Value = ActionClip.Ailment.Death;
                animationHandler.PlayAction(weaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingAttributes)
                {
                    SetKiller(lastAttackingAttributes);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(lastAttackingAttributes, this); }
                }
                else
                {
                    killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            RenderHitGlowOnly();
            AddHP(damage);
            return true;
        }

        public bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamageWithHitReaction() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            ActionClip.Ailment attackAilment = ActionClip.Ailment.None;
            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                attackAilment = ActionClip.Ailment.Death;
                ailment.Value = ActionClip.Ailment.Death;
                animationHandler.PlayAction(weaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingAttributes)
                {
                    SetKiller(lastAttackingAttributes);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(lastAttackingAttributes, this); }
                }
                else
                {
                    killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            else
            {
                ActionClip hitReaction = weaponHandler.GetWeapon().GetHitReactionByDirection(Weapon.HitLocation.Front);
                animationHandler.PlayAction(hitReaction);
            }

            RenderHit(attackingNetworkObject.NetworkObjectId, transform.position, animationHandler.GetArmorType(), Weapon.WeaponBone.Root, attackAilment);
            AddHP(damage);
            return true;
        }

        private NetworkVariable<ulong> killerNetObjId = new NetworkVariable<ulong>();

        private void SetKiller(Attributes killer) { killerNetObjId.Value = killer.NetworkObjectId; }

        public NetworkObject GetKiller()
        {
            if (ailment.Value != ActionClip.Ailment.Death) { Debug.LogError("Trying to get killer while not dead!"); return null; }

            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(killerNetObjId.Value))
                return NetworkManager.SpawnManager.SpawnedObjects[killerNetObjId.Value];
            else
                return null;
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

        private float hitFreezeStartTime = Mathf.NegativeInfinity;
        private bool shouldShake;

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

        private NetworkVariable<int> grabAssailantDataId = new NetworkVariable<int>();
        private NetworkVariable<int> grabVictimDataId = new NetworkVariable<int>();
        private NetworkVariable<FixedString64Bytes> grabAttackClipName = new NetworkVariable<FixedString64Bytes>();
        private NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>();

        public bool IsGrabbed() { return isGrabbed.Value; }

        public Attributes GetGrabAssailant()
        {
            if (PlayerDataManager.Singleton.ContainsId(grabAssailantDataId.Value))
            {
                return PlayerDataManager.Singleton.GetPlayerObjectById(grabAssailantDataId.Value);
            }
            else
            {
                return null;
            }
        }

        public Attributes GetGrabVictim()
        {
            if (PlayerDataManager.Singleton.ContainsId(grabVictimDataId.Value))
            {
                return PlayerDataManager.Singleton.GetPlayerObjectById(grabVictimDataId.Value);
            }
            else
            {
                return null;
            }
        }

        public AnimationClip GetGrabReactionClip()
        {
            Attributes grabAssailant = GetGrabAssailant();
            if (!grabAssailant) { Debug.LogError("No Grab Assailant Found!"); return null; }

            ActionClip grabAttackClip = grabAssailant.weaponHandler.GetWeapon().GetActionClipByName(grabAttackClipName.Value.ToString());

            if (!grabAttackClip.grabVictimClip) { Debug.LogError("Couldn't find grab reaction clip!"); }
            return grabAttackClip.grabVictimClip;
        }

        public void CancelGrab()
        {
            if (IsGrabbed())
            {
                if (grabResetCoroutine != null) { StopCoroutine(grabResetCoroutine); }
                isGrabbed.Value = false;
                grabAssailantDataId.Value = default;
                grabVictimDataId.Value = default;
            }

            if (animationHandler.IsGrabAttacking())
            {
                animationHandler.CancelAllActions(0.15f);
            }
        }

        public const float minStaminaPercentageToBeAbleToBlock = 0.3f;
        private const float notBlockingSpiritHitReactionPercentage = 0.4f;
        private const float blockingSpiritHitReactionPercentage = 0.5f;

        private const float rageDamageMultiplier = 1.15f;

        private Dictionary<Attributes, float> damageMappingThisLife = new Dictionary<Attributes, float>();

        public Dictionary<Attributes, float> GetDamageMappingThisLife() { return damageMappingThisLife; }

        private void AddDamageToMapping(Attributes attacker, float damage)
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

        private bool ProcessHit(bool isMeleeHit, Attributes attacker, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
        {
            if (isMeleeHit)
            {
                if (!runtimeWeapon) { Debug.LogError("When processing a melee hit, you need to pass in a runtime weapon!"); return false; }
            }

            if (GetAilment() == ActionClip.Ailment.Death | attacker.GetAilment() == ActionClip.Ailment.Death) { return false; }

            // Make grab people invinicible to all attacks except for the grab hits
            if (IsGrabbed() & attacker != GetGrabAssailant()) { return false; }
            if (animationHandler.IsGrabAttacking()) { return false; }

            // Don't let grab attack hit players that aren't grabbed
            if (!IsGrabbed() & attacker.animationHandler.IsGrabAttacking()) { return false; }

            if (PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                if (Mathf.Approximately(attack.damage, 0)) { return false; }
            }
            else
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    TryAddStatus(status.status, status.value, status.duration, status.delay, false);
                }
                return false;
            }

            if (attack.maxHitLimit == 0) { return false; }

            if (IsInvincible()) { return false; }
            if (isMeleeHit)
            {
                if (attacker.wasStaggeredThisFrame) { Debug.Log(attacker + " was staggered"); return false; }

                if (!IsUninterruptable())
                {
                    wasStaggeredThisFrame = true;
                    StartCoroutine(ResetStaggerBool());
                }
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

            if (IsUninterruptable()) { attackAilment = ActionClip.Ailment.None; }

            AddStamina(-attack.staminaDamage);
            if (!attacker.IsRaging()) { attacker.AddRage(attackerRageToBeAddedOnHit); }
            if (!IsRaging()) { AddRage(victimRageToBeAddedOnHit); }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, weaponHandler.IsBlocking, attackAilment, ailment.Value);
            hitReaction.SetHitReactionRootMotionMultipliers(attack);

            float HPDamage = -attack.damage;
            HPDamage *= attacker.damageMultiplier;
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
                            hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, false, attackAilment, ailment.Value);
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
                HPDamage *= attacker.animationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.animationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (IsRaging()) { HPDamage *= rageDamageMultiplier; }

            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, false, attackAilment, ailment.Value);
            }

            bool hitReactionWasPlayed = false;
            if (!IsUninterruptable() | hitReaction.ailment == ActionClip.Ailment.Death)
            {
                if (hitReaction.ailment == ActionClip.Ailment.Death & IsGrabbed())
                {
                    GetGrabAssailant().CancelGrab();
                    CancelGrab();
                }

                if (hitReaction.ailment == ActionClip.Ailment.Grab)
                {
                    grabAttackClipName.Value = attack.name;
                    grabAssailantDataId.Value = attacker.GetPlayerDataId();
                    attacker.grabVictimDataId.Value = GetPlayerDataId();
                    isGrabbed.Value = true;
                    Vector3 victimNewPosition = attacker.movementHandler.GetPosition() + (attacker.transform.forward * 1.2f);
                    movementHandler.SetOrientation(victimNewPosition, Quaternion.LookRotation(attacker.movementHandler.GetPosition() - victimNewPosition, Vector3.up));
                    attacker.animationHandler.PlayAction(attacker.weaponHandler.GetWeapon().GetGrabAttackClip(attack));
                }

                if (!(IsGrabbed() & hitReaction.ailment == ActionClip.Ailment.None))
                {
                    if (attack.shouldPlayHitReaction
                        | ailment.Value != ActionClip.Ailment.None
                        | animationHandler.IsCharging()
                        | shouldPlayHitReaction)
                    {
                        if (!(IsRaging() & hitReaction.ailment == ActionClip.Ailment.None))
                        {
                            animationHandler.PlayAction(hitReaction);
                            hitReactionWasPlayed = true;
                        }
                    }
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
                    RenderHit(attacker.NetworkObjectId, impactPosition, animationHandler.GetArmorType(), runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root, attackAilment);
                    float prevHP = GetHP();
                    AddHP(HPDamage);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                    AddDamageToMapping(attacker, prevHP - GetHP());
                }

                EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attack, hitReaction);
            }

            attacker.comboCounter.Value += 1;

            if (IsServer)
            {
                foreach (ActionVFX actionVFX in attack.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        weaponHandler.SpawnActionVFX(weaponHandler.CurrentActionClip, actionVFX, attacker.transform, transform);
                    }
                }
            }

            if (!IsUninterruptable())
            {
                if (attack.shouldFlinch | IsRaging())
                {
                    movementHandler.Flinch(attack.GetFlinchAmount());
                    if (!hitReactionWasPlayed & !IsGrabbed()) { animationHandler.PlayAction(weaponHandler.GetWeapon().GetFlinchClip(attackAngle)); }
                }
            }

            lastAttackingAttributes = attacker;
            return true;
        }

        private void StartHitStop(Attributes attacker, bool isMeleeHit)
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
            Attributes attacker = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<Attributes>();

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

        private NetworkVariable<int> pullAssailantDataId = new NetworkVariable<int>();
        private NetworkVariable<bool> isPulled = new NetworkVariable<bool>();

        public bool IsPulled() { return isPulled.Value; }

        public Attributes GetPullAssailant() { return PlayerDataManager.Singleton.GetPlayerObjectById(pullAssailantDataId.Value); }

        private void EvaluateAilment(ActionClip.Ailment attackAilment, bool applyAilmentRegardless, Vector3 hitSourcePosition, Attributes attacker, ActionClip attack, ActionClip hitReaction)
        {
            foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTargetOnHit)
            {
                TryAddStatus(status.status, status.value, status.duration, status.delay, false);
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
                        pullAssailantDataId.Value = attacker.GetPlayerDataId();
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
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockdownDuration + ActionClip.HitStopEffectDuration, true));
                            break;
                        case ActionClip.Ailment.Knockup:
                            knockupHitCounter = 0;
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockupDuration + ActionClip.HitStopEffectDuration, false));
                            break;
                        case ActionClip.Ailment.Stun:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(stunDuration + ActionClip.HitStopEffectDuration, false));
                            break;
                        case ActionClip.Ailment.Stagger:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays(hitReaction));
                            break;
                        case ActionClip.Ailment.Pull:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays(hitReaction));
                            break;
                        case ActionClip.Ailment.Death:
                            break;
                        default:
                            if (attackAilment != ActionClip.Ailment.Pull & attackAilment != ActionClip.Ailment.Grab) { Debug.LogWarning(attackAilment + " has not been implemented yet!"); }
                            break;
                    }

                    if (attackAilment == ActionClip.Ailment.Pull) { pullResetCoroutine = StartCoroutine(ResetPullAfterAnimationPlays(hitReaction)); }
                    if (attackAilment == ActionClip.Ailment.Grab) { grabResetCoroutine = StartCoroutine(ResetGrabAfterAnimationPlays(hitReaction)); }
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

        private IEnumerator DestroyVFXAfterAilmentIsDone(ActionClip.Ailment vfxAilment, GameObject vfxInstance)
        {
            yield return new WaitUntil(() => ailment.Value != vfxAilment | IsGrabbed() | IsPulled());
            if (vfxInstance) { Destroy(vfxInstance); }
        }

        [System.Serializable]
        private struct OnHitActionVFX
        {
            public ActionClip.Ailment ailment;
            public ActionVFX actionVFX;
        }

        [SerializeField] private List<OnHitActionVFX> ailmentOnHitActionVFXList = new List<OnHitActionVFX>();

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

        private int knockupHitCounter;
        private const int knockupHitLimit = 5;

        private const float stunDuration = 3;
        private const float knockdownDuration = 2;
        private const float knockupDuration = 4;
        private const float attackerRageToBeAddedOnHit = 2;
        private const float victimRageToBeAddedOnHit = 1;

        private void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            GlowRenderer.RenderHit();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
            Weapon weapon = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<WeaponHandler>().GetWeapon();
            AudioManager.Singleton.PlayClipAtPoint(gameObject, weapon.GetInflictHitSoundEffect(armorType, weaponBone, ailment), impactPosition, Weapon.hitSoundEffectVolume);

            RenderHitClientRpc(attackerNetObjId, impactPosition, armorType, weaponBone, ailment);
        }

        [Rpc(SendTo.NotServer)]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            GlowRenderer.RenderHit();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
            Weapon weapon = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<WeaponHandler>().GetWeapon();
            AudioManager.Singleton.PlayClipAtPoint(gameObject, weapon.GetInflictHitSoundEffect(armorType, weaponBone, ailment), impactPosition, Weapon.hitSoundEffectVolume);
        }

        private void RenderHitGlowOnly()
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

        private void RenderBlock(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            GlowRenderer.RenderBlock();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, weaponHandler.GetWeapon().GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);

            RenderBlockClientRpc(impactPosition, attackingWeaponMaterial);
        }

        [Rpc(SendTo.NotServer)]
        private void RenderBlockClientRpc(Vector3 impactPosition, Weapon.WeaponMaterial attackingWeaponMaterial)
        {
            GlowRenderer.RenderBlock();
            PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(gameObject, weaponHandler.GetWeapon().GetBlockingHitSoundEffect(attackingWeaponMaterial), impactPosition, Weapon.hitSoundEffectVolume);
        }

        public ulong GetRoundTripTime() { return roundTripTime.Value; }

        private NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        private void RefreshStatus()
        {
            if (IsOwner)
            {
                pingEnabled.Value = bool.Parse(FasterPlayerPrefs.Singleton.GetString("PingEnabled"));
            }
        }

        private NetworkVariable<bool> pingEnabled = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!IsSpawned) { return; }

            bool isInvincibleThisFrame = IsInvincible();
            if (!isInvincibleThisFrame)
            {
                if (!IsLocalPlayer)
                {
                    if (animationHandler.IsGrabAttacking())
                    {
                        isInvincibleThisFrame = true;
                    }
                    else if (IsGrabbed())
                    {
                        Attributes grabAssailant = GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (!grabAssailant.IsLocalPlayer) { isInvincibleThisFrame = true; }
                        }
                    }
                }
            }

            GlowRenderer.RenderInvincible(isInvincibleThisFrame);
            GlowRenderer.RenderUninterruptable(IsUninterruptable());

            if (!IsServer) { return; }

            if (Time.time - lastComboCounterChangeTime >= comboCounterResetTime) { comboCounter.Value = 0; }

            bool evaluateInvinicibility = true;
            bool evaluateUninterruptability = true;
            if (animationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
            {
                if (weaponHandler.CurrentActionClip.isUninterruptable) { isUninterruptable.Value = true; evaluateUninterruptability = false; }
                if (weaponHandler.CurrentActionClip.isInvincible) { isInvincible.Value = true; evaluateInvinicibility = false; }
            }

            if (evaluateInvinicibility) { isInvincible.Value = Time.time <= invincibilityEndTime; }
            if (evaluateUninterruptability) { isUninterruptable.Value = Time.time <= uninterruptableEndTime; }

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
                if (Time.time - spiritRegenActivateTime <= 50 & !weaponHandler.IsBlocking) { UpdateSpirit(); }
            }
            
            if (pingEnabled.Value) { roundTripTime.Value = networkTransport.GetCurrentRtt(OwnerClientId); }
        }

        private float staminaDelayCooldown;
        private void UpdateStamina()
        {
            staminaDelayCooldown = Mathf.Max(0, staminaDelayCooldown - Time.deltaTime);
            if (staminaDelayCooldown > 0) { return; }
            AddStamina(weaponHandler.GetWeapon().GetStaminaRecoveryRate() * Time.deltaTime, false);
        }

        private float spiritRegenActivateTime = Mathf.NegativeInfinity;
        private const float spiritRegenRate = 3;
        private void UpdateSpirit()
        {
            AddSpirit(spiritRegenRate * Time.deltaTime);
        }

        public const float ragingStaminaCostMultiplier = 1.25f;
        private const float rageDepletionRate = 1;
        private float rageDelayCooldown;
        private void UpdateRage()
        {
            if (IsRaging())
            {
                AddRage(-rageDepletionRate * Time.deltaTime);
            }

            rageDelayCooldown = Mathf.Max(0, rageDelayCooldown - Time.deltaTime);
            if (rageDelayCooldown > 0) { return; }
            AddRage(weaponHandler.GetWeapon().GetRageRecoveryRate() * Time.deltaTime);
        }

        public void OnActivateRage()
        {
            if (!CanActivateRage()) { return; }
            ActivateRage();
        }

        public bool IsRaging() { return isRaging.Value; }
        private NetworkVariable<bool> isRaging = new NetworkVariable<bool>();
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

        [Rpc(SendTo.Server)]
        private void ActivateRageServerRpc()
        {
            ActivateRage();
        }

        private NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        private NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>(Quaternion.Euler(0, 0, 0)); // Don't remove the Quaternion.Euler() call, for some reason it's necessary BLACK MAGIC

        private void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            animationHandler.Animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }

            if (IsServer)
            {
                foreach (OnHitActionVFX onHitActionVFX in ailmentOnHitActionVFXList.FindAll(item => item.ailment == ailment.Value))
                {
                    if (onHitActionVFX.actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        GameObject instance = weaponHandler.SpawnActionVFX(weaponHandler.CurrentActionClip, onHitActionVFX.actionVFX, null, transform);
                        StartCoroutine(DestroyVFXAfterAilmentIsDone(ailment.Value, instance));
                    }
                }
            }

            if (current == ActionClip.Ailment.Death)
            {
                StartCoroutine(ClearDamageMappingAfter1Frame());
                spiritRegenActivateTime = Mathf.NegativeInfinity;
                weaponHandler.OnDeath();
                animationHandler.OnDeath();
                animationHandler.Animator.enabled = false;
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(false); }
                respawnCoroutine = StartCoroutine(RespawnSelf());
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                isRaging.Value = false;
                animationHandler.Animator.enabled = true;
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.gameObject.SetActive(true); }
                if (respawnCoroutine != null) { StopCoroutine(respawnCoroutine); }
            }
        }

        private IEnumerator ClearDamageMappingAfter1Frame()
        {
            yield return null;
            damageMappingThisLife.Clear();
            lastAttackingAttributes = null;
        }

        public float GetRespawnTime() { return Mathf.Clamp(GameModeManager.Singleton.GetRespawnTime() - (Time.time - respawnSelfCalledTime), 0, GameModeManager.Singleton.GetRespawnTime()); }
        public float GetRespawnTimeAsPercentage() { return 1 - (GetRespawnTime() / GameModeManager.Singleton.GetRespawnTime()); }

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
            if (IsServer)
            {
                yield return PlayerDataManager.Singleton.RespawnPlayer(this);
            }
            yield return new WaitUntil(() => ailment.Value != ActionClip.Ailment.Death);
            IsRespawning = false;
        }

        public void ResetAilment() { ailment.Value = ActionClip.Ailment.None; }
        public ActionClip.Ailment GetAilment() { return ailment.Value; }
        public bool ShouldApplyAilmentRotation() { return (ailment.Value != ActionClip.Ailment.None & ailment.Value != ActionClip.Ailment.Pull) | IsGrabbed(); }
        public Quaternion GetAilmentRotation() { return ailmentRotation.Value; }

        private const float recoveryTimeInvincibilityBuffer = 1;
        private Coroutine ailmentResetCoroutine;
        private IEnumerator ResetAilmentAfterDuration(float duration, bool shouldMakeInvincible)
        {
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
            if (shouldMakeInvincible) { SetInviniciblity(duration + recoveryTimeInvincibilityBuffer); }
            yield return new WaitForSeconds(duration);
            SetInviniciblity(recoveryTimeInvincibilityBuffer);
            ailment.Value = ActionClip.Ailment.None;
        }

        private IEnumerator ResetAilmentAfterAnimationPlays(ActionClip hitReaction)
        {
            yield return new WaitForSeconds(animationHandler.GetTotalActionClipLengthInSeconds(hitReaction));
            ailment.Value = ActionClip.Ailment.None;
        }

        private Coroutine pullResetCoroutine;
        private IEnumerator ResetPullAfterAnimationPlays(ActionClip hitReaction)
        {
            if (pullResetCoroutine != null) { StopCoroutine(pullResetCoroutine); }
            yield return new WaitForSeconds(animationHandler.GetTotalActionClipLengthInSeconds(hitReaction));
            isPulled.Value = false;
        }

        private Coroutine grabResetCoroutine;
        private IEnumerator ResetGrabAfterAnimationPlays(ActionClip hitReaction)
        {
            if (hitReaction.ailment != ActionClip.Ailment.Grab) { Debug.LogError("Attributes.ResetGrabAfterAnimationPlays() should only be called with a grab hit reaction clip!"); yield break; }
            if (grabResetCoroutine != null) { StopCoroutine(grabResetCoroutine); }

            float durationLeft = animationHandler.GetTotalActionClipLengthInSeconds(hitReaction);
            Attributes attacker = GetGrabAssailant();
            while (true)
            {
                durationLeft -= Time.deltaTime;
                if (attacker)
                {
                    Vector3 victimNewPosition = attacker.movementHandler.GetPosition() + (attacker.transform.forward * 1.2f);
                    if (Vector3.Distance(victimNewPosition, movementHandler.GetPosition()) > 1)
                    {
                        movementHandler.SetOrientation(victimNewPosition, Quaternion.LookRotation(attacker.movementHandler.GetPosition() - victimNewPosition, Vector3.up));
                    }
                }
                else
                {
                    attacker = GetGrabAssailant();
                }
                yield return null;
                if (durationLeft <= 0) { break; }
            }
            isGrabbed.Value = false;
            grabAssailantDataId.Value = default;
            grabVictimDataId.Value = default;
        }

        public List<ActionClip.Status> GetActiveStatuses()
        {
            List<ActionClip.Status> statusList = new List<ActionClip.Status>();
            for (int i = 0; i < activeStatuses.Count; i++)
            {
                statusList.Add((ActionClip.Status)activeStatuses[i]);
            }
            return statusList;
        }

        private NetworkList<ActionClip.StatusPayload> statuses;

        private NetworkList<int> activeStatuses;

        public bool TryAddStatus(ActionClip.Status status, float value, float duration, float delay, bool associatedWithCurrentWeapon)
        {
            if (!IsServer) { Debug.LogError("Attributes.TryAddStatus() should only be called on the server"); return false; }
            statuses.Add(new ActionClip.StatusPayload(status, value, duration, delay, associatedWithCurrentWeapon));
            return true;
        }

        private bool stopAllStatuses;
        public void RemoveAllStatuses()
        {
            if (!IsServer) { Debug.LogError("Attributes.RemoveAllStatuses() should only be called on the server"); return; }

            if (stopAllStatusesCoroutine != null) { StopCoroutine(stopAllStatusesCoroutine); }
            stopAllStatuses = true;
            stopAllStatusesCoroutine = StartCoroutine(ResetStopAllStatusesBool());
        }

        private Coroutine stopAllStatusesCoroutine;
        private IEnumerator ResetStopAllStatusesBool()
        {
            yield return null;
            yield return null;
            stopAllStatuses = false;
        }

        private bool stopAllStatusesAssociatedWithWeapon;
        public void RemoveAllStatusesAssociatedWithWeapon()
        {
            if (!IsServer) { Debug.LogError("Attributes.RemoveAllStatusesAssociatedWithWeapon() should only be called on the server"); return; }

            if (stopAllStatusesAssociatedWithWeaponCoroutine != null) { StopCoroutine(stopAllStatusesAssociatedWithWeaponCoroutine); }
            stopAllStatusesAssociatedWithWeapon = true;
            stopAllStatusesAssociatedWithWeaponCoroutine = StartCoroutine(ResetStopAllStatusesAssociatedWithWeaponBool());
        }

        private Coroutine stopAllStatusesAssociatedWithWeaponCoroutine;
        private IEnumerator ResetStopAllStatusesAssociatedWithWeaponBool()
        {
            yield return null;
            yield return null;
            stopAllStatusesAssociatedWithWeapon = false;
        }

        private bool TryRemoveStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsServer) { Debug.LogError("Attributes.TryRemoveStatus() should only be called on the server"); return false; }

            if (!statuses.Contains(statusPayload) & !activeStatuses.Contains((int)statusPayload.status))
            {
                Debug.LogError("Trying to remove status but it isn't in both status lists! " + statusPayload.status);
                return false;
            }
            else
            {
                int indexToRemoveAt = -1;
                for (int i = 0; i < statuses.Count; i++)
                {
                    if (statuses[i].status == statusPayload.status
                        & statuses[i].value == statusPayload.value
                        & statuses[i].duration == statusPayload.duration
                        & statuses[i].delay == statusPayload.delay)
                    { indexToRemoveAt = i; break; }
                }

                if (indexToRemoveAt > -1)
                {
                    statuses.RemoveAt(indexToRemoveAt);
                    activeStatuses.Remove((int)statusPayload.status);
                }
                else
                {
                    Debug.LogError("Trying to remove status but couldn't find an index to remove at! " + statusPayload.status);
                    return false;
                }
            }
            return true;
        }

        private float damageMultiplier = 1;
        private float damageReductionMultiplier = 1;
        private float damageReceivedMultiplier = 1;
        private float healingMultiplier = 1;
        private float spiritIncreaseMultiplier = 1;
        private float spiritReductionMultiplier = 1;

        public float GetMovementSpeedDecreaseAmount() { return movementSpeedDecrease.Value; }
        private NetworkVariable<float> movementSpeedDecrease = new NetworkVariable<float>();

        public float GetMovementSpeedIncreaseAmount() { return movementSpeedIncrease.Value; }
        private NetworkVariable<float> movementSpeedIncrease = new NetworkVariable<float>();

        public bool IsRooted() { return activeStatuses.Contains((int)ActionClip.Status.rooted); }
        public bool IsSilenced() { return activeStatuses.Contains((int)ActionClip.Status.silenced); }
        public bool IsFeared() { return activeStatuses.Contains((int)ActionClip.Status.fear); }
        public bool IsImmuneToGroundSpells() { return activeStatuses.Contains((int)ActionClip.Status.immuneToGroundSpells); }

        private void OnStatusChange(NetworkListEvent<ActionClip.StatusPayload> networkListEvent)
        {
            if (!IsServer) { return; }
            if (networkListEvent.Type == NetworkListEvent<ActionClip.StatusPayload>.EventType.Add) { StartCoroutine(ProcessStatusChange(networkListEvent.Value)); }
        }

        public bool ActiveStatusesWasUpdatedThisFrame { get; private set; }
        private void OnActiveStatusChange(NetworkListEvent<int> networkListEvent)
        {
            ActiveStatusesWasUpdatedThisFrame = true;
            if (resetActiveStatusesBoolCoroutine != null) { StopCoroutine(resetActiveStatusesBoolCoroutine); }
            resetActiveStatusesBoolCoroutine = StartCoroutine(ResetActiveStatusesWasUpdatedBool());
        }

        private Coroutine resetActiveStatusesBoolCoroutine;
        private IEnumerator ResetActiveStatusesWasUpdatedBool()
        {
            yield return null;
            ActiveStatusesWasUpdatedThisFrame = false;
        }

        private IEnumerator ProcessStatusChange(ActionClip.StatusPayload statusPayload)
        {
            yield return new WaitForSeconds(statusPayload.delay);
            activeStatuses.Add((int)statusPayload.status);
            switch (statusPayload.status)
            {
                case ActionClip.Status.damageMultiplier:
                    damageMultiplier *= statusPayload.value;

                    float elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    damageMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.damageReductionMultiplier:
                    damageReductionMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    damageReductionMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.damageReceivedMultiplier:
                    damageReceivedMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    damageReceivedMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.healingMultiplier:
                    healingMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    healingMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.spiritIncreaseMultiplier:
                    spiritIncreaseMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    spiritIncreaseMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.spiritReductionMultiplier:
                    spiritReductionMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    spiritReductionMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.burning:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.movementSpeedDecrease:
                    movementSpeedDecrease.Value += statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedDecrease.Value -= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.movementSpeedIncrease:
                    movementSpeedIncrease.Value += statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedIncrease.Value -= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.rooted:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.silenced:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.fear:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.healing:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        AddHP(weaponHandler.GetWeapon().GetMaxHP() / GetHP() * 10 * statusPayload.value * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.immuneToGroundSpells:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    TryRemoveStatus(statusPayload);
                    break;
                default:
                    Debug.LogError(statusPayload.status + " has not been implemented!");
                    break;
            }
        }
    }
}