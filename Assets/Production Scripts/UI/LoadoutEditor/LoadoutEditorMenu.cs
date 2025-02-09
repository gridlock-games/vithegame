using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Core.CombatAgents;
using Vi.Player;
using Vi.Utility;
using Unity.Collections;

namespace Vi.UI
{
    public class LoadoutEditorMenu : Menu
    {
        [Header("Loadout Editor Menu")]
        [SerializeField] private Button[] loadoutButtons;
        [SerializeField] private Camera characterPreviewCamera;
        [SerializeField] private Button openCharacterSectionButton;
        [SerializeField] private Button openGearsSectionButton;

        [Header("Gears Section")]
        [SerializeField] private GameObject gearsSectionParent;
        [SerializeField] private CharacterStatElement[] gearSectionCharacterStatElements;

        [Header("Character Section")]
        [SerializeField] private GameObject characterSectionParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterLevelText;
        [SerializeField] private Text maxHPText;
        [SerializeField] private Text maxDefenseText;
        [SerializeField] private Text availableSkillPointsText;
        [SerializeField] private Button resetStatsButton;
        [SerializeField] private CharacterStatElement[] charSectionCharacterStatElements;

        [Header("Weapon Select Menu")]
        [SerializeField] private WeaponSelectMenu weaponSelectMenu;
        [SerializeField] private Button primaryWeaponButton;
        [SerializeField] private Button secondaryWeaponButton;

        [Header("Armor Select Menu")]
        [SerializeField] private ArmorSelectMenu armorSelectMenu;
        [SerializeField] private Button helmButton;
        [SerializeField] private Image helmImage;
        [SerializeField] private Button chestButton;
        [SerializeField] private Image chestImage;
        [SerializeField] private Button shouldersButton;
        [SerializeField] private Image shouldersImage;
        [SerializeField] private Button glovesButton;
        [SerializeField] private Image glovesImage;
        [SerializeField] private Button capeButton;
        [SerializeField] private Image capeImage;
        [SerializeField] private Sprite defaultSprite;

        private Attributes attributes;
        protected override void Awake()
        {
            base.Awake();
            attributes = GetComponentInParent<Attributes>();

            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent += OnCharPreviewDrag;
            }
        }

        private void Start()
        {
            characterNameText.text = attributes.CachedPlayerData.character.name.ToString();
            OpenGearsSection();

            string characterId = PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString();
            int currentAvailableSkillPoints = 0;
            if (WebRequestManager.Singleton.CharacterManager.TryGetCharacterAttributesInLookup(characterId, out CharacterManager.CharacterStats characterStats))
            {
                characterLevelText.text = characterStats.level.ToString();
                maxHPText.text = characterStats.hp.ToString();
                maxDefenseText.text = (characterStats.defense + characterStats.mdefense).ToString();
                currentAvailableSkillPoints = characterStats.GetAvailableSkillPoints(PlayerDataManager.Singleton.LocalPlayerData.character.attributes); ;
                availableSkillPointsText.text = currentAvailableSkillPoints.ToString();
                resetStatsButton.interactable = currentAvailableSkillPoints < characterStats.nextStatPointRwd & characterStats.nextStatPointRwd > 5;
            }
            else // Set Default values
            {
                characterLevelText.text = "1";
                maxHPText.text = "0000";
                maxDefenseText.text = "0000";
                availableSkillPointsText.text = "0";
                resetStatsButton.interactable = true;
            }

            foreach (CharacterStatElement characterStatElement in charSectionCharacterStatElements)
            {
                characterStatElement.GetAddPointButton().interactable = currentAvailableSkillPoints > 0;

                characterStatElement.OnStatCountChange += (statEle, availableSkillPoints) =>
                {
                    availableSkillPointsText.text = availableSkillPoints.ToString();
                    foreach (CharacterStatElement characterStatElement in charSectionCharacterStatElements)
                    {
                        if (characterStatElement == statEle) { continue; }
                        characterStatElement.GetAddPointButton().interactable = availableSkillPoints > 0;
                    }

                    if (WebRequestManager.Singleton.CharacterManager.TryGetCharacterAttributesInLookup(PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(), out CharacterManager.CharacterStats characterStats))
                    {
                        resetStatsButton.interactable = availableSkillPoints < characterStats.nextStatPointRwd;
                    }
                    else
                    {
                        resetStatsButton.interactable = true;
                    }
                };
            }
        }

        private void OnCharPreviewDrag(Vector2 delta)
        {
            if (previewObject)
            {
                previewObject.transform.rotation *= Quaternion.Euler(0, -delta.x * 0.25f, 0);
            }
        }

        public void ResetCharStats()
        {
            PlayerDataManager.Singleton.SetCharAttributes(PlayerDataManager.Singleton.LocalPlayerData.id, new CharacterManager.CharacterAttributes(1, 1, 1, 1, 1));

            int characterIndex = WebRequestManager.Singleton.CharacterManager.Characters.FindIndex(item => item._id == PlayerDataManager.Singleton.LocalPlayerData.character._id);
            if (characterIndex != -1)
            {
                var newCharacter = WebRequestManager.Singleton.CharacterManager.Characters[characterIndex];
                newCharacter.attributes = new CharacterManager.CharacterAttributes(1, 1, 1, 1, 1);
                WebRequestManager.Singleton.CharacterManager.Characters[characterIndex] = newCharacter;
            }

            foreach (CharacterStatElement element in gearSectionCharacterStatElements)
            {
                element.CurrentStatCount = 1;
                element.UpdateDisplay();
            }

            foreach (CharacterStatElement element in charSectionCharacterStatElements)
            {
                element.CurrentStatCount = 1;
                element.UpdateDisplay();
                element.GetAddPointButton().interactable = true;
            }

            string characterId = PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString();
            if (WebRequestManager.Singleton.CharacterManager.TryGetCharacterAttributesInLookup(characterId, out CharacterManager.CharacterStats characterStats))
            {
                availableSkillPointsText.text = characterStats.nextStatPointRwd.ToString();
            }
            else // Set Default values
            {
                availableSkillPointsText.text = "0";
            }

            resetStatsButton.interactable = false;
        }

        public void OpenCharacterSection()
        {
            characterSectionParent.SetActive(true);
            gearsSectionParent.SetActive(false);
            openCharacterSectionButton.interactable = false;
            openGearsSectionButton.interactable = true;
        }

        public void OpenGearsSection()
        {
            characterSectionParent.SetActive(false);
            gearsSectionParent.SetActive(true);
            openCharacterSectionButton.interactable = true;
            openGearsSectionButton.interactable = false;

            // Refresh character stat display
            //foreach (CharacterStatElement characterStatElement in gearSectionCharacterStatElements)
            //{
            //    characterStatElement.CurrentStatCount = PlayerDataManager.Singleton.LocalPlayerData.character.GetStat(characterStatElement.AttributeType);
            //    characterStatElement.UpdateDisplay();
            //}
        }

        [SerializeField] private Light previewLightPrefab;

        private GameObject previewLightInstance;
        private GameObject previewObject;
        private CharacterManager.Loadout lastLoadoutEvaluated;
        private void CreatePreview(bool force)
        {
            CharacterManager.Character character = PlayerDataManager.Singleton.LocalPlayerData.character;

            if (!force)
            {
                if (character.GetActiveLoadout().EqualsIgnoringSlot(lastLoadoutEvaluated)) { return; }
            }

            if (!previewObject)
            {
                // Instantiate the player model
                Vector3 basePos = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetCharacterPreviewPosition(PlayerDataManager.Singleton.LocalPlayerData.id);
                if (PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.TryGetComponent(out PooledObject pooledObject))
                {
                    previewObject = ObjectPoolingManager.SpawnObject(pooledObject,
                        basePos,
                        Quaternion.Euler(SpawnPoints.previewCharacterRotation)).gameObject;
                }
                else
                {
                    previewObject = Instantiate(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab,
                        basePos,
                        Quaternion.Euler(SpawnPoints.previewCharacterRotation));
                }

                AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();
                animationHandler.ChangeCharacter(character);

                characterPreviewCamera.transform.SetParent(null);
                characterPreviewCamera.transform.position = basePos + SpawnPoints.cameraPreviewCharacterPositionOffset;
                characterPreviewCamera.transform.rotation = Quaternion.Euler(SpawnPoints.cameraPreviewCharacterRotation);

                previewLightInstance = Instantiate(previewLightPrefab.gameObject);
                previewLightInstance.transform.position = basePos + previewObject.transform.rotation * SpawnPoints.previewLightPositionOffset;
                previewLightInstance.transform.rotation = Quaternion.Euler(SpawnPoints.previewLightRotation);
            }

            previewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());
            lastLoadoutEvaluated = character.GetActiveLoadout();
        }

        private void OnDestroy()
        {
            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent -= OnCharPreviewDrag;
            }

            if (characterPreviewCamera) { Destroy(characterPreviewCamera.gameObject); }
            if (previewLightInstance) { Destroy(previewLightInstance); }

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

        private void OnEnable()
        {
            if (characterPreviewCamera) { characterPreviewCamera.enabled = true; }

            CreatePreview(true);
            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutButtons.Length; i++)
            {
                Button button = loadoutButtons[i];
                int var = i;
                if (attributes.CachedPlayerData.character.IsSlotActive(i)) { activeLoadoutSlot = i; }
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate { OpenLoadout(button, var); });
            }

            loadoutButtons[activeLoadoutSlot].onClick.Invoke();
        }

        private void OnDisable()
        {
            if (characterPreviewCamera) { characterPreviewCamera.enabled = false; }
            if (previewLightInstance) { Destroy(previewLightInstance); }

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

        private void Update()
        {
            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame)
            {
                CreatePreview(false);
            }
        }

        private void OpenLoadout(Button button, int loadoutSlot)
        {
            foreach (Button b in loadoutButtons)
            {
                b.interactable = button != b;
            }

            PlayerDataManager.PlayerData playerData = attributes.CachedPlayerData;
            CharacterManager.Loadout loadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            if (!playerData.character.GetActiveLoadout().Equals(playerData.character.GetLoadoutFromSlot(loadoutSlot)))
            {
                playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
            
            Dictionary<string, CharacterReference.WeaponOption> weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptionsDictionary();

            CharacterReference.WeaponOption primaryWeaponOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon1ItemId.ToString(), out CharacterManager.InventoryItem weapon1InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon1InventoryItem.itemId._id, out primaryWeaponOption))
                {
                    Debug.LogWarning("Can't find primary weapon inventory item in character reference");
                }
            }
            else if (!string.IsNullOrWhiteSpace(loadout.weapon1ItemId.ToString()))
            {
                Debug.LogWarning("Can't find primary weapon inventory item " + loadout.weapon1ItemId);
            }

            CharacterReference.WeaponOption secondaryWeaponOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon2ItemId.ToString(), out CharacterManager.InventoryItem weapon2InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon2InventoryItem.itemId._id, out secondaryWeaponOption))
                {
                    Debug.LogWarning("Can't find secondary weapon inventory item in character reference");
                }
            }
            else if (!string.IsNullOrWhiteSpace(loadout.weapon2ItemId.ToString()))
            {
                Debug.LogWarning("Can't find secondary weapon inventory item " + loadout.weapon2ItemId);
            }

            primaryWeaponButton.onClick.RemoveAllListeners();
            CharacterReference.WeaponOption otherSecondaryOption = secondaryWeaponOption;
            if (otherSecondaryOption == null)
            {
                otherSecondaryOption = CharacterManager.GetDefaultSecondaryWeapon();
            }
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(primaryWeaponOption, otherSecondaryOption, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = primaryWeaponOption == null ? defaultSprite : primaryWeaponOption.weaponIcon;
            bool canEditLoadout = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;
            primaryWeaponButton.interactable = canEditLoadout;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            CharacterReference.WeaponOption otherPrimaryOption = primaryWeaponOption;
            if (otherPrimaryOption == null)
            {
                otherPrimaryOption = CharacterManager.GetDefaultPrimaryWeapon();
            }
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(secondaryWeaponOption, otherPrimaryOption, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = secondaryWeaponOption == null ? defaultSprite : secondaryWeaponOption.weaponIcon;
            secondaryWeaponButton.interactable = canEditLoadout;

            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption helmOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.helmGearItemId.ToString(), out CharacterManager.InventoryItem helmInventoryItem))
            {
                helmOption = CharacterManager.GetEquipmentOption(helmInventoryItem, playerData.character.raceAndGender);
            }
            helmButton.onClick.RemoveAllListeners();
            helmButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Helm, loadoutSlot); });
            helmButton.interactable = canEditLoadout;
            helmImage.sprite = helmOption == null ? defaultSprite : helmOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption chestOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.chestArmorGearItemId.ToString(), out CharacterManager.InventoryItem chestInventoryItem))
            {
                chestOption = CharacterManager.GetEquipmentOption(chestInventoryItem, playerData.character.raceAndGender);
            }
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Chest, loadoutSlot); });
            chestButton.interactable = canEditLoadout;
            chestImage.sprite = chestOption == null ? defaultSprite : chestOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption shouldersOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.shouldersGearItemId.ToString(), out CharacterManager.InventoryItem shouldersInventoryItem))
            {
                shouldersOption = CharacterManager.GetEquipmentOption(shouldersInventoryItem, playerData.character.raceAndGender);
            }
            shouldersButton.onClick.RemoveAllListeners();
            shouldersButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Shoulders, loadoutSlot); });
            shouldersButton.interactable = canEditLoadout;
            shouldersImage.sprite = shouldersOption == null ? defaultSprite : shouldersOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption glovesOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.glovesGearItemId.ToString(), out CharacterManager.InventoryItem glovesInventoryItem))
            {
                glovesOption = CharacterManager.GetEquipmentOption(glovesInventoryItem, playerData.character.raceAndGender);
            }
            glovesButton.onClick.RemoveAllListeners();
            glovesButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Gloves, loadoutSlot); });
            glovesButton.interactable = canEditLoadout;
            glovesImage.sprite = glovesOption == null ? defaultSprite : glovesOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption capeOption = null;
            if (CharacterManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.capeGearItemId.ToString(), out CharacterManager.InventoryItem capeInventoryItem))
            {
                capeOption = CharacterManager.GetEquipmentOption(capeInventoryItem, playerData.character.raceAndGender);
            }
            capeButton.onClick.RemoveAllListeners();
            capeButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Cape, loadoutSlot); });
            capeButton.interactable = canEditLoadout;
            capeImage.sprite = capeOption == null ? defaultSprite : capeOption.GetIcon(playerData.character.raceAndGender);
        }

        private void OpenWeaponSelect(CharacterReference.WeaponOption weaponOption, CharacterReference.WeaponOption otherOption, LoadoutManager.WeaponSlotType weaponType, int loadoutSlot)
        {
            GameObject _weaponSelect = Instantiate(weaponSelectMenu.gameObject);
            WeaponSelectMenu menu = _weaponSelect.GetComponent<WeaponSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(weaponOption, otherOption, weaponType, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _weaponSelect;
            gameObject.SetActive(false);
        }

        private void OpenArmorSelect(CharacterReference.EquipmentType equipmentType, int loadoutSlot)
        {
            GameObject _armorSelect = Instantiate(armorSelectMenu.gameObject);
            ArmorSelectMenu menu = _armorSelect.GetComponent<ArmorSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(equipmentType, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _armorSelect;
            gameObject.SetActive(false);
        }
    }
}