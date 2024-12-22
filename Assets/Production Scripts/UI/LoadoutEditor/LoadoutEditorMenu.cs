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
        [SerializeField] private GameObject gearsSectionParent;
        [SerializeField] private GameObject characterSectionParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Button openCharacterSectionButton;
        [SerializeField] private Button openGearsSectionButton;
        [SerializeField] private Text underConstructionAlertText;
        [SerializeField] private Button[] buttonsThatOpenConstructionAlert;

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
        private FixedString64Bytes originalActiveLoadoutSlot;
        private void Awake()
        {
            attributes = GetComponentInParent<Attributes>();
            originalActiveLoadoutSlot = attributes.CachedPlayerData.character.GetActiveLoadout().loadoutSlot;

            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent += OnCharPreviewDrag;
            }
        }

        private void Start()
        {
            characterNameText.text = attributes.CachedPlayerData.character.name.ToString();

            OpenGearsSection();

            underConstructionAlertText.text = "";
            foreach (Button button in buttonsThatOpenConstructionAlert)
            {
                button.onClick.AddListener(OpenConstructionAlert);
            }
        }

        private void OpenConstructionAlert()
        {
            underConstructionAlertText.text = "Coming Soon!";
            if (constructionAlertCoroutine != null) { StopCoroutine(constructionAlertCoroutine); }
            constructionAlertCoroutine = StartCoroutine(ClearConstructionAlert());
        }

        private Coroutine constructionAlertCoroutine;
        private IEnumerator ClearConstructionAlert()
        {
            yield return new WaitForSeconds(2);
            underConstructionAlertText.text = "";
        }

        private void OnCharPreviewDrag(Vector2 delta)
        {
            if (previewObject)
            {
                previewObject.transform.rotation *= Quaternion.Euler(0, -delta.x * 0.25f, 0);
            }
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
        }

        private GameObject previewObject;
        private void CreatePreview()
        {
            WebRequestManager.Character character = PlayerDataManager.Singleton.LocalPlayerData.character;

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
            }

            previewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());
        }

        private void OnDestroy()
        {
            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent -= OnCharPreviewDrag;
            }

            if (characterPreviewCamera) { Destroy(characterPreviewCamera.gameObject); }

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

            FixedString64Bytes activeLoadoutSlot = attributes.CachedPlayerData.character.GetActiveLoadout().loadoutSlot.ToString();
            if (originalActiveLoadoutSlot != activeLoadoutSlot)
            {
                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.UseCharacterLoadout(attributes.CachedPlayerData.character._id.ToString(), activeLoadoutSlot.ToString()));
            }
        }

        private void OnEnable()
        {
            if (characterPreviewCamera) { characterPreviewCamera.enabled = true; }

            CreatePreview();
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
                CreatePreview();
            }
        }

        private void OpenLoadout(Button button, int loadoutSlot)
        {
            foreach (Button b in loadoutButtons)
            {
                b.interactable = button != b;
            }

            PlayerDataManager.PlayerData playerData = attributes.CachedPlayerData;
            WebRequestManager.Loadout loadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            if (!playerData.character.GetActiveLoadout().Equals(playerData.character.GetLoadoutFromSlot(loadoutSlot)))
            {
                playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
            
            Dictionary<string, CharacterReference.WeaponOption> weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptionsDictionary();

            CharacterReference.WeaponOption weaponOption1 = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon1ItemId.ToString(), out WebRequestManager.InventoryItem weapon1InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon1InventoryItem.itemId, out weaponOption1))
                {
                    Debug.LogWarning("Can't find primary weapon inventory item in character reference");
                }
            }
            else if (!string.IsNullOrWhiteSpace(loadout.weapon1ItemId.ToString()))
            {
                Debug.LogWarning("Can't find primary weapon inventory item " + loadout.weapon1ItemId);
            }

            CharacterReference.WeaponOption weaponOption2 = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon2ItemId.ToString(), out WebRequestManager.InventoryItem weapon2InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon2InventoryItem.itemId, out weaponOption2))
                {
                    Debug.LogWarning("Can't find secondary weapon inventory item in character reference");
                }
            }
            else if (!string.IsNullOrWhiteSpace(loadout.weapon2ItemId.ToString()))
            {
                Debug.LogWarning("Can't find secondary weapon inventory item " + loadout.weapon2ItemId);
            }

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, weaponOption2, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1 == null ? defaultSprite : weaponOption1.weaponIcon;
            bool canEditLoadout = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;
            primaryWeaponButton.interactable = canEditLoadout;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, weaponOption1, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2 == null ? defaultSprite : weaponOption2.weaponIcon;
            secondaryWeaponButton.interactable = canEditLoadout;

            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption helmOption = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.helmGearItemId.ToString(), out WebRequestManager.InventoryItem helmInventoryItem))
            {
                helmOption = WebRequestManager.GetEquipmentOption(helmInventoryItem, playerData.character.raceAndGender);
            }
            helmButton.onClick.RemoveAllListeners();
            helmButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Helm, loadoutSlot); });
            helmButton.interactable = canEditLoadout;
            helmImage.sprite = helmOption == null ? defaultSprite : helmOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption chestOption = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.chestArmorGearItemId.ToString(), out WebRequestManager.InventoryItem chestInventoryItem))
            {
                chestOption = WebRequestManager.GetEquipmentOption(chestInventoryItem, playerData.character.raceAndGender);
            }
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Chest, loadoutSlot); });
            chestButton.interactable = canEditLoadout;
            chestImage.sprite = chestOption == null ? defaultSprite : chestOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption shouldersOption = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.shouldersGearItemId.ToString(), out WebRequestManager.InventoryItem shouldersInventoryItem))
            {
                shouldersOption = WebRequestManager.GetEquipmentOption(shouldersInventoryItem, playerData.character.raceAndGender);
            }
            shouldersButton.onClick.RemoveAllListeners();
            shouldersButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Shoulders, loadoutSlot); });
            shouldersButton.interactable = canEditLoadout;
            shouldersImage.sprite = shouldersOption == null ? defaultSprite : shouldersOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption glovesOption = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.glovesGearItemId.ToString(), out WebRequestManager.InventoryItem glovesInventoryItem))
            {
                glovesOption = WebRequestManager.GetEquipmentOption(glovesInventoryItem, playerData.character.raceAndGender);
            }
            glovesButton.onClick.RemoveAllListeners();
            glovesButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Gloves, loadoutSlot); });
            glovesButton.interactable = canEditLoadout;
            glovesImage.sprite = glovesOption == null ? defaultSprite : glovesOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption capeOption = null;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.capeGearItemId.ToString(), out WebRequestManager.InventoryItem capeInventoryItem))
            {
                capeOption = WebRequestManager.GetEquipmentOption(capeInventoryItem, playerData.character.raceAndGender);
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