using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    [RequireComponent(typeof(WeaponHandler))]
    public class LoadoutManager : NetworkBehaviour
    {
        private Weapon primaryWeapon;
        private RuntimeAnimatorController primaryRuntimeAnimatorController;
        private Weapon secondaryWeapon;
        private RuntimeAnimatorController secondaryRuntimeAnimatorController;

        private NetworkVariable<int> currentEquippedWeapon = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
            weaponHandler = GetComponent<WeaponHandler>();
        }

        private void Start()
        {
            if (!IsSpawned) { EquipPrimaryWeapon(); }
        }

        public override void OnNetworkSpawn()
        {
            currentEquippedWeapon.OnValueChanged += OnCurrentEquippedWeaponChange;

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());

            CharacterReference.WeaponOption primaryOption = System.Array.Find(weaponOptions, item => item.weapon.name == playerData.character.loadoutPreset1.weapon1ItemId);
            CharacterReference.WeaponOption secondaryOption = System.Array.Find(weaponOptions, item => item.weapon.name == playerData.character.loadoutPreset1.weapon2ItemId);

            primaryWeapon = Instantiate(primaryOption.weapon);
            secondaryWeapon = Instantiate(secondaryOption.weapon);
            primaryRuntimeAnimatorController = primaryOption.animationController;
            secondaryRuntimeAnimatorController = secondaryOption.animationController;
            EquipPrimaryWeapon();
        }

        public override void OnNetworkDespawn()
        {
            currentEquippedWeapon.OnValueChanged -= OnCurrentEquippedWeaponChange;
        }

        private void OnCurrentEquippedWeaponChange(int prev, int current)
        {
            switch (current)
            {
                case 1:
                    weaponHandler.SetNewWeapon(primaryWeapon, primaryRuntimeAnimatorController);
                    break;
                case 2:
                    weaponHandler.SetNewWeapon(secondaryWeapon, secondaryRuntimeAnimatorController);
                    break;
                default:
                    Debug.LogError(current + " not assigned to a weapon");
                    break;
            }
        }

        private void EquipPrimaryWeapon()
        {
            if (primaryWeapon)
            {
                weaponHandler.SetNewWeapon(primaryWeapon, primaryRuntimeAnimatorController);
            }
            else
            {
                CharacterReference.WeaponOption weaponOption = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions()[0];
                weaponHandler.SetNewWeapon(Instantiate(weaponOption.weapon), weaponOption.animationController);
            }
        }

        private bool CanSwapWeapons()
        {
            if (weaponHandler.IsAiming()) { return false; }
            if (animationHandler.IsAiming()) { return false; }
            if (!animationHandler.IsAtRest()) { return false; }
            return true;
        }

        void OnWeapon1()
        {
            if (!CanSwapWeapons()) { return; }
            currentEquippedWeapon.Value = 1;
        }

        void OnWeapon2()
        {
            if (!CanSwapWeapons()) { return; }
            currentEquippedWeapon.Value = 2;
        }
    }
}