using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

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
        private AnimationHandler animationHandler;
        private WeaponHandler weaponHandler;
        private Animator animator;
        private void Awake()
        {
            glowRenderer = GetComponentInChildren<GlowRenderer>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            animator = GetComponentInChildren<Animator>();
            statuses = new NetworkList<ActionClip.StatusPayload>();
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
                RenderBlock();
                AddHP(-attack.damage * 0.7f);
            }
            else // Not blocking
            {
                RenderHit();
                AddHP(-attack.damage);
            }

            AddStamina(-attack.staminaDamage);
            AddDefense(-attack.defenseDamage);

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

        private void RenderHit()
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            if (!IsClient)
                glowRenderer.RenderHit();

            RenderHitClientRpc();
        }

        [ClientRpc] private void RenderHitClientRpc() { glowRenderer.RenderHit(); }

        private void RenderBlock()
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderBlock() should only be called from the server"); return; }

            if (!IsClient)
                glowRenderer.RenderBlock();
            RenderBlockClientRpc();
        }

        [ClientRpc] private void RenderBlockClientRpc() { glowRenderer.RenderBlock(); }

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
            animator.SetBool("CanResetAilment", current == ActionClip.Ailment.None);
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
            yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"));
            ailment.Value = ActionClip.Ailment.None;
        }

        public NetworkList<ActionClip.StatusPayload> GetActiveStatuses() { return statuses; }
        private NetworkList<ActionClip.StatusPayload> statuses;

        public bool TryAddStatus(ActionClip.Status status, float value, float duration, float delay)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryAddStatus() should only be called on the server"); return false; }

            ActionClip.StatusPayload charStatus = new ActionClip.StatusPayload(status, value, duration, delay);
            if (statuses.Contains(charStatus))
            {
                int index = statuses.IndexOf(charStatus);
                statuses[index] = charStatus;
            }
            else
            {
                statuses.Add(charStatus);
            }
            return true;
        }

        private bool TryRemoveStatus(ActionClip.Status status)
        {
            if (!IsServer) { Debug.LogError("CharacterStatusManager.TryRemoveStatus() should only be called on the server"); return false; }

            ActionClip.StatusPayload charStatus = new ActionClip.StatusPayload(status, 0, 0, 0);
            if (!statuses.Contains(charStatus))
            {
                return false;
            }
            else
            {
                statuses.Remove(charStatus);
            }
            return true;
        }

        private float damageMultiplier;
        private float damageReductionMultiplier;
        private float damageReceivedMultiplier;
        private float healingMultiplier;
        private float defenseIncreaseMultiplier;
        private float defenseReductionMultiplier;
        private bool burning;
        private bool poisoned;
        private bool drain;
        private float movementSpeedDecrease;
        private float movementSpeedIncrease;
        private bool rooted;
        private bool silenced;
        private bool fear;
        private bool healing;
        private void OnStatusChange(NetworkListEvent<ActionClip.StatusPayload> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<ActionClip.StatusPayload>.EventType.Add | networkListEvent.Type == NetworkListEvent<ActionClip.StatusPayload>.EventType.Value)
            {
                switch (networkListEvent.Value.status)
                {
                    case ActionClip.Status.damageMultiplier:
                        break;
                    case ActionClip.Status.damageReductionMultiplier:
                        break;
                    case ActionClip.Status.damageReceivedMultiplier:
                        break;
                    case ActionClip.Status.healingMultiplier:
                        break;
                    case ActionClip.Status.defenseIncreaseMultiplier:
                        break;
                    case ActionClip.Status.defenseReductionMultiplier:
                        break;
                    case ActionClip.Status.burning:
                        break;
                    case ActionClip.Status.poisoned:
                        break;
                    case ActionClip.Status.drain:
                        break;
                    case ActionClip.Status.movementSpeedDecrease:
                        break;
                    case ActionClip.Status.movementSpeedIncrease:
                        break;
                    case ActionClip.Status.rooted:
                        break;
                    case ActionClip.Status.silenced:
                        break;
                    case ActionClip.Status.fear:
                        break;
                    case ActionClip.Status.healing:
                        break;
                    default:
                        Debug.Log(networkListEvent.Value.status + " has not been implemented for status add or value change");
                        break;
                }
            }
        }
    }
}