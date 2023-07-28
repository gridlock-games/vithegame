namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCreator.Core;
    using GameCreator.Characters;
    using GameCreator.Variables;

    [AddComponentMenu("UI/Game Creator/Character Melee UI", 0)]
    public class CharacterMeleeUI : MonoBehaviour
    {
        // PROPERTIES: ----------------------------------------------------------------------------

        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);
        private CharacterMelee melee;
        private AbilityManager abilityManager;

        private Color lowPoiseColor = new Color(3,0,147);

        private Color normalPoiseColor = new Color(200,200,200);

        public Slider healthSlider;
        public Slider defenseSlider;
        public Slider poiseSlider;
        public Image weaponImageFill;

        public Image abilityAImageFill;

        public Image abilityBImageFill;

        public Image abilityCImageFill;

        // INITIALIZERS: --------------------------------------------------------------------------

        private void Start()
        {
            melee = GetComponentInParent<CharacterMelee>();
            abilityManager = GetComponentInParent<AbilityManager>();
        }

        private void Update()
        {
            this.UpdateUI();
        }

        private void LateUpdate()
        {
            this.UpdateWeaponUI();
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void UpdateUI()
        {
            healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
            if (melee.currentShield) defenseSlider.value = melee.GetDefense() / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.GetPoise() / melee.maxPoise.GetValue(gameObject);
        }

        /*
        * Update image of currently equipped Weapon
        */
        private void UpdateWeaponUI()
        {
            if (!melee) { return; }
            if (!melee.currentWeapon) { return; }
            if (!melee.currentWeapon.weaponImage) { return; }
            weaponImageFill.sprite = melee.currentWeapon.weaponImage;

            foreach (Ability ability in abilityManager.GetAbilityInstanceList())
            {
                switch (ability.skillKey)
                {
                    case KeyCode.Q:
                        abilityAImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityAImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.E:
                        abilityBImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityBImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.R:
                        abilityCImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityCImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                }
            }
        }
    }
}