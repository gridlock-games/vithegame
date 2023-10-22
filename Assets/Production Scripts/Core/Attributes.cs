using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class Attributes : NetworkBehaviour
    {
        [SerializeField] private float maxHP = 100;
        [SerializeField] private float maxDefense = 100;
        [SerializeField] private float maxStamina = 100;
        [SerializeField] private float maxRage = 100;

        private NetworkVariable<float> HP = new NetworkVariable<float>();
        private NetworkVariable<float> defense = new NetworkVariable<float>();
        private NetworkVariable<float> stamina = new NetworkVariable<float>();
        private NetworkVariable<float> rage = new NetworkVariable<float>();

        public float GetHP() { return HP.Value; }
        public float GetDefense() { return defense.Value; }
        public float GetStamina() { return stamina.Value; }
        public float GetRage() { return rage.Value; }

        public override void OnNetworkSpawn()
        {
            HP.Value = maxHP;
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

        public void SetInviniciblity(float duration)
        {
            invincibilityEndTime = Time.time + duration;
        }

        public void ProcessMeleeHit(Attributes attacker, Vector3 impactPosition, ActionClip hitReaction)
        {
            Debug.Log(attacker + " is attacking " + this + " " + hitReaction);
            animationHandler.PlayAction(hitReaction);

            glowRenderer.RenderHit();
        }

        public void ProcessProjectileHit()
        {

        }

        private void Update()
        {
            glowRenderer.RenderInvincible(IsInvincible);
        }
    }
}