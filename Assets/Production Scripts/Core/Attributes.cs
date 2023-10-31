using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.VFX;

namespace Vi.Core
{
    public class Attributes : NetworkBehaviour
    {
        [SerializeField] private GameObject worldSpaceLabelPrefab;

        [Header("Health")]
        [SerializeField] private float maxHP = 100;
        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100;
        [SerializeField] private float staminaRecoveryRate = 5;
        [SerializeField] private float staminaDelay = 1;
        [Header("Defense")]
        [SerializeField] private float maxDefense = 100;
        [SerializeField] private float defenseRecoveryRate = 5;
        [SerializeField] private float defenseDelay = 1;
        [Header("Rage")]
        [SerializeField] private float maxRage = 100;
        [SerializeField] private float rageRecoveryRate = 0;

        public float GetMaxHP() { return maxHP; }
        public float GetMaxStamina() { return maxStamina; }
        public float GetMaxDefense() { return maxDefense; }
        public float GetMaxRage() { return maxRage; }

        private NetworkVariable<float> HP = new NetworkVariable<float>();
        private NetworkVariable<float> stamina = new NetworkVariable<float>();
        private NetworkVariable<float> defense = new NetworkVariable<float>();
        private NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }
        public float GetStamina() { return stamina.Value; }
        public float GetDefense() { return defense.Value; }
        public float GetRage() { return rage.Value; }

        public void ResetStats(bool resetRage)
        {
            HP.Value = maxHP;
            defense.Value = 0;
            stamina.Value = 0;
            if (resetRage)
                rage.Value = 0;
        }

        public void AddHP(float amount)
        {
            if (amount < 0) { amount *= damageReceivedMultiplier / damageReductionMultiplier; }
            if (amount > 0) { amount *= healingMultiplier; }

            if (HP.Value + amount > maxHP)
                HP.Value = maxHP;
            else if (HP.Value + amount < 0)
                HP.Value = 0;
            else
                HP.Value += amount;
        }

        public void AddStamina(float amount, bool activateCooldown = true)
        {
            if (activateCooldown)
                staminaDelayCooldown = staminaDelay;

            if (stamina.Value + amount > maxStamina)
                stamina.Value = maxStamina;
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
                defenseDelayCooldown = defenseDelay;

            if (defense.Value + amount > maxDefense)
                defense.Value = maxDefense;
            else if (defense.Value + amount < 0)
                defense.Value = 0;
            else
                defense.Value += amount;
        }

        public void AddRage(float amount)
        {
            if (rage.Value + amount > maxRage)
                rage.Value = maxRage;
            else if (rage.Value + amount < 0)
                rage.Value = 0;
            else
                rage.Value += amount;
        }

        GameObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            HP.Value = maxHP;
            HP.OnValueChanged += OnHPChanged;
            ailment.OnValueChanged += OnAilmentChanged;
            isInvincible.OnValueChanged += OnIsInvincibleChange;
            isUninterruptable.OnValueChanged += OnIsUninterruptableChange;
            statuses.OnListChanged += OnStatusChange;

            if (!IsLocalPlayer) { worldSpaceLabelInstance = Instantiate(worldSpaceLabelPrefab, transform); }
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHPChanged;
            ailment.OnValueChanged -= OnAilmentChanged;
            isInvincible.OnValueChanged -= OnIsInvincibleChange;
            isUninterruptable.OnValueChanged -= OnIsUninterruptableChange;
            statuses.OnListChanged -= OnStatusChange;

            if (worldSpaceLabelInstance) { Destroy(worldSpaceLabelInstance); }
        }

        private void OnHPChanged(float prev, float current)
        {
            if (current < prev)
            {
                //glowRenderer.RenderHit();

                if (prev <= 0 & current > 0)
                {
                    // Character.CancelDeath();
                }
                else
                {
                    
                }
            }
            else if (current > prev)
            {
                glowRenderer.RenderHeal();
            }
        }

        private GlowRenderer glowRenderer;
        private void OnTransformChildrenChanged()
        {
            glowRenderer = GetComponentInChildren<GlowRenderer>();
        }

        private WeaponHandler weaponHandler;
        private AnimationHandler animationHandler;
        private void Awake()
        {
            statuses = new NetworkList<ActionClip.StatusPayload>();
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
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
        public bool ProcessMeleeHit(Attributes attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, float attackAngle)
        {
            if (!IsServer) { Debug.LogError("Attributes.ProcessMeleeHit() should only be called on the server!"); return false; }
            if (IsInvincible) { return false; }
            if (attacker.wasStaggeredThisFrame) { Debug.Log(attacker + " was staggered"); return false; }

            if (!IsUninterruptable)
            {
                wasStaggeredThisFrame = true;
                StartCoroutine(ResetStaggerBool());
            }
            
            // Combination ailment logic here
            ActionClip.Ailment attackAilment = attack.ailment;
            if (ailment.Value == ActionClip.Ailment.Stun & attack.ailment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Stun & attack.ailment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockup; }
            
            if (ailment.Value == ActionClip.Ailment.Stagger & attack.ailment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }

            if (ailment.Value == ActionClip.Ailment.Knockup & attack.ailment == ActionClip.Ailment.Stun) { attackAilment = ActionClip.Ailment.Knockdown; }
            if (ailment.Value == ActionClip.Ailment.Knockup & attack.ailment == ActionClip.Ailment.Stagger) { attackAilment = ActionClip.Ailment.Knockdown; }

            ActionClip hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, weaponHandler.IsBlocking, attackAilment, ailment.Value);
            animationHandler.PlayAction(hitReaction);

            runtimeWeapon.AddHit(this);

            if (hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                RenderBlock(impactPosition);
                AddHP(-attack.damage * 0.7f * attacker.damageMultiplier);
            }
            else // Not blocking
            {
                // Ailments
                if (attackAilment != ailment.Value)
                {
                    bool ailmentChangedOnThisAttack = false;
                    if (attackAilment != ActionClip.Ailment.None)
                    {
                        Vector3 startPos = transform.position;
                        Vector3 endPos = attacker.transform.position;
                        startPos.y = 0;
                        endPos.y = 0;
                        ailmentRotation.Value = Quaternion.LookRotation(endPos - startPos, Vector3.up);

                        ailmentChangedOnThisAttack = ailment.Value != attackAilment;
                        ailment.Value = attackAilment;
                    }
                    else // If this attack's ailment is none
                    {
                        if (ailment.Value == ActionClip.Ailment.Stun | ailment.Value == ActionClip.Ailment.Stagger)
                        {
                            ailment.Value = ActionClip.Ailment.None;
                        }
                    }

                    // If we started a new ailment on this attack, we want to start a reset coroutine
                    if (ailmentChangedOnThisAttack)
                    {
                        switch (ailment.Value)
                        {
                            case ActionClip.Ailment.Knockdown:
                                ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(attack.ailmentDuration, true));
                                break;
                            case ActionClip.Ailment.Knockup:
                                ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(attack.ailmentDuration, false));
                                break;
                            case ActionClip.Ailment.Stun:
                                ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterDuration(attack.ailmentDuration, false));
                                break;
                            case ActionClip.Ailment.Stagger:
                                ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays());
                                break;
                            case ActionClip.Ailment.Pull:
                                ailmentResetCoroutine = StartCoroutine(ResetAilmentAfterAnimationPlays());
                                break;
                            default:
                                Debug.LogWarning(attackAilment + " has not been implemented yet!");
                                break;
                        }
                    }
                }

                RenderHit(impactPosition, ailment.Value == ActionClip.Ailment.Knockdown);
                AddHP(-attack.damage * attacker.damageMultiplier);
            }

            AddStamina(-attack.staminaDamage);
            AddDefense(-attack.defenseDamage);

            foreach (ActionVFX actionVFX in attack.actionVFXList)
            {
                if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnHit) { continue; }
                weaponHandler.SpawnActionVFX(actionVFX, attacker.transform);
            }

            return true;
        }

        private IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        public void ProcessProjectileHit()
        {

        }

        private void RenderHit(Vector3 impactPosition, bool isKnockdown)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            if (!IsClient)
            {
                glowRenderer.RenderHit();
                StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
                AudioManager.Singleton.PlayClipAtPoint(isKnockdown ? weaponHandler.GetWeapon().knockbackHitAudioClip : weaponHandler.GetWeapon().hitAudioClip, impactPosition);
            }

            RenderHitClientRpc(impactPosition, isKnockdown);
        }

        [ClientRpc] private void RenderHitClientRpc(Vector3 impactPosition, bool isKnockdown)
        {
            glowRenderer.RenderHit();
            StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().hitVFXPrefab, impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(isKnockdown ? weaponHandler.GetWeapon().knockbackHitAudioClip : weaponHandler.GetWeapon().hitAudioClip, impactPosition);
        }

        private void RenderBlock(Vector3 impactPosition)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            if (!IsClient)
            {
                glowRenderer.RenderBlock();
                StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
                AudioManager.Singleton.PlayClipAtPoint(weaponHandler.GetWeapon().blockAudioClip, impactPosition);
            }

            RenderBlockClientRpc(impactPosition);
        }

        [ClientRpc] private void RenderBlockClientRpc(Vector3 impactPosition)
        {
            glowRenderer.RenderBlock();
            StartCoroutine(weaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(weaponHandler.GetWeapon().blockVFXPrefab, impactPosition, Quaternion.identity)));
            AudioManager.Singleton.PlayClipAtPoint(weaponHandler.GetWeapon().blockAudioClip, impactPosition);
        }

        public ulong GetRoundTripTime() { return roundTripTime.Value; }

        private NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        private void Update()
        {
            glowRenderer.RenderInvincible(IsInvincible);
            glowRenderer.RenderUninterruptable(IsUninterruptable);

            if (!IsServer) { return; }

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
            AddStamina(staminaRecoveryRate * Time.deltaTime, false);
        }

        private float defenseDelayCooldown;
        private void UpdateDefense()
        {
            if (weaponHandler.IsBlocking) { return; }

            defenseDelayCooldown = Mathf.Max(0, defenseDelayCooldown - Time.deltaTime);
            if (defenseDelayCooldown > 0) return;
            AddDefense(defenseRecoveryRate * Time.deltaTime, false);
        }

        private float rageDelayCooldown;
        private void UpdateRage()
        {
            rageDelayCooldown = Mathf.Max(0, rageDelayCooldown - Time.deltaTime);
            if (rageDelayCooldown > 0) { return; }
            AddRage(rageRecoveryRate * Time.deltaTime);
        }

        private NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        private NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>(Quaternion.Euler(0, 0, 0)); // Don't remove the Quaternion.Euler() call, for some reason it's necessary BLACK MAGIC

        private void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            animationHandler.Animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);
            if (ailmentResetCoroutine != null) { StopCoroutine(ailmentResetCoroutine); }
        }

        public ActionClip.Ailment GetAilment() { return ailment.Value; }
        public bool ShouldApplyAilmentRotation() { return ailment.Value != ActionClip.Ailment.None; }
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

        public bool TryAddStatus(ActionClip.Status status, float value, float duration, float delay)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryAddStatus() should only be called on the server"); return false; }
            statuses.Add(new ActionClip.StatusPayload(status, value, duration, delay));
            return true;
        }

        private bool TryRemoveStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryRemoveStatus() should only be called on the server"); return false; }

            if (!statuses.Contains(statusPayload))
            {
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
                }
                else
                {
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

        public bool IsRooted() { return statuses.Contains(new ActionClip.StatusPayload(ActionClip.Status.rooted, 0, 0, 0)); }
        public bool IsSilenced() { return statuses.Contains(new ActionClip.StatusPayload(ActionClip.Status.silenced, 0, 0, 0)); }
        public bool IsFeared() { return statuses.Contains(new ActionClip.StatusPayload(ActionClip.Status.fear, 0, 0, 0)); }

        private void OnStatusChange(NetworkListEvent<ActionClip.StatusPayload> networkListEvent)
        {
            if (!IsServer) { return; }
            if (networkListEvent.Type == NetworkListEvent<ActionClip.StatusPayload>.EventType.Add) { StartCoroutine(ProcessStatusChange(networkListEvent.Value)); }
        }

        private IEnumerator ProcessStatusChange(ActionClip.StatusPayload statusPayload)
        {
            yield return new WaitForSeconds(statusPayload.delay);
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
                        AddHP(GetHP() * -statusPayload.value * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        AddHP(GetHP() * -statusPayload.value * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    TryRemoveStatus(statusPayload);
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration)
                    {
                        AddHP(GetHP() * -statusPayload.value * Time.deltaTime);
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
                        AddHP(maxHP / GetHP() * 10 * statusPayload.value * Time.deltaTime);
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