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

        private LoadoutManager loadoutManager;
        private void Awake()
        {
            loadoutManager = GetComponentInParent<LoadoutManager>();
        }

        private void OnEnable()
        {
            for (int i = 0; i < loadoutButtons.Length; i++)
            {
                Button button = loadoutButtons[i];
                int var = i;
                button.onClick.AddListener(delegate { OpenLoadout(button, WebRequestManager.Singleton.CharacterById.GetLoadoutFromSlot(var), var); });
            }

            loadoutButtons[0].onClick.Invoke();
        }

        private void OpenLoadout(Button button, WebRequestManager.Loadout loadout, int loadoutSlot)
        {
            foreach (Button b in loadoutButtons)
            {
                b.interactable = button != b;
            }

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            CharacterReference.WeaponOption weaponOption1 = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon1ItemId);
            CharacterReference.WeaponOption weaponOption2 = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon2ItemId);

            primaryWeaponButton.onClick.RemoveAllListeners();
            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1, LoadoutManager.WeaponSlotType.Primary, loadoutSlot); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            primaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption1.name;

            secondaryWeaponButton.onClick.RemoveAllListeners();
            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2, LoadoutManager.WeaponSlotType.Secondary, loadoutSlot); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption2.name;
        }

        private void OpenWeaponSelect(CharacterReference.WeaponOption weaponOption, LoadoutManager.WeaponSlotType weaponType, int loadoutSlot)
        {
            GameObject _weaponSelect = Instantiate(weaponSelectMenu.gameObject);
            WeaponSelectMenu menu = _weaponSelect.GetComponent<WeaponSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(weaponOption, weaponType, loadoutManager, loadoutSlot);
            childMenu = _weaponSelect;
            gameObject.SetActive(false);
        }
    }
}