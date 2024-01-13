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

        [SerializeField] private string[] armorNames;

        private void Start()
        {
            if (!IsSpawned)
            {
                EquipPrimaryWeapon();

                StartCoroutine(Wait());
            }

            foreach (string armorName in armorNames)
            {
                var option = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(CharacterReference.RaceAndGender.HumanMale).FindAll(item => item.wearableEquipmentPrefab.name.Contains(armorName));

                foreach (var c in option)
                {
                    Debug.Log(c.itemWebId + " " + c.wearableEquipmentPrefab.name);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            currentEquippedWeapon.OnValueChanged += OnCurrentEquippedWeaponChange;

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());

            CharacterReference.WeaponOption primaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == playerData.character.loadoutPreset1.weapon1ItemId);
            CharacterReference.WeaponOption secondaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == playerData.character.loadoutPreset1.weapon2ItemId);

            primaryWeapon = Instantiate(primaryOption.weapon);
            secondaryWeapon = Instantiate(secondaryOption.weapon);
            primaryRuntimeAnimatorController = primaryOption.animationController;
            secondaryRuntimeAnimatorController = secondaryOption.animationController;
            EquipPrimaryWeapon();

            StartCoroutine(ApplyEquipmentFromLoadout(playerData.character.loadoutPreset1));
        }

        private IEnumerator Wait()
        {
            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(CharacterReference.RaceAndGender.HumanMale);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == "65a2b5157fd3af802c750fe1"));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == "65a2b4b27fd3af802c750d31"));
        }

        private IEnumerator ApplyEquipmentFromLoadout(WebRequestManager.Loadout loadout)
        {
            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(CharacterReference.RaceAndGender.HumanMale);

            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.helmGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.shouldersGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.chestArmorGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.glovesGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.beltGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.robeGearItemId));
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == loadout.bootsGearItemId));
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