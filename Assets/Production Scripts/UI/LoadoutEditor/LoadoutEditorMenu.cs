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
        [SerializeField] private Button pantsButton;
        [SerializeField] private Image pantsImage;
        [SerializeField] private Button bootsButton;
        [SerializeField] private Image bootsImage;
        [SerializeField] private Button beltButton;
        [SerializeField] private Image beltImage;
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
            CharacterReference.WeaponOption weaponOption1 = weaponOptions[WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon1ItemId).itemId];
            CharacterReference.WeaponOption weaponOption2 = weaponOptions[WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon2ItemId).itemId];

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, weaponOption2, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            bool canEditLoadout = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;
            primaryWeaponButton.interactable = canEditLoadout;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, weaponOption1, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.interactable = canEditLoadout;

            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption helmOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.helmGearItemId).itemId);
            helmButton.onClick.RemoveAllListeners();
            helmButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Helm, loadoutSlot); });
            helmButton.interactable = canEditLoadout;
            helmImage.sprite = helmOption == null ? defaultSprite : helmOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption chestOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.chestArmorGearItemId).itemId);
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Chest, loadoutSlot); });
            chestButton.interactable = canEditLoadout;
            chestImage.sprite = chestOption == null ? defaultSprite : chestOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption shouldersOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.shouldersGearItemId).itemId);
            shouldersButton.onClick.RemoveAllListeners();
            shouldersButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Shoulders, loadoutSlot); });
            shouldersButton.interactable = canEditLoadout;
            shouldersImage.sprite = shouldersOption == null ? defaultSprite : shouldersOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption glovesOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.glovesGearItemId).itemId);
            glovesButton.onClick.RemoveAllListeners();
            glovesButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Gloves, loadoutSlot); });
            glovesButton.interactable = canEditLoadout;
            glovesImage.sprite = glovesOption == null ? defaultSprite : glovesOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption pantsOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.pantsGearItemId).itemId);
            pantsButton.onClick.RemoveAllListeners();
            pantsButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Pants, loadoutSlot); });
            pantsButton.interactable = canEditLoadout;
            pantsImage.sprite = pantsOption == null ? defaultSprite : pantsOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption bootsOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.bootsGearItemId).itemId);
            bootsButton.onClick.RemoveAllListeners();
            bootsButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Boots, loadoutSlot); });
            bootsButton.interactable = canEditLoadout;
            bootsImage.sprite = bootsOption == null ? defaultSprite : bootsOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption beltOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.beltGearItemId).itemId);
            beltButton.onClick.RemoveAllListeners();
            beltButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Belt, loadoutSlot); });
            beltButton.interactable = canEditLoadout;
            beltImage.sprite = beltOption == null ? defaultSprite : beltOption.GetIcon(playerData.character.raceAndGender);

            CharacterReference.WearableEquipmentOption capeOption = armorOptions.Find(item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.capeGearItemId).itemId);
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