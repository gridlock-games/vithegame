using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;
using Vi.Utility;
using Vi.ScriptableObjects;

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
        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
        }

        public int GetAbilityLevel(Weapon weapon, ActionClip ability)
        {
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