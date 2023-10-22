using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class Attributes : NetworkBehaviour
    {
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
            if (HP.Value > maxHP)
                HP.Value = maxHP;
            else if (HP.Value < 0)
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

        public override void OnNetworkSpawn()
        {
            HP.Value = maxHP;
            HP.OnValueChanged += OnHPChanged;
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHPChanged;
        }

        private void OnHPChanged(float prev, float current)
        {
            if (current < prev)
            {
                glowRenderer.RenderHit();

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
        private void Awake()
        {
            glowRenderer = GetComponentInChildren<GlowRenderer>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
        }

        public bool IsInvincible => Time.time <= invincibilityEndTime;
        private float invincibilityEndTime;
        public void SetInviniciblity(float duration) { invincibilityEndTime = Time.time + duration; }

        public bool IsUninterruptable => Time.time <= uninterruptableEndTime;
        private float uninterruptableEndTime;
        public void SetUninterruptable(float duration) { uninterruptableEndTime = Time.time + duration; }

        private bool wasStaggeredThisFrame;
        public void ProcessMeleeHit(Attributes attacker, Vector3 impactPosition, ActionClip hitReaction, ActionClip attack)
        {
            if (attacker.wasStaggeredThisFrame) { return; }

            if (!IsUninterruptable)
            {
                wasStaggeredThisFrame = true;
                animationHandler.PlayAction(hitReaction);
                StartCoroutine(ResetStaggerBool());
            }
            AddHP(-attack.damage);
            AddStamina(-attack.staminaDamage);
            AddDefense(-attack.defenseDamage);
        }

        private IEnumerator ResetStaggerBool()
        {
            yield return null;
            wasStaggeredThisFrame = false;
        }

        public void ProcessProjectileHit()
        {

        }

        private void Update()
        {
            if (!IsServer) { return; }

            glowRenderer.RenderInvincible(IsInvincible);

            UpdateStamina();
            UpdateDefense();
            UpdateRage();
        }

        private float staminaDelayCooldown;
        private void UpdateStamina()
        {
            staminaDelayCooldown = Mathf.Max(0, staminaDelayCooldown - Time.deltaTime);
            if (staminaDelayCooldown > 0) return;
            AddStamina(staminaRecoveryRate * Time.deltaTime, false);
        }

        private float defenseDelayCooldown;
        private void UpdateDefense()
        {
            //if (IsBlocking.Value) return;

            defenseDelayCooldown = Mathf.Max(0, defenseDelayCooldown - Time.deltaTime);
            if (defenseDelayCooldown > 0) return;
            AddDefense(defenseRecoveryRate * Time.deltaTime, false);
        }

        private float rageDelayCooldown;
        private void UpdateRage()
        {
            rageDelayCooldown = Mathf.Max(0, rageDelayCooldown - Time.deltaTime);
            if (rageDelayCooldown > 0) return;
            AddRage(rageRecoveryRate * Time.deltaTime);
        }
    }
}