using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;
using Vi.Core.GameModeManagers;
using Vi.Core.VFX;

namespace Vi.Core.CombatAgents
{
    public class Mob : CombatAgent
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public void SetTeam(PlayerDataManager.Team team) { this.team.Value = team; }

        public CombatAgent Master { get; private set; }
        public void SetMaster(CombatAgent master) { Master = master; }

        protected override void OnDisable()
        {
            base.OnDisable();
            Master = null;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (GetAilment() == ActionClip.Ailment.Death) { return; }

            if (Master)
            {
                if (Master.IsSpawned)
                {
                    if (Master.GetAilment() == ActionClip.Ailment.Death)
                    {
                        ProcessEnvironmentDamage(-GetMaxHP(), null);
                    }
                }
            }
        }

        [SerializeField] private float maxHP = 100;
        [SerializeField] private CharacterReference.WeaponOption weaponOption;
        [SerializeField] private List<ActionClip.Ailment> whitelistedAilments = new List<ActionClip.Ailment>()
        {
            ActionClip.Ailment.None,
            ActionClip.Ailment.Death
        };

        public CharacterReference.WeaponOption GetWeaponOption() { return weaponOption; }

        public override float GetMaxHP() { return maxHP + SessionProgressionHandler.MaxHPBonus; }
        public override float GetMaxStamina() { return 100 + SessionProgressionHandler.MaxStaminaBonus; }
        public override float GetMaxSpirit() { return 100 + SessionProgressionHandler.MaxSpiritBonus; }
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

        protected override (bool, ActionClip.Ailment) GetAttackAilment(ActionClip attack, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter)
        {
            (bool applyAilmentRegardless, ActionClip.Ailment attackAilment) = base.GetAttackAilment(attack, hitCounter);

            if (!whitelistedAilments.Contains(attackAilment))
            {
                if (attackAilment != ActionClip.Ailment.None & whitelistedAilments.Contains(ActionClip.Ailment.Stun))
                {
                    attackAilment = ActionClip.Ailment.Stun;
                    applyAilmentRegardless = true;
                }
                else
                {
                    attackAilment = ActionClip.Ailment.None;
                }
            }

            return (applyAilmentRegardless, attackAilment);
        }

        private bool ProcessHit(bool isMeleeHit, CombatAgent attacker, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, RuntimeWeapon runtimeWeapon = null, float damageMultiplier = 1)
        {
            if (!CanProcessHit(isMeleeHit, attacker, attack, runtimeWeapon)) { return false; }

            if (!PlayerDataManager.Singleton.CanHit(attacker, this))
            {
                AddHP(attack.healAmount);
                foreach (ActionClip.StatusPayload status in attack.statusesToApplyToTeammateOnHit)
                {
                    StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, false);
                }
                return false;
            }

            if (!CanHit(isMeleeHit, attacker, attack)) { return false; }

            (bool applyAilmentRegardless, ActionClip.Ailment attackAilment) = GetAttackAilment(attack, hitCounter);

            if (IsUninterruptable)
            {
                attackAilment = ActionClip.Ailment.None;
            }
            else
            {
                wasStaggeredThisFrame = true;
                StartCoroutine(ResetStaggerBool());
            }

            if (attackAilment == ActionClip.Ailment.Grab) { hitSourcePosition = attacker.MovementHandler.GetPosition(); }

            if (!attacker.IsRaging) { attacker.AddRage(attackerRageToBeAddedOnHit); }
            if (!IsRaging) { AddRage(victimRageToBeAddedOnHit); }

            float attackAngle = Vector3.SignedAngle(transform.forward, hitSourcePosition - transform.position, Vector3.up);
            ActionClip hitReaction = WeaponHandler.GetWeapon().GetHitReaction(attack, attackAngle, WeaponHandler.IsBlocking, attackAilment, ailment.Value, applyAilmentRegardless);
            hitReaction.SetHitReactionRootMotionMultipliers(attack);

            float HPDamage = -(attack.damage + SessionProgressionHandler.BaseDamageBonus);
            HPDamage *= attacker.StatusAgent.DamageMultiplier;
            HPDamage *= damageMultiplier;

            bool shouldPlayHitReaction = false;
            if (attack.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                HPDamage *= attacker.AnimationHandler.HeavyAttackChargeTime * attack.chargeTimeDamageMultiplier;
                if (attack.canEnhance & attacker.AnimationHandler.HeavyAttackChargeTime > ActionClip.enhanceChargeTime)
                {
                    HPDamage *= attack.enhancedChargeDamageMultiplier;
                }
            }

            if (attacker.AnimationHandler.IsCharging()) { shouldPlayHitReaction = true; }

            if (attacker is Attributes attributes) { attributes.AddHitToComboCounter(); }

            bool hitReactionWasPlayed = false;
            if (AddHPWithoutApply(HPDamage) <= 0)
            {
                attackAilment = ActionClip.Ailment.Death;
                hitReaction = WeaponHandler.GetWeapon().GetDeathReaction();

                if (IsGrabbed)
                {
                    GetGrabAssailant().CancelGrab();
                    CancelGrab();
                }

                AnimationHandler.PlayAction(hitReaction);
                hitReactionWasPlayed = true;
            }
            else if (!IsUninterruptable)
            {
                if (hitReaction.ailment == ActionClip.Ailment.Grab)
                {
                    grabAttackClipName.Value = attack.name;
                    grabAssailantDataId.Value = attacker.NetworkObjectId;
                    attacker.SetGrabVictim(NetworkObjectId);
                    isGrabbed.Value = true;
                    attacker.SetIsGrabbingToTrue();
                    attacker.AnimationHandler.PlayAction(attacker.WeaponHandler.GetWeapon().GetGrabAttackClip(attack));
                }

                if (hitReaction.ailment == ActionClip.Ailment.None)
                {
                    if (!IsGrabbed & !IsRaging)
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
                    RenderHit(attacker.NetworkObjectId, impactPosition, armorType, runtimeWeapon ? runtimeWeapon.WeaponBone : Weapon.WeaponBone.Root, attackAilment);
                    float prevHP = GetHP();
                    AddHP(HPDamage);
                    if (GameModeManager.Singleton) { GameModeManager.Singleton.OnDamageOccuring(attacker, this, prevHP - GetHP()); }
                    AddDamageToMapping(attacker, prevHP - GetHP());
                }

                EvaluateAilment(attackAilment, applyAilmentRegardless, hitSourcePosition, attacker, attack, hitReaction);
            }

            if (IsServer & runtimeWeapon)
            {
                foreach (ActionVFX actionVFX in attack.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType == ActionVFX.VFXSpawnType.OnHit)
                    {
                        if (WeaponHandler.SpawnActionVFX(attack, actionVFX, attacker.transform, transform).TryGetComponent(out ActionVFXParticleSystem actionVFXParticleSystem))
                        {
                            if (!hitCounter.ContainsKey(this))
                            {
                                hitCounter.Add(this, new(1, Time.time));
                            }
                            else
                            {
                                hitCounter[this] = new(hitCounter[this].hitNumber + 1, Time.time);
                            }
                            actionVFXParticleSystem.AddToHitCounter(hitCounter);
                        }
                    }
                }
            }

            if (!IsUninterruptable)
            {
                if (attack.shouldFlinch | IsRaging)
                {
                    MovementHandler.Flinch(attack.GetFlinchAmount());
                    if (!hitReactionWasPlayed & !IsGrabbed) { AnimationHandler.PlayAction(WeaponHandler.GetWeapon().GetFlinchClip(attackAngle)); }
                }
            }

            lastAttackingCombatAgent = attacker;
            return true;
        }

        [SerializeField] private Weapon.ArmorType armorType = Weapon.ArmorType.Flesh;

        [SerializeField] private CharacterReference.RaceAndGender raceAndGender;
        public override CharacterReference.RaceAndGender GetRaceAndGender() { return raceAndGender; }

        // Uncomment to make mobs respawn automatically
        //protected override void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        //{
        //    base.OnAilmentChanged(prev, current);

        //    if (current == ActionClip.Ailment.Death)
        //    {
        //        respawnCoroutine = StartCoroutine(RespawnSelf());
        //    }
        //    else if (prev == ActionClip.Ailment.Death)
        //    {
        //        if (respawnCoroutine != null)
        //        {
        //            IsRespawning = false;
        //            StopCoroutine(respawnCoroutine);
        //        }
        //    }
        //}

        //public bool IsRespawning { get; private set; }
        //[HideInInspector] public bool isWaitingForSpawnPoint;
        //private Coroutine respawnCoroutine;
        //private float respawnSelfCalledTime;
        //private IEnumerator RespawnSelf()
        //{
        //    yield return new WaitForSeconds(5);
        //    ResetStats(1, true, true, false);
        //    AnimationHandler.CancelAllActions(0, true);
        //    MovementHandler.SetOrientation(new Vector3(0, 5, 0), Quaternion.identity);
        //    LoadoutManager.SwapLoadoutOnRespawn();
        //}
    }
}