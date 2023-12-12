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
        private CharacterReference.WeaponOption tertiaryWeapon;

        private WeaponHandler weaponHandler;
        private void Awake()
        {
            weaponHandler = GetComponent<WeaponHandler>();
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            primaryWeapon = weaponOptions[0];
            secondaryWeapon = weaponOptions[1];
            tertiaryWeapon = weaponOptions[2];
        }

        public void RefreshCurrentWeapon()
        {
            weaponHandler.SetNewWeapon(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions()[0]);
        }

        void OnWeapon1()
        {
            weaponHandler.SetNewWeapon(primaryWeapon);
        }

        void OnWeapon2()
        {
            weaponHandler.SetNewWeapon(secondaryWeapon);
        }

        void OnWeapon3()
        {
            weaponHandler.SetNewWeapon(tertiaryWeapon);
        }
    }
}