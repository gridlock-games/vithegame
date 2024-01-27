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
        public enum WeaponSlotType
        {
            Primary,
            Secondary
        }

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
            if (!IsSpawned)
            {
                EquipPrimaryWeapon();
            }
        }

        public override void OnNetworkSpawn()
        {
            currentEquippedWeapon.OnValueChanged += OnCurrentEquippedWeaponChange;

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());

            CharacterReference.WeaponOption primaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == playerData.character.GetActiveLoadout().weapon1ItemId).itemId : playerData.character.GetActiveLoadout().weapon1ItemId));
            CharacterReference.WeaponOption secondaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == playerData.character.GetActiveLoadout().weapon2ItemId).itemId : playerData.character.GetActiveLoadout().weapon2ItemId));

            primaryWeapon = Instantiate(primaryOption.weapon);
            secondaryWeapon = Instantiate(secondaryOption.weapon);
            primaryRuntimeAnimatorController = primaryOption.animationController;
            secondaryRuntimeAnimatorController = secondaryOption.animationController;
            EquipPrimaryWeapon();

            StartCoroutine(ApplyEquipmentFromLoadout(playerData.character.raceAndGender, playerData.character.GetActiveLoadout()));
        }

        public IEnumerator ApplyDefaultEquipment(CharacterReference.RaceAndGender raceAndGender)
        {
            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions();
            //animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.wearableEquipmentPrefab.name.Contains("Pants_Peasant"))); // Peasant pants

            WebRequestManager.Loadout loadout = WebRequestManager.Singleton.GetDefaultLoadout();
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.helmGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.shouldersGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.chestArmorGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.glovesGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.beltGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.robeGearItemId), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.bootsGearItemId), raceAndGender);
        }

        public void ChangeWeapon(WeaponSlotType weaponSlotType, CharacterReference.WeaponOption weaponOption)
        {
            if (!CanSwapWeapons()) { Debug.LogError("Can't swap weapons right now!"); }

            switch (weaponSlotType)
            {
                case WeaponSlotType.Primary:
                    primaryWeapon = Instantiate(weaponOption.weapon);
                    primaryRuntimeAnimatorController = weaponOption.animationController;
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                case WeaponSlotType.Secondary:
                    secondaryWeapon = Instantiate(weaponOption.weapon);
                    secondaryRuntimeAnimatorController = weaponOption.animationController;
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                default:
                    Debug.LogError("Not sure what weapon slot to swap " + weaponSlotType);
                    break;
            }
        }

        private IEnumerator ApplyEquipmentFromLoadout(CharacterReference.RaceAndGender raceAndGender, WebRequestManager.Loadout loadout)
        {
            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions();

            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.helmGearItemId).itemId : loadout.helmGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.shouldersGearItemId).itemId : loadout.shouldersGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.chestArmorGearItemId).itemId : loadout.chestArmorGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.glovesGearItemId).itemId : loadout.glovesGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.beltGearItemId).itemId : loadout.beltGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.robeGearItemId).itemId : loadout.robeGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems.Find(item => item.id == loadout.bootsGearItemId).itemId : loadout.bootsGearItemId.ToString())), raceAndGender);
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

        public bool CanSwapWeapons()
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