using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Unity.Collections;
using Vi.Core.CombatAgents;
using UnityEngine.InputSystem;
using Vi.Utility;

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
            if (weapon == primaryWeaponInstance | weapon.name == primaryWeaponInstance.name.Replace("(Clone)", "")) { return primaryAmmo.Value; }
            if (weapon == secondaryWeaponInstance | weapon.name == secondaryWeaponInstance.name.Replace("(Clone)", "")) { return secondaryAmmo.Value; }
            Debug.LogError("Unknown weapon to get ammo count " + weapon);
            return 0;
        }

        public void Reload(Weapon weapon)
        {
            if (!IsServer) { Debug.LogError("LoadoutManager.Reload() should only be called on the server!"); return; }
            if (weapon == primaryWeaponInstance) { primaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            if (weapon == secondaryWeaponInstance) { secondaryAmmo.Value = weapon.GetMaxAmmoCount(); return; }
            Debug.LogError("Unknown weapon to reload " + weapon);
        }

        public void ReloadAllWeapons()
        {
            if (!IsServer) { Debug.LogError("LoadoutManager.ReloadAllWeapons() should only be called on the server!"); return; }
            if (primaryWeaponInstance) { primaryAmmo.Value = primaryWeaponInstance.GetMaxAmmoCount(); }
            if (secondaryWeaponInstance) { secondaryAmmo.Value = secondaryWeaponInstance.GetMaxAmmoCount(); }
        }

        public void UseAmmo(Weapon weapon)
        {
            if (!IsServer) { Debug.LogError("LoadoutManager.UseAmmo() should only be called on the server!"); return; }
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

        public WeaponSlotType GetEquippedSlotType()
        {
            if (currentEquippedWeapon.Value == 1)
            {
                return WeaponSlotType.Primary;
            }
            else if (currentEquippedWeapon.Value == 2)
            {
                return WeaponSlotType.Secondary;
            }
            else
            {
                Debug.LogError("Not sure what slot type corresponds to equipped weapon value of " + currentEquippedWeapon.Value);
                return WeaponSlotType.Primary;
            }
        }

        public Weapon GetWeaponInSlot(LoadoutManager.WeaponSlotType weaponSlotType)
        {
            switch (weaponSlotType)
            {
                case WeaponSlotType.Primary:
                    return primaryWeaponInstance;
                case WeaponSlotType.Secondary:
                    return secondaryWeaponInstance;
                default:
                    Debug.LogError("Unsure how to handle weapon slot type "+ weaponSlotType);
                    return null;
            }
        }

        private CombatAgent combatAgent;
        private AnimationHandler animationHandler;
        private InputAction switchWeaponAction;
        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            combatAgent = GetComponent<CombatAgent>();

            if (TryGetComponent(out PlayerInput playerInput))
            {
                switchWeaponAction = playerInput.actions.FindAction("SwitchWeapon");
            }

            if (TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.OnSpawnFromPool += OnSpawnFromPool;
                pooledObject.OnReturnToPool += OnReturnToPool;
            }
        }

        private void OnSpawnFromPool()
        {
            foreach (CharacterReference.EquipmentType equipmentType in System.Enum.GetValues(typeof(CharacterReference.EquipmentType)))
            {
                equippedEquipment.Add(equipmentType, null);
            }
        }

        private void OnReturnToPool()
        {
            equippedEquipment.Clear();

            PrimaryWeaponOption = default;
            primaryWeaponInstance = default;
            
            SecondaryWeaponOption = default;
            secondaryWeaponInstance = default;

            WeaponNameThatCanFlashAttack = default;
        }

        public override void OnNetworkSpawn()
        {
            currentEquippedWeapon.OnValueChanged += OnCurrentEquippedWeaponChange;

            if (combatAgent is Attributes attributes)
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
                ApplyLoadout(playerData.character.raceAndGender, playerData.character.GetActiveLoadout(), playerData.character._id.ToString());
            }
            else if (combatAgent is Mob mob)
            {
                // Equip weapon here
                CharacterReference.WeaponOption weaponOption = mob.GetWeaponOption();
                PrimaryWeaponOption = weaponOption;
                SecondaryWeaponOption = weaponOption;
                combatAgent.WeaponHandler.SetNewWeapon(Instantiate(weaponOption.weapon), weaponOption.animationController);
            }
            else
            {
                Debug.LogError("Unsure how to handle combat agent sub class! " + combatAgent);
            }
        }

        private Coroutine applyLoadoutCoroutine;
        public void ApplyLoadout(CharacterReference.RaceAndGender raceAndGender, CharacterManager.Loadout loadout, string characterId, bool waitForRespawn = false)
        {
            if (applyLoadoutCoroutine != null) { StopCoroutine(applyLoadoutCoroutine); }
            if (gameObject.activeInHierarchy)
            {
                applyLoadoutCoroutine = StartCoroutine(ApplyLoadoutCoroutine(raceAndGender, loadout, characterId, waitForRespawn));
            }
            else
            {
                Debug.LogError("Trying to apply a loadout to an inactive object " + this);
            }
        }

        private bool canApplyLoadoutThisFrame;
        public void SwapLoadoutOnRespawn()
        {
            if (!IsServer) { Debug.LogError("LoadoutManager.SwapWeaponsOnRespawn() should only be called on the server!"); return; }
            AllowLoadoutSwap();
            SwapWeaponOnRespawnClientRpc();
        }

        [Rpc(SendTo.NotServer)] private void SwapWeaponOnRespawnClientRpc() { AllowLoadoutSwap(); }

        private void AllowLoadoutSwap()
        {
            canApplyLoadoutThisFrame = true;
            if (resetCanApplyLoadoutThisFrameCorountine != null) { StopCoroutine(resetCanApplyLoadoutThisFrameCorountine); }
            resetCanApplyLoadoutThisFrameCorountine = StartCoroutine(ResetCanApplyLoadoutThisFrameBool());
        }

        private Coroutine resetCanApplyLoadoutThisFrameCorountine;
        private IEnumerator ResetCanApplyLoadoutThisFrameBool()
        {
            yield return null;
            canApplyLoadoutThisFrame = false;
        }

        public IEnumerator ApplyLoadoutCoroutine(CharacterReference.RaceAndGender raceAndGender, CharacterManager.Loadout loadout, string characterId, bool waitForRespawn)
        {
            // This will happen when a player hasn't made a loadout in one of its slots yet
            // TODO change this to only modify the loadout's invalid values
            if (!loadout.IsValid())
            {
                loadout = loadout.GetValidCopy(raceAndGender);
            }

            if (waitForRespawn) { yield return new WaitUntil(() => canApplyLoadoutThisFrame); }

            Dictionary<string, CharacterReference.WeaponOption> weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptionsDictionary();

            if (!string.IsNullOrWhiteSpace(characterId))
            {
                if (!CharacterManager.HasCharacterInventory(characterId))
                {
                    yield return WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId);
                }
                else if (IsSpawned) // When spawned, changes to a character's loadout may occur at runtime, therefore we need to refresh the character's inventory if an item doesn't exist on this instance
                {
                    foreach (string inventoryID in loadout.GetLoadoutItemIDsAsArray())
                    {
                        if (string.IsNullOrWhiteSpace(inventoryID)) { continue; }
                        if (!CharacterManager.HasInventoryItem(characterId, inventoryID))
                        {
                            yield return WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId);
                            break;
                        }
                    }
                }
            }
            
            if (CharacterManager.TryGetInventoryItem(characterId, loadout.weapon1ItemId.ToString(), out CharacterManager.InventoryItem weapon1InventoryItem))
            {
                if (weaponOptions.TryGetValue(weapon1InventoryItem.itemId._id, out CharacterReference.WeaponOption weaponOption))
                {
                    PrimaryWeaponOption = weaponOption;
                }
                else
                {
                    Debug.LogWarning("Could not find primary weapon option for inventory id: " + loadout.weapon1ItemId + " for character id: " + characterId);
                }
            }
            else
            {
                if (weaponOptions.TryGetValue(loadout.weapon1ItemId.ToString(), out CharacterReference.WeaponOption weaponOption))
                {
                    PrimaryWeaponOption = weaponOption;
                }
                else
                {
                    Debug.LogWarning("Could not find primary weapon option for generic item id: " + loadout.weapon1ItemId + " for character id: " + characterId);
                }
            }

            if (CharacterManager.TryGetInventoryItem(characterId, loadout.weapon2ItemId.ToString(), out CharacterManager.InventoryItem weapon2InventoryItem))
            {
                if (weaponOptions.TryGetValue(weapon2InventoryItem.itemId._id, out CharacterReference.WeaponOption weaponOption))
                {
                    SecondaryWeaponOption = weaponOption;
                }
                else
                {
                    Debug.LogWarning("Could not find secondary weapon option for inventory id: " + loadout.weapon2ItemId + " for character id: " + characterId);
                }
            }
            else
            {
                if (weaponOptions.TryGetValue(loadout.weapon2ItemId.ToString(), out CharacterReference.WeaponOption weaponOption))
                {
                    SecondaryWeaponOption = weaponOption;
                }
                else
                {
                    Debug.LogWarning("Could not find primary weapon option for generic item id: " + loadout.weapon2ItemId + " for character id: " + characterId);
                }
            }

            primaryWeaponInstance = Instantiate(PrimaryWeaponOption.weapon);
            secondaryWeaponInstance = Instantiate(SecondaryWeaponOption.weapon);

            combatAgent.SessionProgressionHandler.SyncAbilityCooldowns(primaryWeaponInstance);
            combatAgent.SessionProgressionHandler.SyncAbilityCooldowns(secondaryWeaponInstance);

            if (IsServer & IsSpawned)
            {
                primaryAmmo.Value = primaryWeaponInstance.ShouldUseAmmo() ? primaryWeaponInstance.GetMaxAmmoCount() : 0;
                secondaryAmmo.Value = secondaryWeaponInstance.ShouldUseAmmo() ? secondaryWeaponInstance.GetMaxAmmoCount() : 0;
            }

            if (!animationHandler.Animator) { yield return new WaitUntil(() => animationHandler.Animator); }

            OnCurrentEquippedWeaponChange(0, currentEquippedWeapon.Value);

            yield return null;

            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);

            foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString64Bytes> kvp in loadout.GetLoadoutArmorPiecesAsDictionary())
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = null;
                if (CharacterManager.TryGetInventoryItem(characterId, kvp.Value.ToString(), out CharacterManager.InventoryItem equipmentInventoryItem))
                {
                    wearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == equipmentInventoryItem.itemId._id);
                }
                else
                {
                    wearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == kvp.Value.ToString());
                }

                if (wearableEquipmentOption == null & !string.IsNullOrWhiteSpace(kvp.Value.ToString()))
                {
                    Debug.LogWarning("Could not find equipment option for id: " + kvp.Value + " for character id: " + characterId);
                    continue;
                }

                equippedEquipment[kvp.Key] = wearableEquipmentOption;
                animationHandler.ApplyWearableEquipment(kvp.Key, wearableEquipmentOption, raceAndGender);
            }
        }

        private Dictionary<CharacterReference.EquipmentType, CharacterReference.WearableEquipmentOption> equippedEquipment = new Dictionary<CharacterReference.EquipmentType, CharacterReference.WearableEquipmentOption>();

        public CharacterReference.WearableEquipmentOption GetEquippedEquipmentOption(CharacterReference.EquipmentType equipmentType)
        {
            equippedEquipment.TryGetValue(equipmentType, out CharacterReference.WearableEquipmentOption option);
            return option;
        }

        public override void OnNetworkDespawn()
        {
            currentEquippedWeapon.OnValueChanged -= OnCurrentEquippedWeaponChange;

            if (IsLocalPlayer)
            {
                if (combatAgent is Attributes attributes)
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
                    int index = WebRequestManager.Singleton.CharacterManager.Characters.FindIndex(item => item._id == playerData.character._id);
                    if (index == -1)
                    {
                        WebRequestManager.Singleton.CharacterManager.Characters.Add(playerData.character);
                    }
                    else
                    {
                        WebRequestManager.Singleton.CharacterManager.Characters[index] = playerData.character;
                    }
                }
            }
        }

        private void OnCurrentEquippedWeaponChange(int prev, int current)
        {
            switch (current)
            {
                case 1:
                    combatAgent.WeaponHandler.SetNewWeapon(primaryWeaponInstance, PrimaryWeaponOption.animationController);
                    combatAgent.WeaponHandler.SetStowedWeapon(secondaryWeaponInstance);
                    if (IsServer & IsSpawned) { combatAgent.StatusAgent.RemoveAllStatusesAssociatedWithWeapon(); }
                    break;
                case 2:
                    combatAgent.WeaponHandler.SetNewWeapon(secondaryWeaponInstance, SecondaryWeaponOption.animationController);
                    combatAgent.WeaponHandler.SetStowedWeapon(primaryWeaponInstance);
                    if (IsServer & IsSpawned) { combatAgent.StatusAgent.RemoveAllStatusesAssociatedWithWeapon(); }
                    break;
                default:
                    Debug.LogError(current + " not assigned to a weapon");
                    break;
            }
            combatAgent.WeaponHandler.ClearPreviewActionVFXInstances();
        }

        public bool CanSwapWeapons()
        {
            if (combatAgent.GetAilment() != ActionClip.Ailment.None) { return false; }
            if (!animationHandler.IsAtRest()) { return false; }
            if (animationHandler.IsReloading()) { return false; }
            if (animationHandler.WaitingForActionClipToPlay) { return false; }
            return true;
        }

        private float lastPrimaryFlashAttackTime = Mathf.NegativeInfinity;
        private float lastSecondaryFlashAttackTime = Mathf.NegativeInfinity;
        private const float flashAttackCooldown = 5;
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
                    if (combatAgent.WeaponHandler.CanActivateFlashSwitch() & Time.time - lastPrimaryFlashAttackTime > flashAttackCooldown)
                    {
                        if (!animationHandler.AreActionClipRequirementsMet(flashAttack)) { return; }
                        FlashAttackServerRpc(1);
                        currentEquippedWeapon.Value = 1;
                        lastPrimaryFlashAttackTime = Time.time;
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
                    if (combatAgent.WeaponHandler.CanActivateFlashSwitch() & Time.time - lastSecondaryFlashAttackTime > flashAttackCooldown)
                    {
                        if (!animationHandler.AreActionClipRequirementsMet(flashAttack)) { return; }
                        FlashAttackServerRpc(2);
                        currentEquippedWeapon.Value = 2;
                        lastSecondaryFlashAttackTime = Time.time;
                    }
                }
            }
        }

        void OnSwitchWeapon(InputValue value)
        {
            System.Type valueType = switchWeaponAction.activeControl.valueType;
            if (valueType == typeof(float))
            {
                if (!Mathf.Approximately(0, value.Get<float>())) { SwitchWeapon(); }
            }
            else if (valueType == typeof(Vector2))
            {
                if (value.Get<Vector2>() != Vector2.zero) { SwitchWeapon(); }
            }
            else
            {
                Debug.LogError("Unsure how to handle value type! " + valueType);
                SwitchWeapon();
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

        public string WeaponNameThatCanFlashAttack { get; private set; }

        private void Update()
        {
            if (!IsSpawned) { return; }

            if (primaryWeaponInstance)
            {
                primaryWeaponInstance.AbilityCooldownMultiplier = combatAgent.StatusAgent.GetAbilityCooldownMultiplier();
            }

            if (secondaryWeaponInstance)
            {
                secondaryWeaponInstance.AbilityCooldownMultiplier = combatAgent.StatusAgent.GetAbilityCooldownMultiplier();
            }

            if (!IsLocalPlayer) { return; }
            if (!combatAgent.WeaponHandler.WeaponInitialized) { return; }

            if (currentEquippedWeapon.Value == 1)
            {
                bool canFlashAttack = false;
                ActionClip flashAttack = secondaryWeaponInstance.GetFlashAttack();
                if (flashAttack)
                {
                    if (combatAgent.WeaponHandler.CanActivateFlashSwitch() & Time.time - lastSecondaryFlashAttackTime > flashAttackCooldown)
                    {
                        canFlashAttack = animationHandler.AreActionClipRequirementsMet(flashAttack);
                    }
                }
                combatAgent.GlowRenderer.RenderFlashAttack(canFlashAttack);
                WeaponNameThatCanFlashAttack = canFlashAttack ? SecondaryWeaponOption.weapon.name : string.Empty;
            }
            else if (currentEquippedWeapon.Value == 2)
            {
                bool canFlashAttack = false;
                ActionClip flashAttack = primaryWeaponInstance.GetFlashAttack();
                if (flashAttack)
                {
                    if (combatAgent.WeaponHandler.CanActivateFlashSwitch() & Time.time - lastPrimaryFlashAttackTime > flashAttackCooldown)
                    {
                        canFlashAttack = animationHandler.AreActionClipRequirementsMet(flashAttack);
                    }
                }
                combatAgent.GlowRenderer.RenderFlashAttack(canFlashAttack);
                WeaponNameThatCanFlashAttack = canFlashAttack ? PrimaryWeaponOption.weapon.name : string.Empty;
            }
            else
            {
                Debug.LogError("Unsure how to handle current equipped weapon value of " + currentEquippedWeapon.Value);
            }
        }

        [Rpc(SendTo.Server)]
        private void FlashAttackServerRpc(int weaponSlotToSwapTo)
        {
            StartCoroutine(WaitForWeaponSwapThenFlashAttack(weaponSlotToSwapTo));
        }

        private IEnumerator WaitForWeaponSwapThenFlashAttack(int weaponSlotToSwapTo)
        {
            if (weaponSlotToSwapTo == 1)
            {
                yield return new WaitUntil(() => combatAgent.WeaponHandler.GetWeapon() == primaryWeaponInstance);
            }
            else if (weaponSlotToSwapTo == 2)
            {
                yield return new WaitUntil(() => combatAgent.WeaponHandler.GetWeapon() == secondaryWeaponInstance);
            }
            combatAgent.WeaponHandler.PlayFlashAttack();
        }
    }
}