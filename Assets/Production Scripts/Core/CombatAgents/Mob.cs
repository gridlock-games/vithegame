using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using Vi.ScriptableObjects;
using Vi.Core.GameModeManagers;
using Vi.Core.VFX;
using Vi.Core.Weapons;

namespace Vi.Core.CombatAgents
{
    public class Mob : CombatAgent
    {
        private NetworkVariable<PlayerDataManager.Team> team = new NetworkVariable<PlayerDataManager.Team>();

        public override void SetMaster(CombatAgent master)
        {
            base.SetMaster(master);
            SetTeam(master.GetTeam());
        }

        public void SetTeam(PlayerDataManager.Team team) { this.team.Value = team; }

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

        public override Weapon.ArmorType GetArmorType() { return armorType; }
        public CharacterReference.WeaponOption GetWeaponOption() { return weaponOption; }
        public override PlayerDataManager.Team GetTeam() { return team.Value; }
        public override string GetName() { return name.Replace("(Clone)", ""); }

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