using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class WeaponSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private WeaponOptionElement weaponOptionPrefab;

        private List<Button> buttonList = new List<Button>();
        private LoadoutManager.WeaponSlotType weaponType;
        private LoadoutManager loadoutManager;
        public void Initialize(CharacterReference.WeaponOption initialOption, LoadoutManager.WeaponSlotType weaponType, LoadoutManager loadoutManager, int loadoutSlot)
        {
            this.loadoutManager = loadoutManager;
            this.weaponType = weaponType;
            Button invokeThis = null;
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                WeaponOptionElement ele = Instantiate(weaponOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<WeaponOptionElement>();
                ele.Initialize(weaponOption);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeWeapon(button, weaponOption, loadoutSlot); });
                buttonList.Add(button);

                if (weaponOption.itemWebId == initialOption.itemWebId) { invokeThis = button; }
            }

            invokeThis.onClick.Invoke();
        }

        private void ChangeWeapon(Button button, CharacterReference.WeaponOption weaponOption, int loadoutSlot)
        {
            foreach (Button b in buttonList)
            {
                b.interactable = true;
            }
            button.interactable = false;
            
            WebRequestManager.Loadout newLoadout = WebRequestManager.Singleton.CharacterById.GetLoadoutFromSlot(loadoutSlot);
            switch (weaponType)
            {
                case LoadoutManager.WeaponSlotType.Primary:
                    newLoadout.weapon1ItemId = weaponOption.itemWebId;
                    break;
                case LoadoutManager.WeaponSlotType.Secondary:
                    newLoadout.weapon2ItemId = weaponOption.itemWebId;
                    break;
                default:
                    Debug.LogError("Not sure how to handle weapon slot type " + weaponType);
                    break;
            }

            loadoutManager.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(WebRequestManager.Singleton.CharacterById, newLoadout));
            loadoutManager.ChangeWeapon(weaponType, weaponOption);
        }
    }
}