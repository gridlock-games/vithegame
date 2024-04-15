using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Unity.Collections;

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

            StartCoroutine(InstantiateWeaponsOnSpawn());
        }

        private IEnumerator InstantiateWeaponsOnSpawn()
        {
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());

            yield return WebRequestManager.Singleton.GetCharacterInventory(playerData.character._id.ToString());

            PrimaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().weapon1ItemId).itemId : playerData.character.GetActiveLoadout().weapon1ItemId));
            SecondaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == (NetworkObject.IsPlayerObject ? WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().weapon2ItemId).itemId : playerData.character.GetActiveLoadout().weapon2ItemId));

            primaryWeapon = Instantiate(PrimaryWeaponOption.weapon);
            secondaryWeapon = Instantiate(SecondaryWeaponOption.weapon);
            primaryRuntimeAnimatorController = PrimaryWeaponOption.animationController;
            secondaryRuntimeAnimatorController = SecondaryWeaponOption.animationController;

            if (IsServer)
            {
                primaryAmmo.Value = primaryWeapon.ShouldUseAmmo() ? primaryWeapon.GetMaxAmmoCount() : 0;
                secondaryAmmo.Value = secondaryWeapon.ShouldUseAmmo() ? secondaryWeapon.GetMaxAmmoCount() : 0;
            }

            OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);

            StartCoroutine(ApplyEquipmentFromLoadout(playerData.character.raceAndGender, playerData.character.GetActiveLoadout(), playerData.character._id.ToString()));
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
                    if (IsServer) { primaryAmmo.Value = 0; }
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                case WeaponSlotType.Secondary:
                    SecondaryWeaponOption = weaponOption;
                    secondaryWeapon = Instantiate(weaponOption.weapon);
                    secondaryRuntimeAnimatorController = weaponOption.animationController;
                    if (IsServer) { secondaryAmmo.Value = 0; }
                    OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);
                    break;
                default:
                    Debug.LogError("Not sure what weapon slot to swap " + weaponSlotType);
                    break;
            }
        }

        public IEnumerator ApplyEquipmentFromLoadout(CharacterReference.RaceAndGender raceAndGender, WebRequestManager.Loadout loadout, string characterId)
        {
            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions();

            foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString32Bytes> kvp in loadout.GetLoadoutArmorPiecesAsDictionary())
            {
                if (!NetworkObject.IsSpawned) // This would happen if it's a preview object
                {
                    CharacterReference.WearableEquipmentOption wearableEquipmentOption = null;
                    if (WebRequestManager.Singleton.InventoryItems.ContainsKey(characterId)) { wearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[characterId].Find(item => item.id == kvp.Value.ToString()).itemId); }
                    if (wearableEquipmentOption == null) { wearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == kvp.Value.ToString()); }
                    animationHandler.ApplyWearableEquipment(kvp.Key, wearableEquipmentOption, raceAndGender);
                }
                else if (NetworkObject.IsPlayerObject)
                {
                    animationHandler.ApplyWearableEquipment(kvp.Key, wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[characterId].Find(item => item.id == kvp.Value.ToString()).itemId), raceAndGender);
                }
                else
                {
                    animationHandler.ApplyWearableEquipment(kvp.Key, wearableEquipmentOptions.Find(item => item.itemWebId == kvp.Value.ToString()), raceAndGender);
                }
            }
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
                    weaponHandler.SetStowedWeapon(secondaryWeapon);
                    break;
                case 2:
                    weaponHandler.SetNewWeapon(secondaryWeapon, secondaryRuntimeAnimatorController);
                    weaponHandler.SetStowedWeapon(primaryWeapon);
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
                weaponHandler.SetStowedWeapon(secondaryWeapon);
            }
            else
            {
                CharacterReference.WeaponOption weaponOption = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions()[0];
                weaponHandler.SetNewWeapon(Instantiate(weaponOption.weapon), weaponOption.animationController);
                weaponHandler.SetStowedWeapon(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions()[1].weapon);
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

        private float lastFlashAttackTime = Mathf.NegativeInfinity;
        private const float flashAttackCooldown = 3;
        void OnWeapon1()
        {
            if (currentEquippedWeapon.Value == 1) { return; }

            if (CanSwapWeapons())
            {
                currentEquippedWeapon.Value = 1;
            }
            else
            {
                ActionClip flashAttack = primaryWeapon.GetFlashAttack();
                if (flashAttack)
                {
                    if (weaponHandler.CanActivateFlashSwitch() & Time.time - lastFlashAttackTime > flashAttackCooldown)
                    {
                        if (flashAttack.agentStaminaCost > attributes.GetStamina()) { return; }
                        if (flashAttack.agentDefenseCost > attributes.GetDefense()) { return; }
                        if (flashAttack.agentRageCost > attributes.GetRage()) { return; }
                        FlashAttackServerRpc(1);
                        currentEquippedWeapon.Value = 1;
                        lastFlashAttackTime = Time.time;
                    }
                }
            }
        }

        void OnWeapon2()
        {
            if (currentEquippedWeapon.Value == 2) { return; }

            if (CanSwapWeapons())
            {
                currentEquippedWeapon.Value = 2;
            }
            else
            {
                ActionClip flashAttack = secondaryWeapon.GetFlashAttack();
                if (flashAttack)
                {
                    if (weaponHandler.CanActivateFlashSwitch() & Time.time - lastFlashAttackTime > flashAttackCooldown)
                    {
                        if (flashAttack.agentStaminaCost > attributes.GetStamina()) { return; }
                        if (flashAttack.agentDefenseCost > attributes.GetDefense()) { return; }
                        if (flashAttack.agentRageCost > attributes.GetRage()) { return; }
                        FlashAttackServerRpc(2);
                        currentEquippedWeapon.Value = 2;
                        lastFlashAttackTime = Time.time;
                    }
                }
            }
        }

        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!IsLocalPlayer) { return; }
            if (!weaponHandler.WeaponInitialized) { return; }

            if (currentEquippedWeapon.Value == 1)
            {
                bool canFlashAttack = false;
                ActionClip flashAttack = secondaryWeapon.GetFlashAttack();
                if (flashAttack)
                {
                    if (weaponHandler.CanActivateFlashSwitch() & Time.time - lastFlashAttackTime > flashAttackCooldown)
                    {
                        if (flashAttack.agentStaminaCost > attributes.GetStamina())
                            canFlashAttack = false;
                        else if (flashAttack.agentDefenseCost > attributes.GetDefense())
                            canFlashAttack = false;
                        else if (flashAttack.agentRageCost > attributes.GetRage())
                            canFlashAttack = false;
                        else
                            canFlashAttack = true;
                    }
                }
                attributes.GlowRenderer.RenderFlashAttack(canFlashAttack);
            }
            else if (currentEquippedWeapon.Value == 2)
            {
                bool canFlashAttack = false;
                ActionClip flashAttack = primaryWeapon.GetFlashAttack();
                if (flashAttack)
                {
                    if (weaponHandler.CanActivateFlashSwitch() & Time.time - lastFlashAttackTime > flashAttackCooldown)
                    {
                        if (flashAttack.agentStaminaCost > attributes.GetStamina())
                            canFlashAttack = false;
                        else if (flashAttack.agentDefenseCost > attributes.GetDefense())
                            canFlashAttack = false;
                        else if (flashAttack.agentRageCost > attributes.GetRage())
                            canFlashAttack = false;
                        else
                            canFlashAttack = true;
                    }
                }
                attributes.GlowRenderer.RenderFlashAttack(canFlashAttack);
            }
        }

        [ServerRpc]
        private void FlashAttackServerRpc(int weaponSlotToSwapTo)
        {
            StartCoroutine(WaitForWeaponSwapThenFlashAttack(weaponSlotToSwapTo));
        }

        private IEnumerator WaitForWeaponSwapThenFlashAttack(int weaponSlotToSwapTo)
        {
            if (weaponSlotToSwapTo == 1)
            {
                yield return new WaitUntil(() => weaponHandler.GetWeapon() == primaryWeapon);
            }
            else if (weaponSlotToSwapTo == 2)
            {
                yield return new WaitUntil(() => weaponHandler.GetWeapon() == secondaryWeapon);
            }
            weaponHandler.PlayFlashAttack();
        }
    }
}