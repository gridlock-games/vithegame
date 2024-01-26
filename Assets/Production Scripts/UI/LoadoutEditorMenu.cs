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

        private void OnEnable()
        {
            for (int i = 0; i < loadoutButtons.Length; i++)
            {
                WebRequestManager.Loadout loadout = WebRequestManager.Singleton.GetDefaultLoadout(CharacterReference.RaceAndGender.HumanMale);
                switch (i)
                {
                    case 0:
                        loadout = WebRequestManager.Singleton.CharacterById.loadoutPreset1;
                        break;
                    case 1:
                        loadout = WebRequestManager.Singleton.CharacterById.loadoutPreset2;
                        break;
                    case 2:
                        loadout = WebRequestManager.Singleton.CharacterById.loadoutPreset3;
                        break;
                    case 3:
                        loadout = WebRequestManager.Singleton.CharacterById.loadoutPreset4;
                        break;
                    default:
                        Debug.LogError("Not sure how to handle index " + i);
                        break;
                }

                Button button = loadoutButtons[i];
                button.onClick.AddListener(delegate { OpenLoadout(button, loadout); });
            }

            loadoutButtons[0].onClick.Invoke();
        }

        private void OpenLoadout(Button button, WebRequestManager.Loadout loadout)
        {
            foreach (Button b in loadoutButtons)
            {
                b.interactable = button != b;
            }

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            CharacterReference.WeaponOption weaponOption1 = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon1ItemId | item.weapon.name == loadout.weapon1ItemId);
            CharacterReference.WeaponOption weaponOption2 = System.Array.Find(weaponOptions, item => item.itemWebId == loadout.weapon1ItemId | item.weapon.name == loadout.weapon2ItemId);

            primaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption1); });
            primaryWeaponButton.GetComponent<Image>().sprite = weaponOption1.weaponIcon;
            primaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption1.name;

            secondaryWeaponButton.onClick.AddListener(delegate { OpenWeaponSelect(weaponOption2); });
            secondaryWeaponButton.GetComponent<Image>().sprite = weaponOption2.weaponIcon;
            secondaryWeaponButton.GetComponentInChildren<Text>().text = weaponOption2.name;
        }

        private void OpenWeaponSelect(CharacterReference.WeaponOption weaponOption)
        {
            GameObject _weaponSelect = Instantiate(weaponSelectMenu.gameObject);
            WeaponSelectMenu menu = _weaponSelect.GetComponent<WeaponSelectMenu>();
            menu.SetLastMenu(gameObject);
            menu.Initialize(weaponOption);
            childMenu = _weaponSelect;
            gameObject.SetActive(false);
        }
    }
}