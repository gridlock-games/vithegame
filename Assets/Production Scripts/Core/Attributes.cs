using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Core.GameModeManagers;

namespace Vi.Core
{
    [RequireComponent(typeof(WeaponHandler))]
    public class Attributes : CombatAgent
    {
        [SerializeField] private GameObject worldSpaceLabelPrefab;

        private NetworkVariable<int> playerDataId = new NetworkVariable<int>();
        public int GetPlayerDataId() { return playerDataId.Value; }
        public void SetPlayerDataId(int id) { playerDataId.Value = id; name = PlayerDataManager.Singleton.GetPlayerData(id).character.name.ToString(); }
        public PlayerDataManager.Team GetTeam() { return PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()).team; }

        private NetworkVariable<bool> spawnedOnOwnerInstance = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsSpawnedOnOwnerInstance() { return spawnedOnOwnerInstance.Value; }

        public Color GetRelativeTeamColor()
        {
            if (!PlayerDataManager.Singleton.ContainsId(GetPlayerDataId())) { return Color.black; }

            if (!IsClient) { return PlayerDataManager.GetTeamColor(GetTeam()); }
            else if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return Color.black; }
            else if (PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team == PlayerDataManager.Team.Spectator) { return PlayerDataManager.GetTeamColor(GetTeam()); }
            else if (IsLocalPlayer) { return Color.white; }
            else if (PlayerDataManager.CanHit(PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team, PlayerDataManager.Singleton.GetPlayerData(GetPlayerDataId()).team)) { return Color.red; }
            else { return Color.cyan; }
        }

        [SerializeField] private GameObject teamIndicatorPrefab;

        public float GetMaxHP() { return weaponHandler.GetWeapon().GetMaxHP(); }
        public float GetMaxStamina() { return weaponHandler.GetWeapon().GetMaxStamina(); }
        public float GetMaxDefense() { return weaponHandler.GetWeapon().GetMaxDefense(); }
        public float GetMaxRage() { return weaponHandler.GetWeapon().GetMaxRage(); }

        private NetworkVariable<float> HP = new NetworkVariable<float>();
        private NetworkVariable<float> stamina = new NetworkVariable<float>();
        private NetworkVariable<float> defense = new NetworkVariable<float>();
        private NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }
        public float GetStamina() { return stamina.Value; }
        public float GetDefense() { return defense.Value; }
        public float GetRage() { return rage.Value; }

        public void ResetStats(float hpPercentage, bool resetRage)
        {
            HP.Value = weaponHandler.GetWeapon().GetMaxHP() * hpPercentage;
            defense.Value = 0;
            stamina.Value = 0;
            if (resetRage)
                rage.Value = 0;
        }

        private void AddHP(float amount)
        {
            if (amount < 0) { amount *= damageReceivedMultiplier / damageReductionMultiplier; }
            if (amount > 0) { amount *= healingMultiplier; }

            if (HP.Value + amount > weaponHandler.GetWeapon().GetMaxHP())
                HP.Value = weaponHandler.GetWeapon().GetMaxHP();
            else if (HP.Value + amount < 0)
                HP.Value = 0;
            else
                HP.Value += amount;
        }

        public void AddStamina(float amount, bool activateCooldown = true)
        {
            if (activateCooldown)
                staminaDelayCooldown = weaponHandler.GetWeapon().GetStaminaDelay();

            if (stamina.Value + amount > weaponHandler.GetWeapon().GetMaxStamina())
                stamina.Value = weaponHandler.GetWeapon().GetMaxStamina();
            else if (stamina.Value + amount < 0)
                stamina.Value = 0;
            else
                stamina.Value += amount;
        }

        public void AddDefense(float amount, bool activateCooldown = true)
        {
            if (amount < 0) { amount *= defenseReductionMultiplier; }
            if (amount > 0) { amount *= defenseIncreaseMultiplier; }

            if (activateCooldown)
                defenseDelayCooldown = weaponHandler.GetWeapon().GetDefenseDelay();

            if (defense.Value + amount > weaponHandler.GetWeapon().GetMaxDefense())
                defense.Value = weaponHandler.GetWeapon().GetMaxDefense();
            else if (defense.Value + amount < 0)
                defense.Value = 0;
            else
                defense.Value += amount;
        }

        public void AddRage(float amount)
        {
            if (rage.Value + amount > weaponHandler.GetWeapon().GetMaxRage())
                rage.Value = weaponHandler.GetWeapon().GetMaxRage();
            else if (rage.Value + amount < 0)
                rage.Value = 0;
            else
                rage.Value += amount;
        }

        GameObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            if (IsServer) { StartCoroutine(InitHP()); }
            HP.OnValueChanged += OnHPChanged;
            ailment.OnValueChanged += OnAilmentChanged;
            isInvincible.OnValueChanged += OnIsInvincibleChange;
            isUninterruptable.OnValueChanged += OnIsUninterruptableChange;
            statuses.OnListChanged += OnStatusChange;
            comboCounter.OnValueChanged += OnComboCounterChange;

            if (!IsLocalPlayer) { worldSpaceLabelInstance = Instantiate(worldSpaceLabelPrefab, transform); }
            StartCoroutine(AddPlayerObjectToGameLogicManager());

            if (IsOwner) { spawnedOnOwnerInstance.Value = true; }
        }

        private IEnumerator InitHP()
        {
            yield return new WaitUntil(() => weaponHandler.GetWeapon() != null);
            HP.Value = weaponHandler.GetWeapon().GetMaxHP();
        }

        private IEnumerator AddPlayerObjectToGameLogicManager()
        {
            if (!(IsHost & IsLocalPlayer)) { yield return new WaitUntil(() => GetPlayerDataId() != (int)NetworkManager.ServerClientId); }
            PlayerDataManager.Singleton.AddPlayerObject(GetPlayerDataId(), this);
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHPChanged;
            ailment.OnValueChanged -= OnAilmentChanged;
            isInvincible.OnValueChanged -= OnIsInvincibleChange;
            isUninterruptable.OnValueChanged -= OnIsUninterruptableChange;
            statuses.OnListChanged -= OnStatusChange;
            comboCounter.OnValueChanged -= OnComboCounterChange;

            if (worldSpaceLabelInstance) { Destroy(worldSpaceLabelInstance); }
            PlayerDataManager.Singleton.RemovePlayerObject(GetPlayerDataId());
        }

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
        }

        public GlowRenderer GlowRenderer { get; private set; }
        private void OnTransformChildrenChanged()
        {
            GlowRenderer = GetComponentInChildren<GlowRenderer>();
        }

        private WeaponHandler weaponHandler;
        private AnimationHandler animationHandler;
        private void Awake()
        {
            statuses = new NetworkList<ActionClip.StatusPayload>();
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
        }

        private GameObject teamIndicatorInstance;
        private void Start()
        {
            teamIndicatorInstance = Instantiate(teamIndicatorPrefab, transform);
        }

        private void OnEnable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.SetActive(true); }
        }

        private void OnDisable()
        {
            if (worldSpaceLabelInstance) { worldSpaceLabelInstance.SetActive(false); }
        }

        public bool IsInvincible { get; private set; }
        private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>();
        private void OnIsInvincibleChange(bool prev, bool current) { IsInvincible = current; }
        private float invincibilityEndTime;
        public void SetInviniciblity(float duration) { invincibilityEndTime = Time.time + duration; }

        public bool IsUninterruptable { get; private set; }
        private NetworkVariable<bool> isUninterruptable = new NetworkVariable<bool>();
        private void OnIsUninterruptableChange(bool prev, bool current) { IsUninterruptable = current; }
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

        public bool ProcessProjectileHit(Attributes attacker, RuntimeWeapon runtimeWeapon, Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessProjectileHit() should only be called on the server!"); return false; }

            return ProcessHit(false, attacker, attack, impactPosition, hitSourcePosition, hitCounter, runtimeWeapon);
        }

        public bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamage() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                ailment.Value = ActionClip.Ailment.Death;
                killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                animationHandler.PlayAction(weaponHandler.GetWeapon().GetDeathReaction());

                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
            }
            RenderHitGlowOnly();
            AddHP(damage);
            return true;
        }

        public bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessEnvironmentDamageWithHitReaction() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                ailment.Value = ActionClip.Ailment.Death;
                killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                animationHandler.PlayAction(weaponHandler.GetWeapon().GetDeathReaction());

                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
            }
            else
            {
                ActionClip hitReaction = weaponHandler.GetWeapon().GetHitReactionByDirection(Weapon.HitLocation.Front);
                animationHandler.PlayAction(hitReaction);
            }

            RenderHit(attackingNetworkObject.NetworkObjectId, transform.position, false);
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

        private bool ProcessHit(bool isMeleeHit, Attributes attacker, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<Attributes, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null)
        {
            if (isMeleeHit)
            {
                if (!runtimeWeapon) { Debug.LogError("When processing a melee hit, you need to pass in a runtime weapon!"); return false; }
            }

            if (GetAilment() == ActionClip.Ailment.Death | attacker.GetAilment() == ActionClip.Ailment.Death) { return false; }
            if (!PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    TryAddStatus(status.status, status.value, status.duration, status.delay);
                }
                return false;
            }

            if (attack.maxHitLimit == 0) { return false; }

            if (IsInvincible) { return false; }
            if (isMeleeHit)
            {
                if (attacker.wasStaggeredThisFrame) { Debug.Log(attacker + " was staggered"); return false; }

                if (!IsUninterruptable)
                {
                    wasStaggeredThisFrame = true;
                    StartCoroutine(ResetStaggerBool());
                }
            }

            // Combination ailment logic here
            bool applyAilmentRegardless = false;
            ActionClip.Ailment attackAilment = attack.ailment == ActionClip.Ailment.Grab ? ActionClip.Ailment.None : attack.ailment;

            if (attack.ailment != ActionClip.Ailment.Grab)
            {
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
            }

            if (ailment.Value == ActionClip.Ailment.Stun & attackAilment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Stun & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockup; }
            if (ailment.Value == ActionClip.Ailment.Stun & attack.isFollowUpAttack) { attackAilment = ActionClip.Ailment.Stagger; }

            if (ailment.Value == ActionClip.Ailment.Stagger & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }

            if (ailment.Value == ActionClip.Ailment.Knockup & attackAilment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attackAilment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attack.GetClipType() == ActionClip.ClipType.FlashAttack) { attackAilment = ActionClip.Ailment.Knockup; applyAilmentRegardless = true; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attack.isFollowUpAttack) { attackAilment = ActionClip.Ailment.Knockup; applyAilmentRegardless = true; }

            if (ailment.Value != ActionClip.Ailment.None & attackAilment == ActionClip.Ailment.Pull) { attackAilment = ActionClip.Ailment.None; }

            if (IsUninterruptable) { attackAilment = ActionClip.Ailment.None; }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, weaponHandler.IsBlocking, attackAilment, ailment.Value);
            hitReaction.hitReactionRootMotionForwardMultiplier = attack.attackRootMotionForwardMultiplier;
            hitReaction.hitReactionRootMotionSidesMultiplier = attack.attackRootMotionSidesMultiplier;
            hitReaction.hitReactionRootMotionVerticalMultiplier = attack.attackRootMotionVerticalMultiplier;

            float damage = hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking ? -attack.damage * 0.7f : -attack.damage;
            damage *= attacker.damageMultiplier;

            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                damage *= attacker.animationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.animationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    damage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (HP.Value + damage <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, weaponHandler.IsBlocking, attackAilment, ailment.Value);
            }

            if (damage != 0)
            {
                if (!IsUninterruptable | hitReaction.ailment == ActionClip.Ailment.Death) { animationHandler.PlayAction(hitReaction); }
            }

            if (runtimeWeapon) { runtimeWeapon.AddHit(this); }

            shouldShake = true;
            attacker.shouldShake = false;

            hitFreezeStartTime = Time.time;
            attacker.hitFreezeStartTime = Time.time;

            if (hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                RenderBlock(impactPosition);
                AddHP(damage);
            }
            else // Not blocking
            {
                StartCoroutine(EvaluateAfterHitStop(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attack));

                if (damage != 0)
                {
                    RenderHit(attacker.NetworkObjectId, impactPosition, attackAilment == ActionClip.Ailment.Knockdown);
                    AddHP(damage);
                }
            }

            attacker.comboCounter.Value += 1;

            AddStamina(-attack.staminaDamage);
            AddDefense(-attack.defenseDamage);
            attacker.AddRage(rageToBeAddedOnHit);

            foreach (ActionVFX actionVFX in attack.actionVFXList)
            {
                if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnHit) { continue; }
                weaponHandler.SpawnActionVFX(weaponHandler.CurrentActionClip, actionVFX, attacker.transform);
            }

            foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTargetOnHit)
            {
                TryAddStatus(status.status, status.value, status.duration, status.delay);
            }

            return true;
        }

        private IEnumerator EvaluateAfterHitStop(ActionClip.Ailment attackAilment, bool applyAilmentRegardless, Vector3 hitSourcePosition, Attributes attacker, ActionClip attack)
        {
            yield return new WaitForSeconds(ActionClip.HitStopEffectDuration);

            // Ailments
            if (attackAilment != ailment.Value | applyAilmentRegardless)
            {
                bool shouldApplyAilment = false;
                if (attackAilment != ActionClip.Ailment.None & attackAilment != ActionClip.Ailment.Pull)
                {
                    Vector3 startPos = transform.position;
                    Vector3 endPos = hitSourcePosition;
                    startPos.y = 0;
                    endPos.y = 0;
                    ailmentRotation.Value = Quaternion.LookRotation(endPos - startPos, Vector3.up);

                    shouldApplyAilment = true;
                    ailment.Value = attackAilment;
                    if (ailment.Value == ActionClip.Ailment.Death)
                    {
                        if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(attacker, this); }
                        SetKiller(attacker);
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
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockdownDuration, true));
                            break;
                        case ActionClip.Ailment.Knockup:
                            knockupHitCounter = 0;
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(knockupDuration, false));
                            break;
                        case ActionClip.Ailment.Stun:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(stunDuration, false));
                            break;
                        case ActionClip.Ailment.Stagger:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays());
                            break;
                        case ActionClip.Ailment.Pull:
                            ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays());
                            break;
                        case ActionClip.Ailment.Death:
                            break;
                        default:
                            Debug.LogWarning(attackAilment + " has not been implemented yet!");
                            break;
                    }
                }
            }

            if (ailment.Value == ActionClip.Ailment.Knockup)
            {
                knockupHitCounter++;
                if (knockupHitCounter >= knockupHitLimit)
                {
                    if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
                    SetInviniciblity(recoveryTimeInvincibilityBuffer);
                    ailment.Value = ActionClip.Ailment.None;
                }
            }
        }

        private int knockupHitCounter;
        private const int knockupHitLimit = 5;

        private const float stunDuration = 3;
        private const float knockdownDuration = 2;
        private const float knockupDuration = 4;
        private const float rageToBeAddedOnHit = 2;

        private void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, bool isKnockdown)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }
            
            if (!IsClient)
            {
                GlowRenderer.RenderHit();
                StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
                Weapon weapon = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<WeaponHandler>().GetWeapon();
                AudioManager.Singleton.PlayClipAtPoint(isKnockdown ? weapon.knockbackHitAudioClip : weapon.hitAudioClip, impactPosition);
            }

            RenderHitClientRpc(attackerNetObjId, impactPosition, isKnockdown);
        }

        [ClientRpc]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, bool isKnockdown)
        {
            GlowRenderer.RenderHit();
            StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
            Weapon weapon = NetworkManager.SpawnManager.SpawnedObjects[attackerNetObjId].GetComponent<WeaponHandler>().GetWeapon();
            AudioManager.Singleton.PlayClipAtPoint(isKnockdown ? weapon.knockbackHitAudioClip : weapon.hitAudioClip, impactPosition);
        }

        private void RenderHitGlowOnly()
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHitGlowOnly() should only be called from the server"); return; }

            if (!IsClient)
            {
                GlowRenderer.RenderHit();
            }

            RenderHitGlowOnlyClientRpc();
        }

        [ClientRpc]
        private void RenderHitGlowOnlyClientRpc()
        {
            GlowRenderer.RenderHit();
        }

        private void RenderBlock(Vector3 impactPosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            if (!IsClient)
            {
                GlowRenderer.RenderBlock();
                StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
                AudioManager.Singleton.PlayClipAtPoint(weaponHandler.GetWeapon().blockAudioClip, impactPosition);
            }

            RenderBlockClientRpc(impactPosition);
        }

        [ClientRpc] private void RenderBlockClientRpc(Vector3 impactPosition)
        {
            GlowRenderer.RenderBlock();
            StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(weaponHandler.GetWeapon().blockAudioClip, impactPosition);
        }

        public ulong GetRoundTripTime() { return roundTripTime.Value; }

        private NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        private void Update()
        {
            if (!IsSpawned) { return; }

            GlowRenderer.RenderInvincible(IsInvincible);
            GlowRenderer.RenderUninterruptable(IsUninterruptable);

            if (!IsServer) { return; }

            if (Time.time - lastComboCounterChangeTime >= comboCounterResetTime) { comboCounter.Value = 0; }

            isInvincible.Value = Time.time <= invincibilityEndTime;
            isUninterruptable.Value = Time.time <= uninterruptableEndTime;

            UpdateStamina();
            UpdateDefense();
            UpdateRage();

            roundTripTime.Value = NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(OwnerClientId);
        }

        private float staminaDelayCooldown;
        private void UpdateStamina()
        {
            staminaDelayCooldown = Mathf.Max(0, staminaDelayCooldown - Time.deltaTime);
            if (staminaDelayCooldown > 0) { return; }
            AddStamina(weaponHandler.GetWeapon().GetStaminaRecoveryRate() * Time.deltaTime, false);
        }

        private float defenseDelayCooldown;
        private void UpdateDefense()
        {
            if (weaponHandler.IsBlocking) { return; }

            defenseDelayCooldown = Mathf.Max(0, defenseDelayCooldown - Time.deltaTime);
            if (defenseDelayCooldown > 0) return;
            AddDefense(weaponHandler.GetWeapon().GetDefenseRecoveryRate() * Time.deltaTime, false);
        }

        private float rageDelayCooldown;
        private void UpdateRage()
        {
            rageDelayCooldown = Mathf.Max(0, rageDelayCooldown - Time.deltaTime);
            if (rageDelayCooldown > 0) { return; }
            AddRage(weaponHandler.GetWeapon().GetRageRecoveryRate() * Time.deltaTime);
        }

        private NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        private NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>(Quaternion.Euler(0, 0, 0)); // Don't remove the Quaternion.Euler() call, for some reason it's necessary BLACK MAGIC

        private void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            animationHandler.Animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }

            if (current == ActionClip.Ailment.Death)
            {
                animationHandler.Animator.enabled = false;
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.SetActive(false); }
                respawnCoroutine = StartCoroutine(RespawnSelf());
            }
            else if (prev == ActionClip.Ailment.Death)
            {
                animationHandler.Animator.enabled = true;
                if (worldSpaceLabelInstance) { worldSpaceLabelInstance.SetActive(true); }
                if (respawnCoroutine != null) { StopCoroutine(respawnCoroutine); }
            }
        }

        public float GetRespawnTime() { return Mathf.Clamp(GameModeManager.Singleton.GetRespawnTime() - (Time.time - respawnSelfCalledTime), 0, GameModeManager.Singleton.GetRespawnTime()); }
        public float GetRespawnTimeAsPercentage() { return 1 - (GetRespawnTime() / GameModeManager.Singleton.GetRespawnTime()); }

        public bool IsRespawning { get; private set; }
        public bool isWaitingForSpawnPoint;
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
        public bool ShouldApplyAilmentRotation() { return ailment.Value != ActionClip.Ailment.None & ailment.Value != ActionClip.Ailment.Pull; }
        public Quaternion GetAilmentRotation() { return ailmentRotation.Value; }

        private const float recoveryTimeInvincibilityBuffer = 1;
        private Coroutine ailmentResetCoroutine;
        private IEnumerator ResetAilmentAfterDuration(float duration, bool shouldMakeInvincible)
        {
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
            if (shouldMakeInvincible) { SetInviniciblity(duration); }
            yield return new WaitForSeconds(duration);
            SetInviniciblity(recoveryTimeInvincibilityBuffer);
            ailment.Value = ActionClip.Ailment.None;
        }

        private IEnumerator ResetAilmentAfterAnimationPlays()
        {
            yield return new WaitUntil(() => !animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName("Empty"));
            yield return new WaitUntil(() => animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName("Empty"));
            ailment.Value = ActionClip.Ailment.None;
        }

        public NetworkList<ActionClip.StatusPayload> GetActiveStatuses() { return statuses; }
        private NetworkList<ActionClip.StatusPayload> statuses;

        private List<ActionClip.Status> activeStatuses = new List<ActionClip.Status>();

        public bool TryAddStatus(ActionClip.Status status, float value, float duration, float delay)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryAddStatus() should only be called on the server"); return false; }
            statuses.Add(new ActionClip.StatusPayload(status, value, duration, delay));
            return true;
        }

        private bool TryRemoveStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryRemoveStatus() should only be called on the server"); return false; }

            if (!statuses.Contains(statusPayload) & !activeStatuses.Contains(statusPayload.status))
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
                    activeStatuses.Remove(statusPayload.status);
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
        private float defenseIncreaseMultiplier = 1;
        private float defenseReductionMultiplier = 1;
        
        public float GetMovementSpeedDecreaseAmount() { return movementSpeedDecrease.Value; }
        private NetworkVariable<float> movementSpeedDecrease = new NetworkVariable<float>();

        public float GetMovementSpeedIncreaseAmount() { return movementSpeedIncrease.Value; }
        private NetworkVariable<float> movementSpeedIncrease = new NetworkVariable<float>();

        public bool IsRooted() { return activeStatuses.Contains(ActionClip.Status.rooted); }
        public bool IsSilenced() { return activeStatuses.Contains(ActionClip.Status.silenced); }
        public bool IsFeared() { return activeStatuses.Contains(ActionClip.Status.fear); }

        private void OnStatusChange(NetworkListEvent<ActionClip.StatusPayload> networkListEvent)
        {
            if (!IsServer) { return; }
            if (networkListEvent.Type == NetworkListEvent<ActionClip.StatusPayload>.EventType.Add) { StartCoroutine(ProcessStatusChange(networkListEvent.Value)); }
        }

        private IEnumerator ProcessStatusChange(ActionClip.StatusPayload statusPayload)
        {
            yield return new WaitForSeconds(statusPayload.delay);
            activeStatuses.Add(statusPayload.status);
            switch (statusPayload.status)
            {
                case ActionClip.Status.damageMultiplier:
                    damageMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    damageMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.damageReductionMultiplier:
                    damageReductionMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    damageReductionMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.damageReceivedMultiplier:
                    damageReceivedMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    damageReceivedMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.healingMultiplier:
                    healingMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    healingMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.defenseIncreaseMultiplier:
                    defenseIncreaseMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    defenseIncreaseMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.defenseReductionMultiplier:
                    defenseReductionMultiplier *= statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    defenseReductionMultiplier /= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.burning:
                    float elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        ProcessEnvironmentDamage(GetHP() * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.movementSpeedDecrease:
                    movementSpeedDecrease.Value += statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    movementSpeedDecrease.Value -= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.movementSpeedIncrease:
                    movementSpeedIncrease.Value += statusPayload.value;
                    yield return new WaitForSeconds(statusPayload.duration);
                    movementSpeedIncrease.Value -= statusPayload.value;
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.rooted:
                    yield return new WaitForSeconds(statusPayload.duration);
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.silenced:
                    yield return new WaitForSeconds(statusPayload.duration);
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.fear:
                    yield return new WaitForSeconds(statusPayload.duration);
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.healing:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        AddHP(weaponHandler.GetWeapon().GetMaxHP() / GetHP() * 10 * statusPayload.value * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
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