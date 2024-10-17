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

        [SerializeField] private CharacterReference.WeaponOption weaponOption;
        [SerializeField] private List<ActionClip.Ailment> whitelistedAilments = new List<ActionClip.Ailment>()
        {
            ActionClip.Ailment.None,
            ActionClip.Ailment.Death
        };

        public CharacterReference.WeaponOption GetWeaponOption() { return weaponOption; }

        public override PlayerDataManager.Team GetTeam() { return team.Value; }

        public override string GetName() { return name.Replace("(Clone)", ""); }

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