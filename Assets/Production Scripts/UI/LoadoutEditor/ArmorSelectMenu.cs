using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class ArmorSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private WeaponOptionElement weaponOptionPrefab;

        private List<Button> buttonList = new List<Button>();
        private int playerDataId;
        private LoadoutManager loadoutManager;
        public void Initialize(CharacterReference.EquipmentType equipmentType, LoadoutManager loadoutManager, int loadoutSlot, int playerDataId)
        {
            this.playerDataId = playerDataId;
            this.loadoutManager = loadoutManager;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions();
            CharacterReference.WearableEquipmentOption initialWearableEquipmentOption = null;

            switch (equipmentType)
            {
                case CharacterReference.EquipmentType.Belt:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().beltGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Boots:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().bootsGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Cape:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().capeGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Chest:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().chestArmorGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Gloves:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().glovesGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Helm:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().helmGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Pants:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().pantsGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Robe:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().robeGearItemId.ToString()).itemId);
                    break;
                case CharacterReference.EquipmentType.Shoulders:
                    initialWearableEquipmentOption = wearableEquipmentOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetActiveLoadout().shouldersGearItemId.ToString()).itemId);
                    break;
                default:
                    Debug.LogError("Unsure how to handle equipment type " + equipmentType);
                    return;
            }

            if (initialWearableEquipmentOption == null)
            {
                Debug.LogError("Unable to find initial wearable equipment option for armor editor menu");
                return;
            }

            Button invokeThis = null;
            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in wearableEquipmentOptions)
            {
                if (wearableEquipmentOption.equipmentType != equipmentType) { continue; }

                WeaponOptionElement ele = Instantiate(weaponOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<WeaponOptionElement>();
                ele.Initialize(wearableEquipmentOption);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeArmor(button, wearableEquipmentOption, loadoutSlot); });

                if (wearableEquipmentOption.itemWebId == initialWearableEquipmentOption.itemWebId) { invokeThis = button; }

                buttonList.Add(button);
            }

            invokeThis.onClick.Invoke();
        }

        private void ChangeArmor(Button button, CharacterReference.WearableEquipmentOption wearableEquipmentOption, int loadoutSlot)
        {
            foreach (Button b in buttonList)
            {
                b.interactable = true;
            }
            button.interactable = false;

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            WebRequestManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);
            string inventoryItemId = WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.itemId == wearableEquipmentOption.itemWebId).id;
            switch (wearableEquipmentOption.equipmentType)
            {
                case CharacterReference.EquipmentType.Belt:
                    newLoadout.beltGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Boots:
                    newLoadout.bootsGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Cape:
                    newLoadout.capeGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Chest:
                    newLoadout.chestArmorGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Gloves:
                    newLoadout.glovesGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Helm:
                    newLoadout.helmGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Pants:
                    newLoadout.pantsGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Robe:
                    newLoadout.robeGearItemId = inventoryItemId;
                    break;
                case CharacterReference.EquipmentType.Shoulders:
                    newLoadout.shouldersGearItemId = inventoryItemId;
                    break;
            }

            if (armorPreviewObject) { Destroy(armorPreviewObject); }
            //if (wearableEquipmentOption.armorPreviewPrefab) { armorPreviewObject = Instantiate(wearableEquipmentOption.armorPreviewPrefab); }

            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(playerData.character._id.ToString(), newLoadout));
            loadoutManager.StartCoroutine(loadoutManager.ApplyEquipmentFromLoadout(playerData.character.raceAndGender, newLoadout));

            playerData.character = playerData.character.ChangeLoadoutFromSlot(loadoutSlot, newLoadout);
            PlayerDataManager.Singleton.SetPlayerData(playerData);
        }

        private GameObject armorPreviewObject;
        private void OnDestroy()
        {
            Destroy(armorPreviewObject);
        }
    }
}