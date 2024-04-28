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
            if (weapon == primaryWeaponInstance) { return primaryAmmo.Value; }
            if (weapon == secondaryWeaponInstance) { return secondaryAmmo.Value; }
            Debug.LogError("Unknown weapon to get ammo count " + weapon);
            return 0;
        }

        public void Reload(Weapon weapon)
        {
            if (weapon == primaryWeaponInstance) { primaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            if (weapon == secondaryWeaponInstance) { secondaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            Debug.LogError("Unknown weapon to reload " + weapon);
        }

        public void UseAmmo(Weapon weapon)
        {
            if (weapon == primaryWeaponInstance) { primaryAmmo.Value--; return; }
            if (weapon == secondaryWeaponInstance) { secondaryAmmo.Value--; return; }
            Debug.LogError("Unknown weapon to fire " + weapon);
        }

        public CharacterReference.WeaponOption PrimaryWeaponOption { get; private set; }
        private Weapon primaryWeaponInstance;
        private NetworkVariable<int> primaryAmmo = new NetworkVariable<int>();

        public CharacterReference.WeaponOption SecondaryWeaponOption { get; private set; }
        private Weapon secondaryWeaponInstance;
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

        public override void OnNetworkSpawn()
        {
            currentEquippedWeapon.OnValueChanged += OnCurrentEquippedWeaponChange;

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            ApplyLoadout(playerData.character.raceAndGender, playerData.character.GetActiveLoadout(), playerData.character._id.ToString());
        }

        private Coroutine applyLoadoutCoroutine;
        public void ApplyLoadout(CharacterReference.RaceAndGender raceAndGender, WebRequestManager.Loadout loadout, string characterId, bool waitForDeath = false)
        {
            if (applyLoadoutCoroutine != null) { StopCoroutine(applyLoadoutCoroutine); }
            applyLoadoutCoroutine = StartCoroutine(ApplyLoadoutCoroutine(raceAndGender, loadout, characterId, waitForDeath));
        }

        public IEnumerator ApplyLoadoutCoroutine(CharacterReference.RaceAndGender raceAndGender, WebRequestManager.Loadout loadout, string characterId, bool waitForDeath)
        {
            if (waitForDeath)
            {
                yield return new WaitUntil(() => attributes.GetAilment() == ActionClip.Ailment.Death);
                yield return new WaitUntil(() => attributes.GetAilment() == ActionClip.Ailment.None);
            }

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            if (!string.IsNullOrWhiteSpace(characterId) & !WebRequestManager.Singleton.InventoryItems.ContainsKey(characterId)) { yield return WebRequestManager.Singleton.GetCharacterInventory(characterId); }
            if (WebRequestManager.Singleton.InventoryItems.ContainsKey(characterId)) { PrimaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[characterId].Find(item => item.id == loadout.weapon1ItemId.ToString()).itemId); }
            if (PrimaryWeaponOption == null) { PrimaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon1ItemId.ToString()); }

            if (!string.IsNullOrWhiteSpace(characterId) & !WebRequestManager.Singleton.InventoryItems.ContainsKey(characterId)) { yield return WebRequestManager.Singleton.GetCharacterInventory(characterId); }
            if (WebRequestManager.Singleton.InventoryItems.ContainsKey(characterId)) { SecondaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[characterId].Find(item => item.id == loadout.weapon2ItemId.ToString()).itemId); }
            if (SecondaryWeaponOption == null) { SecondaryWeaponOption = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon2ItemId.ToString()); }

            primaryWeaponInstance = Instantiate(PrimaryWeaponOption.weapon);
            secondaryWeaponInstance = Instantiate(SecondaryWeaponOption.weapon);

            if (IsServer)
            {
                primaryAmmo.Value = primaryWeaponInstance.ShouldUseAmmo() ? primaryWeaponInstance.GetMaxAmmoCount() : 0;
                secondaryAmmo.Value = secondaryWeaponInstance.ShouldUseAmmo() ? secondaryWeaponInstance.GetMaxAmmoCount() : 0;
            }

            OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);

            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);

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
                    weaponHandler.SetNewWeapon(primaryWeaponInstance, PrimaryWeaponOption.animationController);
                    weaponHandler.SetStowedWeapon(secondaryWeaponInstance);
                    break;
                case 2:
                    weaponHandler.SetNewWeapon(secondaryWeaponInstance, SecondaryWeaponOption.animationController);
                    weaponHandler.SetStowedWeapon(primaryWeaponInstance);
                    break;
                default:
                    Debug.LogError(current + " not assigned to a weapon");
                    break;
            }
        }

        public bool CanSwapWeapons()
        {
            if (attributes.GetAilment() != ActionClip.Ailment.None) { return false; }
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
                ActionClip flashAttack = primaryWeaponInstance.GetFlashAttack();
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
                ActionClip flashAttack = secondaryWeaponInstance.GetFlashAttack();
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

        public void SwitchWeapon()
        {
            if (currentEquippedWeapon.Value == 1)
            {
                OnWeapon2();
            }
            else if (currentEquippedWeapon.Value == 2)
            {
                OnWeapon1();
            }
            else
            {
                Debug.LogError("Unsure how to handle current equipped weapon value of - " + currentEquippedWeapon.Value);
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
                ActionClip flashAttack = secondaryWeaponInstance.GetFlashAttack();
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
                ActionClip flashAttack = primaryWeaponInstance.GetFlashAttack();
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
                yield return new WaitUntil(() => weaponHandler.GetWeapon() == primaryWeaponInstance);
            }
            else if (weaponSlotToSwapTo == 2)
            {
                yield return new WaitUntil(() => weaponHandler.GetWeapon() == secondaryWeaponInstance);
            }
            weaponHandler.PlayFlashAttack();
        }
    }
}