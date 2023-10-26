using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class AbilityCard : MonoBehaviour
    {
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image cooldownIcon;
        [SerializeField] private Text keybindText;

        private ActionClip ability;

        public void UpdateCard(ActionClip ability, string keybindText)
        {
            this.ability = ability;
            abilityIcon.sprite = ability.abilityImageIcon;
            this.keybindText.text = keybindText;
        }

        private WeaponHandler weaponHandler;
        private void Start()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
        }

        private void Update()
        {
            if (ability == null) { return; }
            cooldownIcon.fillAmount = 1 - weaponHandler.GetWeapon().GetAbilityCooldownProgress(ability);
        }
    }
}