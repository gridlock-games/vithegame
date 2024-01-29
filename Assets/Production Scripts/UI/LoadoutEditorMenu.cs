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
        [SerializeField] private WeaponSelectMenu weaponSelectMenu;
        [SerializeField] private Button[] loadoutButtons;

        [SerializeField] private Button primaryWeaponButton;
        [SerializeField] private Button secondaryWeaponButton;

        [SerializeField] private Camera characterPreviewCameraPrefab;
        [SerializeField] private Vector3 characterPreviewCameraOffset;

        private LoadoutManager loadoutManager;
        private Attributes attributes;
        private GameObject camInstance;
        private void Awake()
        {
            loadoutManager = GetComponentInParent<LoadoutManager>();
            attributes = GetComponentInParent<Attributes>();

            camInstance = Instantiate(characterPreviewCameraPrefab.gameObject, transform);
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
            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutButtons.Length; i++)
            {
                Button button = loadoutButtons[i];
                int var = i;
                if (PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.IsSlotActive(i)) { activeLoadoutSlot = i; }
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

            WebRequestManager.Singleton.UseCharacterLoadout(playerData.character._id.ToString(), (loadoutSlot+1).ToString());

            playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
            PlayerDataManager.Singleton.SetPlayerData(playerData);

            CharacterReference.WeaponOption weaponOption1 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon1ItemId).itemId);
            CharacterReference.WeaponOption weaponOption2 = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == loadout.weapon2ItemId).itemId);

            loadoutManager.ChangeWeapon(LoadoutManager.WeaponSlotType.Primary, weaponOption1);
            loadoutManager.ChangeWeapon(LoadoutManager.WeaponSlotType.Secondary, weaponOption2);

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, weaponOption2, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            primaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption1.name;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, weaponOption1, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption2.name;
        }

        private void OpenWeaponSelect(CharacterReference.WeaponOption weaponOption, CharacterReference.WeaponOption otherOption, LoadoutManager.WeaponSlotType weaponType, int loadoutSlot)
        {
            GameObject _weaponSelect = Instantiate(weaponSelectMenu.gameObject);
            WeaponSelectMenu menu = _weaponSelect.GetComponent<WeaponSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(weaponOption, otherOption, weaponType, loadoutManager, loadoutSlot, attributes.GetPlayerDataId());
            childMenu = _weaponSelect;
            gameObject.SetActive(false);
        }
    }
}