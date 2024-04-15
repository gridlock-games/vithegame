using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class ArmorSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private WeaponOptionElement weaponOptionPrefab;

        private int playerDataId;
        public void Initialize(int playerDataId)
        {
            this.playerDataId = playerDataId;

            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions())
            {
                WeaponOptionElement ele = Instantiate(weaponOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<WeaponOptionElement>();
                ele.Initialize(wearableEquipmentOption);
                Button button = ele.GetComponentInChildren<Button>();
                //button.onClick.AddListener(delegate { ChangeWeapon(button, weaponOption, loadoutSlot); });

                //if (weaponOption.itemWebId != otherWeapon.itemWebId) { buttonList.Add(button); }
                //else { button.interactable = false; }

                //if (weaponOption.itemWebId == initialOption.itemWebId) { invokeThis = button; }
            }
        }

        private void ChangeArmor()
        {

        }
    }
}