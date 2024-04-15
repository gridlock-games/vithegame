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
        [SerializeField] private Button chestButton;
        [SerializeField] private Button shouldersButton;
        [SerializeField] private Button glovesButton;
        [SerializeField] private Button pantsButton;
        [SerializeField] private Button bootsButton;
        [SerializeField] private Button beltButton;
        [SerializeField] private Button capeButton;

        private LoadoutManager loadoutManager;
        private Attributes attributes;
        private GameObject camInstance;
        private void Awake()
        {
            loadoutManager = GetComponentInParent<LoadoutManager>();
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

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
            WebRequestManager.Loadout loadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UseCharacterLoadout(playerData.character._id.ToString(), (loadoutSlot+1).ToString()));

            playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
            PlayerDataManager.Singleton.SetPlayerData(playerData);

            CharacterReference.WeaponOption weaponOption1 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon1ItemId).itemId);
            CharacterReference.WeaponOption weaponOption2 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon2ItemId).itemId);

            loadoutManager.ChangeWeapon(LoadoutManager.WeaponSlotType.Primary, loadout.weapon1ItemId.ToString(), PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None);
            loadoutManager.ChangeWeapon(LoadoutManager.WeaponSlotType.Secondary, loadout.weapon2ItemId.ToString(), PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None);
            loadoutManager.StartCoroutine(loadoutManager.ApplyEquipmentFromLoadout(playerData.character.raceAndGender, loadout));

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, weaponOption2, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            primaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption1.name;
            primaryWeaponButton.interactable = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, weaponOption1, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption2.name;
            secondaryWeaponButton.interactable = PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;

            helmButton.onClick.RemoveAllListeners();
            helmButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Helm, loadoutSlot); });
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Chest, loadoutSlot); });
            shouldersButton.onClick.RemoveAllListeners();
            shouldersButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Shoulders, loadoutSlot); });
            glovesButton.onClick.RemoveAllListeners();
            glovesButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Gloves, loadoutSlot); });
            pantsButton.onClick.RemoveAllListeners();
            pantsButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Pants, loadoutSlot); });
            bootsButton.onClick.RemoveAllListeners();
            bootsButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Boots, loadoutSlot); });
            beltButton.onClick.RemoveAllListeners();
            beltButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Belt, loadoutSlot); });
            capeButton.onClick.RemoveAllListeners();
            capeButton.onClick.AddListener(delegate { OpenArmorSelect(CharacterReference.EquipmentType.Cape, loadoutSlot); });
        }

        private void OpenWeaponSelect(CharacterReference.WeaponOption weaponOption, CharacterReference.WeaponOption otherOption, LoadoutManager.WeaponSlotType weaponType, int loadoutSlot)
        {
            GameObject _weaponSelect = Instantiate(weaponSelectMenu.gameObject);
            WeaponSelectMenu menu = _weaponSelect.GetComponent<WeaponSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(weaponOption, otherOption, weaponType, loadoutManager, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _weaponSelect;
            gameObject.SetActive(false);
            camInstance.SetActive(false);
        }

        private void OpenArmorSelect(CharacterReference.EquipmentType equipmentType, int loadoutSlot)
        {
            GameObject _armorSelect = Instantiate(armorSelectMenu.gameObject);
            ArmorSelectMenu menu = _armorSelect.GetComponent<ArmorSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(equipmentType, loadoutManager, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _armorSelect;
            gameObject.SetActive(false);
            camInstance.SetActive(true);
        }
    }
}