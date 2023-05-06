using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Melee
{
    public class SwitchMelee : NetworkBehaviour
    {
        [SerializeField] private CharacterMelee _characterMelee;
        [SerializeField] private WeaponMeleeSO _weaponMeleeSO;
        [SerializeField] private NetworkVariable<WeaponType> _currentWeaponType = new NetworkVariable<WeaponType>();

        private LimbReferences limbs;

        // Map keyboard keys to weapon types
        private readonly Dictionary<KeyCode, WeaponType> _keyToWeaponType = new Dictionary<KeyCode, WeaponType>()
        {
            { KeyCode.Alpha1, WeaponType.GREATSWORD },
            { KeyCode.Alpha2, WeaponType.HAMMER },
            { KeyCode.Alpha3, WeaponType.DAGGER },
            { KeyCode.Alpha4, WeaponType.LANCE },
            { KeyCode.Alpha5, WeaponType.SWORD }
        };

        public override void OnNetworkSpawn() { _currentWeaponType.OnValueChanged += OnCurrentWeaponTypeChange; SwitchWeapon(); }
        public override void OnNetworkDespawn() { _currentWeaponType.OnValueChanged -= OnCurrentWeaponTypeChange; }
        private void OnCurrentWeaponTypeChange(WeaponType prev, WeaponType current) { SwitchWeapon(); }
        [ServerRpc] private void ChangeWeaponTypeServerRpc(WeaponType weaponType) { _currentWeaponType.Value = weaponType; }

        private void Awake()
        {
            _characterMelee = GetComponent<CharacterMelee>();
            limbs = GetComponentInChildren<LimbReferences>();
        }

        private void Update()
        {
            // Only check for keyboard input if a key is currently pressed down
            if (!IsLocalPlayer) return;
            if (!Input.anyKeyDown) return;

            // Loop through each key in the dictionary and check if it's been pressed down
            foreach (var key in _keyToWeaponType.Keys.Where(key => Input.GetKeyDown(key)))
            {
                // If there is a melee weapon assigned to this weaponType and the weaponType is present in our scriptable object
                bool weaponIsValid = false;
                foreach (var weaponData in _weaponMeleeSO.weaponCollections)
                {
                    if (weaponData.weaponType == _keyToWeaponType[key])
                    {
                        if (weaponData.meleeWeapon) { weaponIsValid = true; }
                        break;
                    }
                }

                if (weaponIsValid)
                {
                    // Get the weapon type corresponding to the pressed key
                    if (IsServer) { _currentWeaponType.Value = _keyToWeaponType[key]; }
                    else { ChangeWeaponTypeServerRpc(_keyToWeaponType[key]); }
                    // SwitchWeapon() call occurs in OnCurrentWeaponTypeChange() method
                }
                break;
            }
        }

        // Switch the character's melee weapon to the one specified by _currentWeaponType
        void SwitchWeapon()
        {
            if (!limbs) { Debug.LogError("No LimbReferences Component in Children of " + name + ". This object will not be able to switch weapons"); return; }
            //SetupWeaponType();

            // Unequip the current weapon before switching
            UnequipWeapon();

            // Find the weapon from the WeaponMeleeSO asset based on the current weapon type
            var weapon = _weaponMeleeSO.weaponCollections.FirstOrDefault(x => x.weaponType == _currentWeaponType.Value);
            if (weapon != null)
            {
                // Equip the new weapon and update the currentWeapon property of CharacterMelee
                _characterMelee.currentWeapon = weapon.meleeWeapon;
                _characterMelee.currentShield = weapon.meleeWeapon.defaultShield;
                
                _characterMelee.TestDraw();

                // _characterMelee.currentWeapon?.EquipNewWeapon(_characterMelee.CharacterAnimator);
                //Debug.Log($"{_currentWeaponType.Value} _currentWeaponType");
            }
        }

        // Destroy the currently equipped weapon blade, if any
        //TO BE REFACTORED ON SHIELD
        void UnequipWeapon()
        {
            var asd = limbs.rightHand.GetComponentInChildren<BladeComponent>();
            if (asd != null)
            {
                Destroy(asd.gameObject);
                //Debug.Log($"{asd.gameObject.name} weaponClone");
            }
            
            var qwe = limbs.leftHand.GetComponentInChildren<BladeComponent>();
            if (qwe != null)
            {
                Destroy(qwe.gameObject);
                //Debug.Log($"{qwe.gameObject.name} weaponClone");
            }
        }
    }
}