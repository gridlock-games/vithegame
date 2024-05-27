using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class LoadoutEditorMenu : Menu
    {
        [Header("Loadout Editor Menu")]
        [SerializeField] private Button[] loadoutButtons;
        [SerializeField] private Camera characterPreviewCameraPrefab;
        [SerializeField] private Vector3 characterPreviewCameraOffset;

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
        private GameObject camInstance;
        private void Awake()
        {
            attributes = GetComponentInParent<Attributes>();

            camInstance = Instantiate(characterPreviewCameraPrefab.gameObject, transform.parent);
            camInstance.transform.position = transform.root.position + transform.root.rotation * new Vector3(characterPreviewCameraOffset.x, 0, characterPreviewCameraOffset.z);
            camInstance.transform.LookAt(transform.root);
            camInstance.transform.position += new Vector3(0, characterPreviewCameraOffset.y, 0);
        }

        private void Update()
        {
            camInstance.transform.position = transform.root.position + transform.root.rotation * new Vector3(characterPreviewCameraOffset.x, 0, characterPreviewCameraOffset.z);
            camInstance.transform.LookAt(transform.root);
            camInstance.transform.position += new Vector3(0, characterPreviewCameraOffset.y, 0);
        }

        private void OnDestroy()
        {
            Destroy(camInstance);
        }

        private void OnEnable()
        {
            camInstance.SetActive(true);
            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutButtons.Length; i++)
            {
                Button button = loadoutButtons[i];
                int var = i;
                if (PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.IsSlotActive(i)) { activeLoadoutSlot = i; }
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate { OpenLoadout(button, var); });
            }

            loadoutButtons[activeLoadoutSlot].onClick.Invoke();
        }

        private void OpenLoadout(Button button, int loadoutSlot)
        {
            foreach (Button b in loadoutButtons)
            {
                b.interactable = button != b;
            }

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            WebRequestManager.Loadout loadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UseCharacterLoadout(playerData.character._id.ToString(), (loadoutSlot+1).ToString()));

            playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
            PlayerDataManager.Singleton.SetPlayerData(playerData);

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            CharacterReference.WeaponOption weaponOption1 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon1ItemId).itemId);
            CharacterReference.WeaponOption weaponOption2 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon2ItemId).itemId);

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, weaponOption2, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            primaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption1.name;
            bool canEditLoadout = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;
            primaryWeaponButton.interactable = canEditLoadout;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, weaponOption1, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption2.name;
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
            camInstance.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OpenArmorSelect(CharacterReference.EquipmentType equipmentType, int loadoutSlot)
        {
            GameObject _armorSelect = Instantiate(armorSelectMenu.gameObject);
            ArmorSelectMenu menu = _armorSelect.GetComponent<ArmorSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(equipmentType, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _armorSelect;
            camInstance.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}