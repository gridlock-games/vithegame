using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;
using Vi.Utility;
using Vi.ScriptableObjects;
using Vi.Core.GameModeManagers;
using Vi.Core.CombatAgents;
using Vi.Core.Structures;
using UnityEngine.Events;

namespace Vi.Core
{
    public class SessionProgressionHandler : NetworkBehaviour
    {
        public float TotalExperience { get { return experience.Value; } }

        public float ExperienceAsPercentTowardsNextLevel { get { return experience.Value % experienceRequiredToReachNextLevel / 100; } }

        public string DisplayLevel { get { return (CalculateLevel(experience.Value) + 1).ToString(); } }

        public int Level { get { return CalculateLevel(experience.Value); } }

        private int CalculateLevel(float experience) { return Mathf.FloorToInt(experience / experienceRequiredToReachNextLevel); }

        private const float experienceRequiredToReachNextLevel = 100;
        private const float maxExperience = 99800;

        private NetworkVariable<float> experience = new NetworkVariable<float>();

        public float MaxHPBonus { get { return Level * 4; } }
        public float MaxStaminaBonus { get { return Level * 4; } }
        public float MaxArmorBonus { get { return Level * 4; } }

        public float BaseDamageBonus { get { return Level; } }

        public void AddExperience(float experienceToAdd)
        {
            if (!IsSpawned) { Debug.LogError("SessionProgressionHandler.AddExperience should only be called when spawned!"); return; }
            if (!IsServer) { Debug.LogError("SessionProgressionHandler.AddExperience should only be called on the server!"); return; }

            if (mob) { return; }

            int prevLevel = Level;
            // Cap at level 100
            if (experience.Value + experienceToAdd > maxExperience)
            {
                experience.Value = maxExperience;
            }
            else
            {
                experience.Value += experienceToAdd;
            }

            if (Level != prevLevel)
            {
                combatAgent.AddHP(MaxHPBonus);
                combatAgent.AddStamina(MaxStaminaBonus);
                combatAgent.AddPhysicalArmor(MaxArmorBonus);
                combatAgent.AddMagicalArmor(MaxArmorBonus);
            }
        }

        public void AddEssence()
        {
            if (!IsSpawned) { Debug.LogError("SessionProgressionHandler.AddEssence should only be called when spawned!"); return; }
            if (!IsServer) { Debug.LogError("SessionProgressionHandler.AddEssence should only be called on the server!"); return; }

            if (mob) { return; }

            essences.Value++;
        }

        public int Essences { get { return essences.Value; } }
        private NetworkVariable<int> essences = new NetworkVariable<int>();

        private static Dictionary<SessionProgressionHandler, List<int>> essenceBuffStatusTracker = new Dictionary<SessionProgressionHandler, List<int>>();

        private void RegisterEssenceBuffStatus(ActionClip.StatusPayload statusPayload)
        {
            if (essenceBuffStatusTracker.ContainsKey(this))
            {
                (bool, int) tuple = combatAgent.StatusAgent.AddConditionalStatus(statusPayload);
                if (tuple.Item1)
                {
                    essenceBuffStatusTracker[this].Add(tuple.Item2);
                }
            }
            else
            {
                (bool, int) tuple = combatAgent.StatusAgent.AddConditionalStatus(statusPayload);
                if (tuple.Item1)
                {
                    essenceBuffStatusTracker.Add(this, new List<int>() { tuple.Item2 });
                }
            }
        }

        public static void RemoveAllEssenceBuffStatuses()
        {
            foreach (KeyValuePair<SessionProgressionHandler, List<int>> kvp in essenceBuffStatusTracker)
            {
                foreach (int statusId in kvp.Value)
                {
                    kvp.Key.combatAgent.StatusAgent.RemoveConditionalStatus(statusId);
                }
            }
            essenceBuffStatusTracker.Clear();
        }

        public void RedeemEssenceBuff(int essenceBuffIndex)
        {
            if (Essences < GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].requiredEssenceCount) { return; }

            if (IsServer)
            {
                essences.Value -= GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].requiredEssenceCount;

                switch (GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].title)
                {
                    case "Heal The Ancient":
                        Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
                        if (structures.Length > 0)
                        {
                            Structure structure = structures[0];
                            structure.StatusAgent.TryAddStatus(new ActionClip.StatusPayload(ActionClip.Status.healing, 1, false, buffDuration, 0, false));
                        }
                        break;
                    case "Rage":
                        combatAgent.AddRage(combatAgent.GetMaxRage(), false);
                        combatAgent.OnActivateRage();
                        break;
                    case "Increased Move Speed":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.movementSpeedIncrease, 0.2f, true, buffDuration, 0, false));
                        break;
                    case "Resist Ailments":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.immuneToAilments, 0, false, buffDuration, 0, false));
                        break;
                    case "Resist Statuses":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.immuneToNegativeStatuses, 0, false, buffDuration, 0, false));
                        break;
                    case "Damage Resistance":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.damageReductionMultiplier, 0.7f, false, buffDuration, 0, false));
                        break;
                    case "Health Regeneration":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.healing, 0.1f, false, buffDuration, 0, false));
                        break;
                    case "Increased Attack Speed":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.attackSpeedIncrease, 0.2f, true, buffDuration, 0, false));
                        break;
                    case "Increased Damage":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.damageMultiplier, 1.2f, false, buffDuration, 0, false));
                        break;
                    case "Stamina Regeneration":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.staminaRegeneration, 0.1f, true, buffDuration, 0, false));
                        break;
                    case "Armor Regeneration":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.physicalArmorRegeneration, 0.1f, true, buffDuration, 0, false));
                        break;
                    case "Ability Cooldown Reduction":
                        RegisterEssenceBuffStatus(new ActionClip.StatusPayload(ActionClip.Status.abilityCooldownDecrease, 0.2f, true, buffDuration, 0, false));
                        break;
                    default:
                        Debug.LogError("Unsure how to handle essence buff option titled: " + GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].title);
                        break;
                }
            }
            else if (IsOwner)
            {
                RedeemEssenceBuffRpc(essenceBuffIndex);
            }
            else
            {
                Debug.LogError("RedeemEssenceBuff should only be called on the server or from the owner!");
            }
        }

        private const float buffDuration = 60;

        [Rpc(SendTo.Server)]
        private void RedeemEssenceBuffRpc(int essenceBuffIndex)
        {
            RedeemEssenceBuff(essenceBuffIndex);
        }

        public override void OnNetworkSpawn()
        {
            experience.OnValueChanged += OnExperienceChanged;
        }

        public override void OnNetworkDespawn()
        {
            experience.OnValueChanged -= OnExperienceChanged;
        }

        public VisualEffect LevelUpVisualEffect { get { return levelUpVisualEffect; } }
        [SerializeField] private VisualEffect levelUpVisualEffect;
        [SerializeField] private AudioClip[] levelUpAudioClips;

        private NetworkVariable<int> skillPoints = new NetworkVariable<int>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private void OnExperienceChanged(float prev, float current)
        {
            if (CalculateLevel(prev) < CalculateLevel(current))
            {
                if (combatAgent.GetAilment() != ActionClip.Ailment.Death)
                {
                    levelUpVisualEffect.Play();
                    AudioManager.Singleton.PlayClipOnTransform(transform, levelUpAudioClips[Random.Range(0, levelUpAudioClips.Length)], false, 0.5f);
                }

                if (IsServer)
                {
                    skillPoints.Value++;
                    if (!NetworkObject.IsPlayerObject)
                    {
                        //UpgradeAbility(abilityAttackTypes[lastAbilityIndexUpgraded]);
                        //if (lastAbilityIndexUpgraded > abilityAttackTypes.Length) { lastAbilityIndexUpgraded = 0; }
                    }
                }
            }
        }

        private CombatAgent combatAgent;
        private Mob mob;
        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();

            if (combatAgent is Mob mob)
            {
                this.mob = mob;
            }
        }

        private float GetAbilityLevelCooldownReduction(string weaponName, string abilityName)
        {
            if (mob) { return 0; }
            if (!GameModeManager.Singleton) { return 0; }
            if (!GameModeManager.Singleton.LevelingEnabled) { return 0; }

            var key = (weaponName.Replace("(Clone)", ""), abilityName);
            if (abilityLevelTracker.ContainsKey(key))
            {
                return abilityLevelTracker[key] / (float)10;
            }
            else
            {
                return 0;
            }
        }

        public int GetAbilityLevel(Weapon weapon, ActionClip ability)
        {
            if (mob) { return 1; }

            var key = (weapon.name.Replace("(Clone)", ""), ability.name);
            if (abilityLevelTracker.ContainsKey(key))
            {
                return abilityLevelTracker[key];
            }
            else
            {
                return -1;
            }
        }

        public void UpgradeAbility(Weapon weapon, ActionClip ability)
        {
            if (!IsSpawned) { Debug.LogError("Calling SessionProgressionHandler.UpgradeAbility() when not spawned!"); return; }
            if (skillPoints.Value == 0) { return; }
            if (mob) { return; }

            if (IsServer)
            {
                UpgradeAbilityLocally(weapon.name, ability.name);
            }
            else if (IsOwner)
            {
                UpgradeAbilityServerRpc(weapon.name, ability.name);
            }
            else
            {
                Debug.LogError("Calling UpgradeAbility() when not the owner and not the server!");
            }
        }

        private Dictionary<(string, string), int> abilityLevelTracker = new Dictionary<(string, string), int>();

        private void OnDisable()
        {
            abilityLevelTracker.Clear();
            essenceBuffStatusTracker.Remove(this);
        }

        private bool UpgradeAbilityLocally(string weaponName, string abilityName)
        {
            if (IsServer)
            {
                if (skillPoints.Value == 0) { return false; }
                skillPoints.Value--;
            }

            var key = (weaponName.Replace("(Clone)", ""), abilityName);
            if (abilityLevelTracker.ContainsKey(key))
            {
                abilityLevelTracker[key]++;
            }
            else
            {
                abilityLevelTracker.Add(key, 0);
            }

            int newAbilityLevel = abilityLevelTracker[key];

            ActionClip ability = combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(abilityName);
            combatAgent.WeaponHandler.GetWeapon().PermanentlyReduceAbilityCooldownTime(ability,
                GetAbilityLevelCooldownReduction(weaponName, abilityName));

            OnAbilityUpgrade?.Invoke(ability, newAbilityLevel);

            return true;
        }

        public UnityAction<ActionClip, int> OnAbilityUpgrade;

        public void SyncAbilityCooldowns(Weapon weapon)
        {
            foreach (ActionClip ability in weapon.GetAbilities())
            {
                weapon.PermanentlyReduceAbilityCooldownTime(ability, GetAbilityLevelCooldownReduction(weapon.name, ability.name));
            }
        }

        public bool CanUpgradeAbility(ActionClip ability, Weapon weapon)
        {
            return skillPoints.Value > 0 & weapon.GetAbilityOffsetDifference(ability) > 0;
        }

        [Rpc(SendTo.Server)]
        private void UpgradeAbilityServerRpc(string weaponName, string abilityName)
        {
            if (UpgradeAbilityLocally(weaponName, abilityName))
            {
                UpgradeAbilityOwnerRpc(weaponName, abilityName);
            }
        }

        [Rpc(SendTo.Owner)]
        private void UpgradeAbilityOwnerRpc(string weaponName, string abilityName)
        {
            UpgradeAbilityLocally(weaponName, abilityName);
        }
    }
}