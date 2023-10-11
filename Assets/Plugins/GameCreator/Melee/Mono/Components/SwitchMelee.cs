using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;
using UnityEngine.SceneManagement;

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
            { KeyCode.Alpha3, WeaponType.LANCE },
            { KeyCode.Alpha4, WeaponType.BRAWLER }
        };

        public override void OnNetworkSpawn() { _currentWeaponType.OnValueChanged += OnCurrentWeaponTypeChange; SwitchWeapon(_currentWeaponType.Value); }
        public override void OnNetworkDespawn() { _currentWeaponType.OnValueChanged -= OnCurrentWeaponTypeChange; }

        private void OnCurrentWeaponTypeChange(WeaponType prev, WeaponType current)
        {
            SwitchWeapon(current);
            PlayVFX();
        }

        public void OnModelChange()
        {
            SwitchWeapon(_currentWeaponType.Value);
        }

        [ServerRpc]
        private void ChangeWeaponTypeServerRpc(WeaponType weaponType)
        {
            if (_characterMelee.IsBlocking.Value) return;
            if (_characterMelee.IsStaggered) return;
            if (_characterMelee.IsAttacking) return;
            if (_characterMelee.Character.characterAilment != Characters.CharacterLocomotion.CHARACTER_AILMENTS.None) return;

            _currentWeaponType.Value = weaponType;
        }

        [SerializeField] private VisualEffect[] _switchWeaponVFX;

        public WeaponType GetCurrentWeaponType()
        {
            return _currentWeaponType.Value;
        }

        public void SwitchWeaponBeforeSpawn()
        {
            if (IsSpawned) { Debug.LogError("SwitchWeaponBeforeSpawn() should only be called when the object is not spawned"); return; }
            SwitchWeapon(_currentWeaponType.Value);
        }

        private void Awake()
        {
            _characterMelee = GetComponent<CharacterMelee>();
            limbs = GetComponentInChildren<LimbReferences>();
            _switchWeaponVFX = GetComponentsInChildren<VisualEffect>();
        }

        private void Start()
        {
            if (_switchWeaponVFX != null) { StopVFX(); }
        }

        private void LateUpdate()
        {
            // Only check for keyboard input if a key is currently pressed down
            if (!IsLocalPlayer) return;
            if (!Input.anyKeyDown) return;
            if (_characterMelee.IsBlocking.Value) return;
            if (_characterMelee.IsStaggered) return;
            if (_characterMelee.IsAttacking) return;
            if (_characterMelee.Character.characterAilment != Characters.CharacterLocomotion.CHARACTER_AILMENTS.None) return;

            if (SceneManager.GetActiveScene().name != "Prototype") return;

            // Loop through each key in the dictionary and check if it's been pressed down
            foreach (var key in _keyToWeaponType.Keys.Where(key => Input.GetKeyDown(key)))
            {
                // If there is a melee weapon assigned to this weaponType and the weaponType is present in our scriptable object
                bool weaponIsValid = false;
                foreach (var weaponData in _weaponMeleeSO.weaponCollections)
                {
                    if (weaponData.weaponType == _keyToWeaponType[key])
                    {
                        //_switchWeaponVFX.Play();
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

        void StopVFX()
        {
            foreach (var vfx in _switchWeaponVFX)
            {
                vfx.Stop();
            }
        }

        void PlayVFX()
        {
            foreach (var vfx in _switchWeaponVFX)
            {
                vfx.Play();
            }
        }

        // Switch the character's melee weapon to the one specified by _currentWeaponType
        void SwitchWeapon(WeaponType weaponType)
        {
            if (!limbs) { limbs = GetComponentInChildren<LimbReferences>(); }
            if (!limbs) { Debug.LogError("No LimbReferences Component in Children of " + name + ". This object will not be able to switch weapons"); return; }
            //SetupWeaponType();

            // Unequip the current weapon before switching
            UnequipWeapon();

            // Find the weapon from the WeaponMeleeSO asset based on the current weapon type
            var weapon = _weaponMeleeSO.weaponCollections.FirstOrDefault(x => x.weaponType == weaponType);
            if (weapon != null)
            {
                // Equip the new weapon and update the currentWeapon property of CharacterMelee
                _characterMelee.currentWeapon = weapon.meleeWeapon;
                _characterMelee.currentShield = weapon.meleeWeapon.defaultShield;

                _characterMelee.DrawWeapon();

                //_characterMelee.currentWeapon?.EquipNewWeapon(_characterMelee.CharacterAnimator);
                //Debug.Log($"{_currentWeaponType.Value} _currentWeaponType");
            }
        }

        // Destroy the currently equipped weapon blade, if any
        //TO BE REFACTORED ON SHIELD
        void UnequipWeapon()
        {
            if (!limbs) { limbs = GetComponentInChildren<LimbReferences>(); }
            if (!limbs) { Debug.LogError("No LimbReferences Component in Children of " + name + ". This object will not be able to switch weapons"); return; }

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