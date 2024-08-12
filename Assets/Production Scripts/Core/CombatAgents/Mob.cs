using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;

namespace Vi.Core.CombatAgents
{
    public class Mob : CombatAgent
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public void SetTeam(PlayerDataManager.Team team)
        {
            this.team.Value = team;
        }

        [SerializeField] private float maxHP = 100;

        public override float GetMaxHP() { return maxHP; }
        public override float GetMaxStamina() { return 100; }
        public override float GetMaxSpirit() { return 100; }
        public override float GetMaxRage() { return 100; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                HP.Value = maxHP;
            }
        }

        public override PlayerDataManager.Team GetTeam() { return team.Value; }

        public override string GetName() { return name.Replace("(Clone)", ""); }

        public override Color GetRelativeTeamColor()
        {
            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.GetTeamColor(GetTeam());
            }
            else
            {
                return localTeam == GetTeam() ? Color.cyan : Color.red;
            }
        }

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Mob.ProcessMeleeHit() should only be called on the server!"); return false; }

            return ProcessHit(true, attacker, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
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
                //animationHandler.PlayAction(weaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingCombatAgent)
                {
                    //SetKiller((Attributes)lastAttackingCombatAgent);
                    //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill((Attributes)lastAttackingCombatAgent, this); }
                }
                else
                {
                    //killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                    //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            RenderHitGlowOnly();
            AddHP(damage);
            return true;
        }

        public override bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject)
        {
            if (!IsServer) { Debug.LogError("Mob.ProcessEnvironmentDamageWithHitReaction() should only be called on the server!"); return false; }
            if (ailment.Value == ActionClip.Ailment.Death) { return false; }

            ActionClip.Ailment attackAilment = ActionClip.Ailment.None;
            if (HP.Value + damage <= 0 & ailment.Value != ActionClip.Ailment.Death)
            {
                attackAilment = ActionClip.Ailment.Death;
                ailment.Value = ActionClip.Ailment.Death;
                AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetDeathReaction());

                if (lastAttackingCombatAgent)
                {
                    //SetKiller((Attributes)lastAttackingCombatAgent);
                    //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill((Attributes)lastAttackingCombatAgent, this); }
                }
                else
                {
                    //killerNetObjId.Value = attackingNetworkObject.NetworkObjectId;
                    //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnEnvironmentKill(this); }
                }
            }
            else
            {
                ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReactionByDirection(Weapon.HitLocation.Front);
                AnimationHandler.PlayAction(hitReaction);
            }

            RenderHit(attackingNetworkObject.NetworkObjectId, transform.position, armorType, Weapon.WeaponBone.Root, attackAilment);
            AddHP(damage);
            return true;
        }

        private bool ProcessHit(bool isMeleeHit, CombatAgent attackerCombatAgent, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
        {
            if (isMeleeHit)
            {
                if (!runtimeWeapon) { Debug.LogError("When processing a melee hit, you need to pass in a runtime weapon!"); return false; }
            }

            if (GetAilment() == ActionClip.Ailment.Death | attackerCombatAgent.GetAilment() == ActionClip.Ailment.Death) { return false; }

            if (!PlayerDataManager.Singleton.CanHit(attackerCombatAgent, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
                }
                return false;
            }

            if (attack.maxHitLimit == 0) { return false; }

            if (IsInvincible()) { return false; }

            if (isMeleeHit)
            {
                if (attackerCombatAgent.wasStaggeredThisFrame) { Debug.Log(attackerCombatAgent + " was staggered"); return false; }

                if (!IsUninterruptable())
                {
                    wasStaggeredThisFrame = true;
                    StartCoroutine(ResetStaggerBool());
                }
            }

            (bool applyAilmentRegardless, ActionClip.Ailment attackAilment) = GetAttackAilment(attack, hitCounter);

            if (IsUninterruptable()) { attackAilment = ActionClip.Ailment.None; }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, WeaponHandler.IsBlocking, attackAilment, ailment.Value);
            hitReaction.SetHitReactionRootMotionMultipliers(attack);

            float HPDamage = -attack.damage;
            HPDamage *= attackerCombatAgent.StatusAgent.DamageMultiplier;
            HPDamage *= damageMultiplier;

            bool shouldPlayHitReaction = false;
            if (attackerCombatAgent is Attributes attacker)
            {
                if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
                {
                    HPDamage *= attacker.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                    if (attack.canEnhance & attacker.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                    {
                        HPDamage *= attack.enhancedChargeDamageMultiplier;
                    }
                }

                if (attacker.AnimationHandler.IsCharging()) { shouldPlayHitReaction = true; }

                attacker.AddHitToComboCounter();
            }
            else if (attackerCombatAgent is Mob mob)
            {

            }
            else
            {
                Debug.LogError("Unsure how to handle subclass type of combat agent! " + attackerCombatAgent);
            }

            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                //hitReaction = weaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, false, attackAilment, ailment.Value);
            }

            bool hitReactionWasPlayed = false;
            if (!IsUninterruptable() | hitReaction.ailment == ActionClip.Ailment.Death)
            {
                if (hitReaction.ailment != ActionClip.Ailment.None)
                {
                    if (attack.shouldPlayHitReaction
                        | ailment.Value != ActionClip.Ailment.None
                        | shouldPlayHitReaction)
                    {
                        if (hitReaction.ailment != ActionClip.Ailment.None)
                        {
                            //animationHandler.PlayAction(hitReaction);
                            hitReactionWasPlayed = true;
                        }
                    }
                }
            }

            if (runtimeWeapon) { runtimeWeapon.AddHit(this); }

            StartHitStop(attackerCombatAgent, isMeleeHit);

            if (hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                RenderBlock(impactPosition, runtimeWeapon ? runtimeWeapon.GetWeaponMaterial() : Weapon.WeaponMaterial.Metal);
                float prevHP = GetHP();
                AddHP(HPDamage);
                //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                //AddDamageToMapping(attacker, prevHP - GetHP());
            }
            else // Not blocking
            {
                if (!Mathf.Approximately(HPDamage, 0))
                {
                    RenderHit(attackerCombatAgent.NetworkObjectId, impactPosition, armorType, runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root, attackAilment);
                    float prevHP = GetHP();
                    AddHP(HPDamage);
                    //if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                    //AddDamageToMapping(attacker, prevHP - GetHP());
                }

                //EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attack, hitReaction);
            }

            if (IsServer)
            {
                foreach (ActionVFX actionVFX in attack.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        WeaponHandler.SpawnActionVFX(WeaponHandler.CurrentActionClip, actionVFX, attackerCombatAgent.transform, transform);
                    }
                }
            }

            //if (!IsUninterruptable())
            //{
            //    if (attack.shouldFlinch | IsRaging())
            //    {
            //        movementHandler.Flinch(attack.GetFlinchAmount());
            //        if (!hitReactionWasPlayed & !IsGrabbed()) { animationHandler.PlayAction(weaponHandler.GetWeapon().GetFlinchClip(attackAngle)); }
            //    }
            //}

            lastAttackingCombatAgent = attackerCombatAgent;
            return true;
        }

        [SerializeField] private PooledObject hitVFXPrefab;
        [SerializeField] private PooledObject blockVFXPrefab;

        protected override PooledObject GetHitVFXPrefab() { return hitVFXPrefab; }
        protected override PooledObject GetBlockVFXPrefab() { return blockVFXPrefab; }

        [SerializeField] private Weapon.ArmorType armorType;

        protected override AudioClip GetHitSoundEffect(Weapon.ArmorType armorType, Weapon.WeaponBone weaponBone, ActionClip.Ailment ailment) { return null; }
        protected override AudioClip GetBlockingHitSoundEffect(Weapon.WeaponMaterial attackingWeaponMaterial) { return null; }
    }
}