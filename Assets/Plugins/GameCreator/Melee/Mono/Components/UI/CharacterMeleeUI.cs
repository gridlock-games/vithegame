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

        [Header("Health")]
        public float currentHP = 100;
        public Slider healthSlider;

        [Header("Poise")]
        public Image poiseImageFill;
        public Text poiseTextCurrentValue;
        public Text poiseTextMaxValue;

        [Header("Defense")]
        public Image defenseImageFill;
        public Text defenseTextCurrentValue;
        public Text defenseTextMaxValue;

        [Header("WeaponUI")]
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
            float healthPercent = currentHP / melee.maxHealth;
            healthSlider.value = healthPercent;

            float maxPoise = this.melee.maxPoise.GetValue(melee.gameObject);
            float percentPoise = this.melee.Poise / maxPoise;

            if (this.poiseImageFill) this.poiseImageFill.fillAmount = percentPoise;
            if (this.poiseTextCurrentValue) this.poiseTextCurrentValue.text = this.melee.Poise.ToString("0.00");
            if (this.poiseTextMaxValue) this.poiseTextMaxValue.text = maxPoise.ToString("0.00");

            float maxDefense = this.melee.currentShield
                ? this.melee.currentShield.maxDefense.GetValue(melee.gameObject)
                : 0f;

            float percentDefense = this.melee.Defense / maxDefense;

            if (this.defenseImageFill) this.defenseImageFill.fillAmount = percentDefense;
            if (this.defenseTextCurrentValue) this.defenseTextCurrentValue.text = this.melee.Defense.ToString("0.00");
            if (this.defenseTextMaxValue) this.defenseTextMaxValue.text = maxDefense.ToString("0.00");
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