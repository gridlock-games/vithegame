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

        protected override void Awake()
        {
            base.Awake();
            foreach (ActionClip actionClip in weaponOption.weapon.GetAllActionClips())
            {
                if (!actionClip) { continue; }
                if (actionClip.summonableCount > 0)
                {
                    useArmor = true;
                    break;
                }
            }
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

        private bool useArmor;
        protected override bool ShouldUseArmor() { return useArmor; }

        [SerializeField] private bool useRage;
        protected override bool ShouldUseRage() { return useRage; }

        protected override void OnAilmentChanged(ActionClip.Ailment prev, ActionClip.Ailment current)
        {
            base.OnAilmentChanged(prev, current);

            if (IsServer)
            {
                if (current == ActionClip.Ailment.Death)
                {
                    StartCoroutine(DespawnAfterDuration());
                }
            }
        }

        private IEnumerator DespawnAfterDuration()
        {
            yield return new WaitForSeconds(AnimationHandler.deadRendererDisplayTime);
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                bool isPlayerKiller = false;
                foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                {
                    if (attributes.GetAilment() == ActionClip.Ailment.Death)
                    {
                        if (attributes.TryGetKiller(out NetworkObject killer))
                        {
                            if (killer == NetworkObject) { isPlayerKiller = true; break; }
                        }
                    }
                }

                if (!isPlayerKiller) { break; }

                yield return new WaitForSeconds(0.5f);
            }

            NetworkObject.Despawn(true);
        }
    }
}