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

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
            weaponHandler = GetComponent<WeaponHandler>();
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            primaryWeapon = Instantiate(weaponOptions[playerData.primaryWeaponIndex].weapon);
            secondaryWeapon = Instantiate(weaponOptions[playerData.secondaryWeaponIndex].weapon);
            primaryRuntimeAnimatorController = weaponOptions[playerData.primaryWeaponIndex].animationController;
            secondaryRuntimeAnimatorController = weaponOptions[playerData.secondaryWeaponIndex].animationController;
        }

        public void EquipPrimaryWeapon()
        {
            weaponHandler.SetNewWeapon(primaryWeapon, primaryRuntimeAnimatorController);
        }

        void OnWeapon1()
        {
            if (animationHandler.IsAtRest()) { weaponHandler.SetNewWeapon(primaryWeapon, primaryRuntimeAnimatorController); }
        }

        void OnWeapon2()
        {
            if (animationHandler.IsAtRest()) { weaponHandler.SetNewWeapon(secondaryWeapon, secondaryRuntimeAnimatorController); }
        }
    }
}