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

        public Slider healthSlider;
        public Slider defenseSlider;
        public Slider poiseSlider;
        public Image weaponImageFill;

        // INITIALIZERS: --------------------------------------------------------------------------

        private void Start()
        {
            melee = GetComponentInParent<CharacterMelee>();
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
            if (melee.currentShield) defenseSlider.value = melee.Defense.Value / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.Poise / melee.maxPoise.GetValue(gameObject);
        }

        /*
        * Update image of currently equipped Weapon
        */
        private void UpdateWeaponUI() {
            if (!melee) { return; }
            if (!melee.currentWeapon) { return; }
            if (!melee.currentWeapon.weaponImage) { return; }
            weaponImageFill.sprite = melee.currentWeapon.weaponImage;
        }
    }
}