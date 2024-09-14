using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;
using Vi.Core.GameModeManagers;

namespace Vi.Core.CombatAgents
{
    public class Mob : CombatAgent
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public void SetTeam(PlayerDataManager.Team team) { this.team.Value = team; }

        [SerializeField] private float maxHP = 100;
        [SerializeField] private CharacterReference.WeaponOption weaponOption;
        [SerializeField] private List<ActionClip.Ailment> whitelistedAilments = new List<ActionClip.Ailment>()
        {
            ActionClip.Ailment.None,
            ActionClip.Ailment.Death
        };

        public CharacterReference.WeaponOption GetWeaponOption() { return weaponOption; }

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

        public override bool ProcessMeleeHit(CombatAgent attacker, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            if (!IsServer) { Debug.LogError("Mob.ProcessMeleeHit() should only be called on the server!"); return false; }

            return ProcessHit(true, attacker, attack, impactPosition, hitSourcePosition, runtimeWeapon.GetHitCounter(), runtimeWeapon);
        }

        public override bool ProcessProjectileHit(CombatAgent attacker, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
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
                AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetDeathReaction());

                if (GameModeManager.Singleton)
                {
                    if (lastAttackingCombatAgent)
                    {
                        GameModeManager.Singleton.OnPlayerKill(lastAttackingCombatAgent, this);
                    }
                    else
                    {
                        GameModeManager.Singleton.OnEnvironmentKill(this);
                    }
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

                if (GameModeManager.Singleton)
                {
                    if (lastAttackingCombatAgent)
                    {
                        GameModeManager.Singleton.OnPlayerKill(lastAttackingCombatAgent, this);
                    }
                    else
                    {
                        GameModeManager.Singleton.OnEnvironmentKill(this);
                    }
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

        private bool ProcessHit(bool isMeleeHit, CombatAgent attackerCombatAgent, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
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
            if (!whitelistedAilments.Contains(attack.ailment)) { attackAilment = ActionClip.Ailment.None; }

            if (IsUninterruptable()) { attackAilment = ActionClip.Ailment.None; }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, WeaponHandler.IsBlocking, attackAilment, ailment.Value);
            hitReaction.SetHitReactionRootMotionMultipliers(attack);

            float HPDamage = -attack.damage;
            HPDamage *= attackerCombatAgent.StatusAgent.DamageMultiplier;
            HPDamage *= damageMultiplier;

            bool shouldPlayHitReaction = false;
            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                HPDamage *= attackerCombatAgent.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attackerCombatAgent.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (attackerCombatAgent.AnimationHandler.IsCharging()) { shouldPlayHitReaction = true; }

            if (attackerCombatAgent is Attributes attacker) { attacker.AddHitToComboCounter(); }

            bool hitReactionWasPlayed = false;
            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                hitReaction = WeaponHandler.GetWeapon().GetDeathReaction();

                if (IsGrabbed())
                {
                    GetGrabAssailant().CancelGrab();
                    CancelGrab();
                }

                AnimationHandler.PlayAction(hitReaction);
                hitReactionWasPlayed = true;
            }
            else if (!IsUninterruptable())
            {
                if (hitReaction.ailment == ActionClip.Ailment.Grab)
                {
                    grabAttackClipName.Value = attack.name;
                    grabAssailantDataId.Value = attackerCombatAgent.NetworkObjectId;
                    attackerCombatAgent.SetGrabVictim(NetworkObjectId);
                    isGrabbed.Value = true;

                    Vector3 victimNewPosition = attackerCombatAgent.MovementHandler.GetPosition() + (attackerCombatAgent.transform.forward * 1.2f);
                    MovementHandler.SetOrientation(victimNewPosition, Quaternion.LookRotation(attackerCombatAgent.MovementHandler.GetPosition() - victimNewPosition, Vector3.up));
                    attackerCombatAgent.AnimationHandler.PlayAction(attackerCombatAgent.WeaponHandler.GetWeapon().GetGrabAttackClip(attack));
                }

                if (hitReaction.ailment == ActionClip.Ailment.None)
                {
                    if (!IsGrabbed() & !IsRaging())
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

            StartHitStop(attackerCombatAgent, isMeleeHit);

            if (hitReaction.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                RenderBlock(impactPosition, runtimeWeapon ? runtimeWeapon.GetWeaponMaterial() : Weapon.WeaponMaterial.Metal);
                float prevHP = GetHP();
                AddHP(HPDamage);
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attackerCombatAgent, this, prevHP - GetHP()); }
                AddDamageToMapping(attackerCombatAgent, prevHP - GetHP());
            }
            else // Not blocking
            {
                if (!Mathf.Approximately(HPDamage, 0))
                {
                    RenderHit(attackerCombatAgent.NetworkObjectId, impactPosition, armorType, runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root, attackAilment);
                    float prevHP = GetHP();
                    AddHP(HPDamage);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attackerCombatAgent, this, prevHP - GetHP()); }
                    AddDamageToMapping(attackerCombatAgent, prevHP - GetHP());
                }

                EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attackerCombatAgent, attack, hitReaction);
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

            if (!IsUninterruptable())
            {
                if (attack.shouldFlinch | IsRaging())
                {
                    MovementHandler.Flinch(attack.GetFlinchAmount());
                    if (!hitReactionWasPlayed & !IsGrabbed()) { AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetFlinchClip(attackAngle)); }
                }
            }

            lastAttackingCombatAgent = attackerCombatAgent;
            return true;
        }

        protected override void EvaluateAilment(ActionClip.Ailment attackAilment, bool applyAilmentRegardless, Vector3 hitSourcePosition, CombatAgent attacker, ActionClip attack, ActionClip hitReaction)
        {
            foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTargetOnHit)
            {
                StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
            }

            ailment.Value = attackAilment;

            if (ailment.Value == ActionClip.Ailment.Death)
            {
                if (GameModeManager.Singleton) { GameModeManager.Singleton.OnPlayerKill(attacker, this); }
            }
        }

        [SerializeField] private Weapon.ArmorType armorType = Weapon.ArmorType.Flesh;

        [SerializeField] private CharacterReference.RaceAndGender raceAndGender;
        public override CharacterReference.RaceAndGender GetRaceAndGender() { return raceAndGender; }
    }
}