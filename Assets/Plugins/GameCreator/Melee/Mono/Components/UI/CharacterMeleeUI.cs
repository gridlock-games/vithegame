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
        private Abilities abilities;

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
            abilities = GetComponentInParent<Abilities>();
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
        private void UpdateWeaponUI() {
            if (!melee) { return; }
            if (!melee.currentWeapon) { return; }
            if (!melee.currentWeapon.weaponImage) { return; }
            weaponImageFill.sprite = melee.currentWeapon.weaponImage;

            foreach(Ability ability in abilities.abilities) {
                switch(ability.skillKey) {
                    case KeyCode.Q:
                        abilityAImageFill.sprite = ability.isOnCoolDown == false ? ability.skillImageFill : null;
                        break;
                    case KeyCode.E:
                        abilityBImageFill.sprite = ability.isOnCoolDown == false ? ability.skillImageFill : null;
                        break;
                    case KeyCode.R:
                        abilityCImageFill.sprite = ability.isOnCoolDown == false ? ability.skillImageFill : null;
                        break;
                }
            }


            // if(melee.currentWeapon.abilityA != null) {
            //     abilityAImageFill.sprite = melee.currentWeapon.abilityA.skillImageFill;
            // }
            // if(melee.currentWeapon.abilityB != null) {
            //     abilityBImageFill.sprite = melee.currentWeapon.abilityB.skillImageFill;
            // }
            // if(melee.currentWeapon.abilityC != null) {
            //     abilityCImageFill.sprite = melee.currentWeapon.abilityC.skillImageFill;
            // }
        }
    }
}