using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Player;
using Vi.Utility;
using Unity.Collections;

namespace Vi.UI
{
    public class ArmorSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private LoadoutOptionElement loadoutOptionPrefab;
        //[SerializeField] private Camera characterPreviewCamera;

        //private void Start()
        //{
        //    CreatePreview();

        //    foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
        //    {
        //        data.OnDragEvent += OnCharPreviewDrag;
        //    }
        //}

        //private void OnCharPreviewDrag(Vector2 delta)
        //{
        //    if (previewObject)
        //    {
        //        previewObject.transform.rotation *= Quaternion.Euler(0, -delta.x * 0.25f, 0);
        //    }
        //}

        //private void OnDestroy()
        //{
        //    foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
        //    {
        //        data.OnDragEvent -= OnCharPreviewDrag;
        //    }

        //    if (characterPreviewCamera) { Destroy(characterPreviewCamera.gameObject); }

        //    if (previewObject)
        //    {
        //        if (previewObject.TryGetComponent(out PooledObject pooledObject))
        //        {
        //            ObjectPoolingManager.ReturnObjectToPool(pooledObject);
        //            previewObject = null;
        //        }
        //        else
        //        {
        //            Destroy(previewObject);
        //        }
        //    }
        //}

        //private GameObject previewObject;
        //private void CreatePreview()
        //{
        //    WebRequestManager.Character character = PlayerDataManager.Singleton.LocalPlayerData.character;

        //    var playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(character.raceAndGender);

        //    if (!previewObject)
        //    {
        //        // Instantiate the player model
        //        Vector3 basePos = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetCharacterPreviewPosition(playerDataId);
        //        if (PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.TryGetComponent(out PooledObject pooledObject))
        //        {
        //            previewObject = ObjectPoolingManager.SpawnObject(pooledObject,
        //                basePos,
        //                Quaternion.Euler(SpawnPoints.previewCharacterRotation)).gameObject;
        //        }
        //        else
        //        {
        //            previewObject = Instantiate(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab,
        //                basePos,
        //                Quaternion.Euler(SpawnPoints.previewCharacterRotation));
        //        }

        //        AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();
        //        animationHandler.ChangeCharacter(character);

        //        characterPreviewCamera.transform.SetParent(null);
        //        characterPreviewCamera.transform.position = basePos + SpawnPoints.cameraPreviewCharacterPositionOffset;
        //        characterPreviewCamera.transform.rotation = Quaternion.Euler(SpawnPoints.cameraPreviewCharacterRotation);
        //    }
            
        //    previewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());
        //}

        private List<Button> buttonList = new List<Button>();
        private int playerDataId;
        private List<WebRequestManager.InventoryItem> inventory;
        public void Initialize(CharacterReference.EquipmentType equipmentType, int loadoutSlot, int playerDataId)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(playerData.character.raceAndGender);
            CharacterReference.WearableEquipmentOption initialWearableEquipmentOption = null;

            inventory = WebRequestManager.GetInventory(playerData.character._id.ToString());

            foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString64Bytes> kvp in playerData.character.GetActiveLoadout().GetLoadoutArmorPiecesAsDictionary())
            {
                if (kvp.Key == equipmentType)
                {
                    if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), kvp.Value.ToString(), out WebRequestManager.InventoryItem equipmentInventoryItem))
                    {
                        initialWearableEquipmentOption = WebRequestManager.GetEquipmentOption(equipmentInventoryItem, playerData.character.raceAndGender);
                    }
                    break;
                }
            }

            Button invokeThis = null;
            if (WebRequestManager.NullableEquipmentTypes.Contains(equipmentType))
            {
                LoadoutOptionElement emptyEle = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                emptyEle.InitializeEquipment(null, playerData.character.raceAndGender);
                Button emptyButton = emptyEle.GetComponentInChildren<Button>();
                emptyButton.onClick.AddListener(delegate { ChangeArmor(emptyButton, equipmentType, null,
                    WebRequestManager.InventoryItem.GetEmptyInventoryItem(), loadoutSlot, wearableEquipmentOptions); });
                buttonList.Add(emptyButton);

                invokeThis = emptyButton;
            }
            
            // Create UI buttons for each item in our inventory
            foreach (WebRequestManager.InventoryItem inventoryItem in inventory)
            {
                CharacterReference.WearableEquipmentOption wearableEquipmentOption = WebRequestManager.GetEquipmentOption(inventoryItem, playerData.character.raceAndGender);

                if (wearableEquipmentOption == null) { continue; }
                if (wearableEquipmentOption.equipmentType != equipmentType) { continue; }
                if (wearableEquipmentOption.GetModel(playerData.character.raceAndGender, null) == null) { continue; }

                LoadoutOptionElement ele = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                ele.InitializeEquipment(wearableEquipmentOption, playerData.character.raceAndGender);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate {
                    ChangeArmor(button, equipmentType, wearableEquipmentOption, inventoryItem, loadoutSlot, wearableEquipmentOptions);
                });

                if (initialWearableEquipmentOption != null)
                {
                    if (wearableEquipmentOption.itemWebId == initialWearableEquipmentOption.itemWebId) { invokeThis = button; }
                }

                buttonList.Add(button);
            }

            if (invokeThis)
            {
                invokeThis.onClick.Invoke();
            }
        }

        //private void Update()
        //{
        //    if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame)
        //    {
        //        CreatePreview();
        //    }
        //}

        private void ChangeArmor(Button button, CharacterReference.EquipmentType equipmentType, CharacterReference.WearableEquipmentOption wearableEquipmentOption,
            WebRequestManager.InventoryItem inventoryItem, int loadoutSlot, List<CharacterReference.WearableEquipmentOption> allOptions)
        {
            foreach (Button b in buttonList)
            {
                b.interactable = true;
            }
            button.interactable = false;

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            WebRequestManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            string inventoryItemId = inventoryItem.id;
            switch (equipmentType)
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

                    var pants = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Pants);
                    string pantsId;
                    if (pants == null)
                    {
                        pantsId = "";
                    }
                    else if (inventory.Exists(item => item.itemId._id == pants.itemWebId))
                    {
                        pantsId = inventory.Find(item => item.itemId._id == pants.itemWebId).id;
                        newLoadout.pantsGearItemId = pantsId;
                    }

                    var boots = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Boots);
                    string bootsId;
                    if (boots == null)
                    {
                        bootsId = "";
                    }
                    else if (inventory.Exists(item => item.itemId._id == boots.itemWebId))
                    {
                        bootsId = inventory.Find(item => item.itemId._id == boots.itemWebId).id;
                        newLoadout.bootsGearItemId = bootsId;
                    }

                    var belt = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Belt);
                    string beltId;
                    if (belt == null)
                    {
                        beltId = "";
                    }
                    else if (inventory.Exists(item => item.itemId._id == belt.itemWebId))
                    {
                        beltId = inventory.Find(item => item.itemId._id == belt.itemWebId).id;
                        newLoadout.beltGearItemId = beltId;
                    }

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

            if (!newLoadout.Equals(playerData.character.GetActiveLoadout()))
            {
                PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(playerData.character._id.ToString(), newLoadout));

                playerData.character = playerData.character.ChangeLoadoutFromSlot(loadoutSlot, newLoadout);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
        }
    }
}