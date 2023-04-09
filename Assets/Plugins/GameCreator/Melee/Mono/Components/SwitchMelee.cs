using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCreator.Melee
{
    public class SwitchMelee : MonoBehaviour
    {
        public WeaponMeleeSO weaponMeleeSO;
        [SerializeField] private CharacterMelee characterMelee;
        [SerializeField] private List<MeleeWeapon> meleeWeapons = new List<MeleeWeapon>();
        private BladeComponent currentBladeComponent;

        private void Start()
        {
            SetWeaponCollection();

            characterMelee = GetComponent<CharacterMelee>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                SwitchWeaponToHammer();
            }
        }

        void SwitchWeaponToHammer()
        {
            UnequipWeapon();
            characterMelee.currentWeapon = meleeWeapons.FirstOrDefault(x => x.meleeWeaponName == "Sword");
            Debug.Log(characterMelee.currentWeapon.meleeWeaponName);
            characterMelee.currentWeapon.EquipWeapon(characterMelee.CharacterAnimator);
        }

        void UnequipWeapon()
        {
            characterMelee.currentWeapon = meleeWeapons.FirstOrDefault(x => x.meleeWeaponName == characterMelee.currentWeapon.meleeWeaponName);
            currentBladeComponent = characterMelee.Blade;
            Destroy(currentBladeComponent.gameObject);
        }

        void SetWeaponCollection()
        {
            foreach (var weapon in weaponMeleeSO.weaponCollections)
            {
                meleeWeapons.Add(weapon.meleeWeapon);
            }
        }
    }
}