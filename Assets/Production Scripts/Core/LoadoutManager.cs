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
        private CharacterReference.WeaponOption primaryWeapon;
        private CharacterReference.WeaponOption secondaryWeapon;

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private void Awake()
        {
            attributes = GetComponent<Attributes>();
            weaponHandler = GetComponent<WeaponHandler>();
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            primaryWeapon = weaponOptions[playerData.primaryWeaponIndex];
            secondaryWeapon = weaponOptions[playerData.secondaryWeaponIndex];
        }

        public void RefreshCurrentWeapon()
        {
            weaponHandler.SetNewWeapon(primaryWeapon);
        }

        void OnWeapon1()
        {
            weaponHandler.SetNewWeapon(primaryWeapon);
        }

        void OnWeapon2()
        {
            weaponHandler.SetNewWeapon(secondaryWeapon);
        }
    }
}