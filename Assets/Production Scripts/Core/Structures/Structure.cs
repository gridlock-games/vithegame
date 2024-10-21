using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using Vi.Core.MeshSlicing;

namespace Vi.Core.Structures
{
    public class Structure : HittableAgent
    {
        [SerializeField] private float maxHP = 100;
        [SerializeField] private PlayerDataManager.Team team = PlayerDataManager.Team.Competitor;
        [SerializeField] private AudioClip deathSound;

        public Collider[] Colliders { get; private set; }

        private ExplodableMesh[] explodableMeshes;
        protected override void Awake()
        {
            base.Awake();
            Colliders = GetComponentsInChildren<Collider>();
            explodableMeshes = GetComponentsInChildren<ExplodableMesh>();

            List<Collider> networkPredictionLayerColliders = new List<Collider>();
            foreach (Collider col in Colliders)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("NetworkPrediction"))
                {
                    networkPredictionLayerColliders.Add(col);
                }
            }
            Colliders = networkPredictionLayerColliders.ToArray();
        }

        private List<PooledObject> explodableMeshInstances = new List<PooledObject>();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            HP.OnValueChanged += OnHPChanged;
            if (IsServer)
            {
                HP.Value = maxHP;
            }
            PlayerDataManager.Singleton.AddStructure(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            HP.OnValueChanged -= OnHPChanged;
            PlayerDataManager.Singleton.RemoveStructure(this);
        }

        public override string GetName() { return name.Replace("(Clone)", ""); }
        public override PlayerDataManager.Team GetTeam() { return team; }
        public override float GetMaxHP() { return maxHP; }

        public bool IsDead { get; private set; }
        private void OnHPChanged(float prev, float current)
        {
            if (prev > 0 & Mathf.Approximately(current, 0))
            {
                IsDead = true;

                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    r.forceRenderingOff = true;
                }

                foreach (ExplodableMesh explodableMesh in explodableMeshes)
                {
                    explodableMeshInstances.AddRange(explodableMesh.Explode());
                }

                if (deathSound)
                {
                    AudioManager.Singleton.PlayClipAtPoint(gameObject, deathSound, transform.position, 1);
                }
            }
            else if (prev <= 0 & current > 0)
            {
                IsDead = false;
                foreach (PooledObject explodableMeshInstance in explodableMeshInstances)
                {
                    ObjectPoolingManager.ReturnObjectToPool(explodableMeshInstance);
                }
                explodableMeshInstances.Clear();
            }
        }

        private void OnDisable()
        {
            foreach (PooledObject explodableMeshInstance in explodableMeshInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(explodableMeshInstance);
            }
            explodableMeshInstances.Clear();
        }

        public override Weapon.ArmorType GetArmorType() { return armorType; }
        [SerializeField] private Weapon.ArmorType armorType = Weapon.ArmorType.Metal;
        protected bool ProcessHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!PlayerDataManager.Singleton.CanHit(attacker, this)) { return false; }

            if (!PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
                }
                return false;
            }

            float HPDamage = -attack.damage;
            HPDamage *= attacker.StatusAgent.DamageMultiplier;

            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                HPDamage *= attacker.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (runtimeWeapon) { runtimeWeapon.AddHit(this); }

            RenderHit(attacker.NetworkObjectId, impactPosition, runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root);

            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnStructureKill(attacker, this); }
            }

            AddHP(HPDamage);

            foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTargetOnHit)
            {
                StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
            }

            lastAttackingCombatAgent = attacker;
            return true;
        }

        protected void RenderHit(ulong attackerNetObjId, Vector3 impactPosition, Weapon.WeaponBone weaponBone)
        {
            if (!IsServer) { Debug.LogError("Attributes.RenderHit() should only be called from the server"); return; }

            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetObjId, out NetworkObject attacker))
            {
                if (attacker.TryGetComponent(out CombatAgent attackingCombatAgent))
                {
                    PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(attackingCombatAgent.GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
                    AudioManager.Singleton.PlayClipAtPoint(gameObject,
                        attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ActionClip.Ailment.None),
                        impactPosition, Weapon.hitSoundEffectVolume);

                    RenderHitClientRpc(attackerNetObjId, impactPosition, weaponBone);
                }
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void RenderHitClientRpc(ulong attackerNetObjId, Vector3 impactPosition, Weapon.WeaponBone weaponBone)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetObjId, out NetworkObject attacker))
            {
                if (attacker.TryGetComponent(out CombatAgent attackingCombatAgent))
                {
                    PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(attackingCombatAgent.GetHitVFXPrefab(), impactPosition, Quaternion.identity)));
                    AudioManager.Singleton.PlayClipAtPoint(gameObject,
                        attackingCombatAgent.GetHitSoundEffect(armorType, weaponBone, ActionClip.Ailment.None),
                        impactPosition, Weapon.hitSoundEffectVolume);
                }
            }
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return ProcessHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return ProcessHit(attacker, attack, runtimeWeapon, impactPosition, hitSourcePosition);
        }

        protected CombatAgent lastAttackingCombatAgent;
        public override bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Structure.ProcessEnvironmentDamage() should only be called on the server!"); return false; }
            if (IsDead) { return false; }

            if (HP.Value + damage <= 0 & !IsDead)
            {
                IsDead = true;
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnStructureKill(lastAttackingCombatAgent, this); }
            }
            AddHP(damage);
            return true;
        }

        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Structure.ProcessEnvironmentDamage() should only be called on the server!"); return false; }
            if (IsDead) { return false; }

            if (HP.Value + damage <= 0 & !IsDead)
            {
                IsDead = true;
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnStructureKill(lastAttackingCombatAgent, this); }
            }
            AddHP(damage);
            return true;
        }
    }
}