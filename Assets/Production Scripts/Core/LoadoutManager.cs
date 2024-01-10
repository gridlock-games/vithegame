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
        }

        private void Start()
        {
            if (!IsSpawned) { EquipPrimaryWeapon(); }
        }

        public override void OnNetworkSpawn()
        {
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