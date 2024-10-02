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
        private void OnExperienceChanged(float prev, float current)
        {
            if (CalculateLevel(prev) < CalculateLevel(current))
            {
                if (combatAgent.GetAilment() != ActionClip.Ailment.Death)
                {
                    levelUpVisualEffect.Play();
                    AudioManager.Singleton.PlayClipOnTransform(transform, levelUpAudioClip, false, 0.5f);
                }
            }
        }

        private CombatAgent combatAgent;
        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
        }
    }
}