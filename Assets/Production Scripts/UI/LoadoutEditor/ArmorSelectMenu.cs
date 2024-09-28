using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Player;
using Vi.Utility;

namespace Vi.UI
{
    public class ArmorSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private LoadoutOptionElement loadoutOptionPrefab;
        [SerializeField] private Camera characterPreviewCamera;

        private void Start()
        {
            CreatePreview();
        }

        private void OnDestroy()
        {
            if (previewObject)
            {
                if (previewObject.TryGetComponent(out PooledObject pooledObject))
                {
                    ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    previewObject = null;
                }
                else
                {
                    Destroy(previewObject);
                }
            }
        }

        private GameObject previewObject;
        private void CreatePreview()
        {
            WebRequestManager.Character character = PlayerDataManager.Singleton.LocalPlayerData.character;

            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            KeyValuePair<int, int> kvp = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptionIndices(character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            if (!previewObject)
            {
                // Instantiate the player model
                Vector3 basePos = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetCharacterPreviewPosition(playerDataId);
                if (playerModelOptionList[characterIndex].playerPrefab.TryGetComponent(out PooledObject pooledObject))
                {
                    previewObject = ObjectPoolingManager.SpawnObject(pooledObject,
                        basePos,
                        Quaternion.Euler(SpawnPoints.previewCharacterRotation)).gameObject;
                }
                else
                {
                    previewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab,
                        basePos,
                        Quaternion.Euler(SpawnPoints.previewCharacterRotation));
                }

                AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();
                animationHandler.ChangeCharacter(character);

                characterPreviewCamera.transform.position = basePos + SpawnPoints.cameraPreviewCharacterPositionOffset;
                characterPreviewCamera.transform.rotation = Quaternion.Euler(SpawnPoints.cameraPreviewCharacterRotation);
            }
            
            previewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());
        }

        private List<Button> buttonList = new List<Button>();
        private int playerDataId;
        public void Initialize(CharacterReference.EquipmentType equipmentType, int loadoutSlot, int playerDataId)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            List<CharacterReference.WearableEquipmentOption> wearableEquipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(playerData.character.raceAndGender);
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

            Button invokeThis = null;
            if (WebRequestManager.NullableEquipmentTypes.Contains(equipmentType))
            {
                LoadoutOptionElement emptyEle = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                emptyEle.InitializeEquipment(null, playerData.character.raceAndGender);
                Button emptyButton = emptyEle.GetComponentInChildren<Button>();
                emptyButton.onClick.AddListener(delegate { ChangeArmor(emptyButton, equipmentType, null, loadoutSlot); });
                buttonList.Add(emptyButton);

                invokeThis = emptyButton;
            }
            
            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in wearableEquipmentOptions)
            {
                if (wearableEquipmentOption.equipmentType != equipmentType) { continue; }
                if (wearableEquipmentOption.GetModel(playerData.character.raceAndGender, null) == null) { continue; }
                if (!WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Exists(item => item.itemId == wearableEquipmentOption.itemWebId)) { continue; }

                LoadoutOptionElement ele = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                ele.InitializeEquipment(wearableEquipmentOption, playerData.character.raceAndGender);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeArmor(button, equipmentType, wearableEquipmentOption, loadoutSlot); });

                if (initialWearableEquipmentOption != null)
                {
                    if (wearableEquipmentOption.itemWebId == initialWearableEquipmentOption.itemWebId) { invokeThis = button; }
                }
                
                buttonList.Add(button);
            }

            invokeThis.onClick.Invoke();
        }

        private void Update()
        {
            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame)
            {
                CreatePreview();
            }
        }

        private void ChangeArmor(Button button, CharacterReference.EquipmentType equipmentType, CharacterReference.WearableEquipmentOption wearableEquipmentOption, int loadoutSlot)
        {
            foreach (Button b in buttonList)
            {
                b.interactable = true;
            }
            button.interactable = false;

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            WebRequestManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);
            string inventoryItemId = wearableEquipmentOption == null ? "" : WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.itemId == wearableEquipmentOption.itemWebId).id;
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