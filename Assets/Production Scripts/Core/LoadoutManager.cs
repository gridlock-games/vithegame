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

        public int GetAmmoCount(Weapon weapon)
        {
            if (weapon == primaryWeapon) { return primaryAmmo.Value; }
            if (weapon == secondaryWeapon) { return secondaryAmmo.Value; }
            Debug.LogError("Unknown weapon to get ammo count " + weapon);
            return 0;
        }

        public void Reload(Weapon weapon)
        {
            if (weapon == primaryWeapon) { primaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            if (weapon == secondaryWeapon) { secondaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            Debug.LogError("Unknown weapon to reload " + weapon);
        }

        public void UseAmmo(Weapon weapon)
        {
            if (weapon == primaryWeapon) { primaryAmmo.Value--; return; }
            if (weapon == secondaryWeapon) { secondaryAmmo.Value--; return; }
            Debug.LogError("Unknown weapon to fire " + weapon);
        }

        public CharacterReference.WeaponOption PrimaryWeaponOption { get; private set; }
        private Weapon primaryWeapon;
        private RuntimeAnimatorController primaryRuntimeAnimatorController;
        private NetworkVariable<int> primaryAmmo = new NetworkVariable<int>();

        public CharacterReference.WeaponOption SecondaryWeaponOption { get; private set; }
        private Weapon secondaryWeapon;
        private RuntimeAnimatorController secondaryRuntimeAnimatorController;
        private NetworkVariable<int> secondaryAmmo = new NetworkVariable<int>();

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

            PrimaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().weapon1ItemId).itemId : playerData.character.GetActiveLoadout().weapon1ItemId));
            SecondaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().weapon2ItemId).itemId : playerData.character.GetActiveLoadout().weapon2ItemId));

            primaryWeapon = Instantiate(PrimaryWeaponOption.weapon);
            secondaryWeapon = Instantiate(SecondaryWeaponOption.weapon);
            primaryRuntimeAnimatorController = PrimaryWeaponOption.animationController;
            secondaryRuntimeAnimatorController = SecondaryWeaponOption.animationController;
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

        public void ChangeWeaponBeforeSpawn(WeaponSlotType weaponSlotType, CharacterReference.WeaponOption weaponOption)
        {
            if (IsSpawned) { Debug.LogError("ChangeWeaponBeforeSpawn() should only be called when an object isn't spawned! Use it for displaying previews"); return; }

            switch (weaponSlotType)
            {
                case WeaponSlotType.Primary:
                    PrimaryWeaponOption = weaponOption;
                    primaryWeapon = Instantiate(weaponOption.weapon);
                    primaryRuntimeAnimatorController = weaponOption.animationController;
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                case WeaponSlotType.Secondary:
                    SecondaryWeaponOption = weaponOption;
                    secondaryWeapon = Instantiate(weaponOption.weapon);
                    secondaryRuntimeAnimatorController = weaponOption.animationController;
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                default:
                    Debug.LogError("Not sure what weapon slot to swap " + weaponSlotType);
                    break;
            }
        }

        public void ChangeWeapon(WeaponSlotType weaponSlotType, string inventoryItemId, bool waitForDeath)
        {
            if (!IsSpawned) { Debug.LogError("ChangeWeapon() should only be called when spawned!"); return; }
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            CharacterReference.WeaponOption weaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == inventoryItemId).itemId);
            
            switch (weaponSlotType)
            {
                case WeaponSlotType.Primary:
                    if (primaryWeapon.name.Replace("(Clone)", "") == weaponOption.weapon.name) { return; }
                    break;
                case WeaponSlotType.Secondary:
                    if (secondaryWeapon.name.Replace("(Clone)", "") == weaponOption.weapon.name) { return; }
                    break;
                default:
                    Debug.LogError("Not sure what weapon slot to swap " + weaponSlotType);
                    break;
            }

            if (IsServer)
            {
                ChangeWeaponOnServer(weaponSlotType, inventoryItemId, waitForDeath);
            }
            else
            {
                ChangeWeaponServerRpc(weaponSlotType, inventoryItemId, waitForDeath);
            }
        }

        [ServerRpc] private void ChangeWeaponServerRpc(WeaponSlotType weaponSlotType, string inventoryItemId, bool waitForDeath) { ChangeWeaponOnServer(weaponSlotType, inventoryItemId, waitForDeath); }

        private void ChangeWeaponOnServer(WeaponSlotType weaponSlotType, string inventoryItemId, bool waitForDeath)
        {
            Coroutine coroutine = StartCoroutine(ChangeWeaponWhenPossible(weaponSlotType, inventoryItemId, waitForDeath));

            if (!changeWeaponCoroutines.ContainsKey(weaponSlotType))
            {
                changeWeaponCoroutines.Add(weaponSlotType, coroutine);
            }
            else
            {
                if (changeWeaponCoroutines[weaponSlotType] != null) { StopCoroutine(changeWeaponCoroutines[weaponSlotType]); }
                changeWeaponCoroutines[weaponSlotType] = coroutine;
            }

            ChangeWeaponClientRpc(weaponSlotType, inventoryItemId, waitForDeath);
        }

        [ClientRpc] private void ChangeWeaponClientRpc(WeaponSlotType weaponSlotType, string inventoryItemId, bool waitForDeath)
        {
            if (!IsServer)
            {
                Coroutine coroutine = StartCoroutine(ChangeWeaponWhenPossible(weaponSlotType, inventoryItemId, waitForDeath));

                if (!changeWeaponCoroutines.ContainsKey(weaponSlotType))
                {
                    changeWeaponCoroutines.Add(weaponSlotType, coroutine);
                }
                else
                {
                    if (changeWeaponCoroutines[weaponSlotType] != null) { StopCoroutine(changeWeaponCoroutines[weaponSlotType]); }
                    changeWeaponCoroutines[weaponSlotType] = coroutine;
                }
            }
        }

        private Dictionary<WeaponSlotType, Coroutine> changeWeaponCoroutines = new Dictionary<WeaponSlotType, Coroutine>();
        private IEnumerator ChangeWeaponWhenPossible(WeaponSlotType weaponSlotType, string inventoryItemId, bool waitForDeath)
        {
            if (waitForDeath)
            {
                yield return new WaitUntil(() => attributes.GetAilment() == ActionClip.Ailment.Death);
                yield return new WaitUntil(() => attributes.GetAilment() == ActionClip.Ailment.None);
            }
            yield return new WaitUntil(() => CanSwapWeapons());
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            CharacterReference.WeaponOption weaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == inventoryItemId).itemId);

            switch (weaponSlotType)
            {
                case WeaponSlotType.Primary:
                    PrimaryWeaponOption = weaponOption;
                    primaryWeapon = Instantiate(weaponOption.weapon);
                    primaryRuntimeAnimatorController = weaponOption.animationController;
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                case WeaponSlotType.Secondary:
                    SecondaryWeaponOption = weaponOption;
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

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.helmGearItemId).itemId : loadout.helmGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.shouldersGearItemId).itemId : loadout.shouldersGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.chestArmorGearItemId).itemId : loadout.chestArmorGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.glovesGearItemId).itemId : loadout.glovesGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.beltGearItemId).itemId : loadout.beltGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.robeGearItemId).itemId : loadout.robeGearItemId.ToString())), raceAndGender);
            animationHandler.ApplyWearableEquipment(wearableEquipmentOptions.Find(item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.bootsGearItemId).itemId : loadout.bootsGearItemId.ToString())), raceAndGender);
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
            if (animationHandler.IsReloading()) { return false; }
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