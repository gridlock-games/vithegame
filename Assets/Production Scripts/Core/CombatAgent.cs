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

        public float GetHP() { return HP.Value; }

        public abstract float GetMaxHP();

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

        public StatusAgent StatusAgent { get; private set; }
        protected void Awake()
        {
            StatusAgent = GetComponent<StatusAgent>();
        }

        [SerializeField] private PooledObject worldSpaceLabelPrefab;
        protected PooledObject worldSpaceLabelInstance;
        public override void OnNetworkSpawn()
        {
            if (!IsLocalPlayer) { worldSpaceLabelInstance = ObjectPoolingManager.SpawnObject(worldSpaceLabelPrefab, transform); }
        }

        public override void OnNetworkDespawn()
        {
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

        public abstract bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition);

        public abstract bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1);

        protected NetworkVariable<ActionClip.Ailment> ailment = new NetworkVariable<ActionClip.Ailment>();
        public ActionClip.Ailment GetAilment() { return ailment.Value; }

        public virtual bool IsInvincible() { return false; }
    }
}
