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
        private const float maxExperience = 10000;

        private NetworkVariable<float> experience = new NetworkVariable<float>();

        public float MaxHPBonus { get { return Level * 4; } }
        public float MaxStaminaBonus { get { return Level * 4; } }
        public float MaxSpiritBonus { get { return Level * 4; } }

        public float BaseDamageBonus { get { return Level / 10; } }

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
                combatAgent.AddSpirit(MaxSpiritBonus);
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

        public void RedeemEssenceBuff(int essenceBuffIndex)
        {
            if (Essences < GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].requiredEssenceCount) { return; }

            if (IsServer)
            {
                essences.Value -= GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].requiredEssenceCount;

                // TODO perform the action here
                switch (GameModeManager.Singleton.EssenceBuffOptions[essenceBuffIndex].title)
                {
                    case "Heal The Ancient":
                        Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
                        if (structures.Length > 0)
                        {
                            Structure structure = structures[0];
                            structure.StatusAgent.TryAddStatus(ActionClip.Status.healing, 0.1f, buffDuration, 0, false);
                        }
                        break;
                    case "Rage":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.AddRage(attributes.GetMaxRage(), false);
                            attributes.OnActivateRage();
                        }
                        break;
                    case "Increased Move Speed":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.StatusAgent.TryAddStatus(ActionClip.Status.movementSpeedIncrease, 0.2f, buffDuration, 0, false);
                        }
                        break;
                    case "Resist Ailments":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.StatusAgent.TryAddStatus(ActionClip.Status.immuneToAilments, 0, buffDuration, 0, false);
                        }
                        break;
                    case "Resist Statuses":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.StatusAgent.TryAddStatus(ActionClip.Status.immuneToNegativeStatuses, 0, buffDuration, 0, false);
                        }
                        break;
                    case "Damage Resistance":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.StatusAgent.TryAddStatus(ActionClip.Status.damageReductionMultiplier, 0.7f, buffDuration, 0, false);
                        }
                        break;
                    case "Health Regeneration":
                        foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
                        {
                            attributes.StatusAgent.TryAddStatus(ActionClip.Status.healing, 0.1f, buffDuration, 0, false);
                        }
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

        [SerializeField] private VisualEffect levelUpVisualEffect;
        [SerializeField] private AudioClip levelUpAudioClip;

        public int SkillPoints { get { return skillPoints.Value; } }
        private NetworkVariable<int> skillPoints = new NetworkVariable<int>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private void OnExperienceChanged(float prev, float current)
        {
            if (CalculateLevel(prev) < CalculateLevel(current))
            {
                if (combatAgent.GetAilment() != ActionClip.Ailment.Death)
                {
                    levelUpVisualEffect.Play();
                    AudioManager.Singleton.PlayClipOnTransform(transform, levelUpAudioClip, false, 0.5f);
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
                return 0;
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
        }

        private void UpgradeAbilityLocally(string weaponName, string abilityName)
        {
            if (IsServer)
            {
                if (skillPoints.Value == 0) { return; }
                skillPoints.Value--;
            }

            var key = (weaponName.Replace("(Clone)", ""), abilityName);
            if (abilityLevelTracker.ContainsKey(key))
            {
                abilityLevelTracker[key]++;
            }
            else
            {
                abilityLevelTracker.Add(key, 1);
            }

            combatAgent.WeaponHandler.GetWeapon().PermanentlyReduceAbilityCooldownTime(combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(abilityName),
                GetAbilityLevelCooldownReduction(weaponName, abilityName));
        }

        public void SyncAbilityCooldowns(Weapon weapon)
        {
            foreach (ActionClip ability in weapon.GetAbilities())
            {
                weapon.PermanentlyReduceAbilityCooldownTime(ability, GetAbilityLevelCooldownReduction(weapon.name, ability.name));
            }
        }

        [Rpc(SendTo.Server)]
        private void UpgradeAbilityServerRpc(string weaponName, string abilityName)
        {
            UpgradeAbilityLocally(weaponName, abilityName);
            UpgradeAbilityOwnerRpc(weaponName, abilityName);
        }

        [Rpc(SendTo.Owner)]
        private void UpgradeAbilityOwnerRpc(string weaponName, string abilityName)
        {
            UpgradeAbilityLocally(weaponName, abilityName);
        }
    }
}