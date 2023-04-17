using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace GameCreator.Melee
{
    public class SwitchMelee : MonoBehaviour
    {
        [SerializeField] private CharacterMelee _characterMelee;
        [SerializeField] private WeaponMeleeSO _weaponMeleeSO;
        private WeaponType _currentWeaponType;

        [SerializeField] private GameObject _rightHand;
        [SerializeField] private GameObject _leftHand;

        // Map keyboard keys to weapon types
        private readonly Dictionary<KeyCode, WeaponType> _keyToWeaponType = new Dictionary<KeyCode, WeaponType>()
        {
            { KeyCode.Alpha1, WeaponType.GREATSWORD },
            { KeyCode.Alpha2, WeaponType.HAMMER },
            { KeyCode.Alpha3, WeaponType.DAGGER },
            { KeyCode.Alpha4, WeaponType.LANCE },
            { KeyCode.Alpha5, WeaponType.SWORD }
        };

        private void Start()
        {
            Initialize();
        }

        // Initialize the component by getting the CharacterMelee component
        void Initialize()
        {
            _characterMelee = GetComponent<CharacterMelee>();
        }

        private void Update()
        {
            // Only check for keyboard input if a key is currently pressed down
            if (!Input.anyKeyDown) return;

            // Loop through each key in the dictionary and check if it's been pressed down
            foreach (var key in _keyToWeaponType.Keys.Where(key => Input.GetKeyDown(key)))
            {
                // Get the weapon type corresponding to the pressed key
                _currentWeaponType = _keyToWeaponType[key];
                SwitchWeapon();
                break;
            }
        }

        // Switch the character's melee weapon to the one specified by _currentWeaponType
        void SwitchWeapon()
        {
            //SetupWeaponType();
            
            // Unequip the current weapon before switching
            UnequipWeapon();

            // Find the weapon from the WeaponMeleeSO asset based on the current weapon type
            var weapon = _weaponMeleeSO.weaponCollections.FirstOrDefault(x => x.weaponType == _currentWeaponType);
            if (weapon != null)
            {
                // Equip the new weapon and update the currentWeapon property of CharacterMelee
                _characterMelee.currentWeapon = weapon.meleeWeapon;
                _characterMelee.currentShield = weapon.meleeWeapon.defaultShield;
                
                _characterMelee.TestDraw();

                _characterMelee.currentWeapon?.EquipNewWeapon(_characterMelee.CharacterAnimator);
                Debug.Log($"{_currentWeaponType.ToString()} _currentWeaponType");
            }
        }

        // Destroy the currently equipped weapon blade, if any
        //TO BE REFACTORED ON SHIELD
        void UnequipWeapon()
        {
            var asd = _rightHand.GetComponentInChildren<BladeComponent>();
            if (asd != null)
            {
                Destroy(asd.gameObject);
                Debug.Log($"{asd.gameObject.name} weaponClone");
            }
            
            var qwe = _leftHand.GetComponentInChildren<BladeComponent>();
            if (qwe != null)
            {
                Destroy(qwe.gameObject);
                Debug.Log($"{qwe.gameObject.name} weaponClone");
            }
        }
    }
}