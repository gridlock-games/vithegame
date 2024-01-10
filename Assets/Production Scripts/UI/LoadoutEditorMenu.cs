using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vi.Core;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.UI
{
    public class LoadoutEditorMenu : Menu
    {
        [SerializeField] private TMP_Dropdown primaryWeaponDropdown;
        [SerializeField] private TMP_Dropdown secondaryWeaponDropdown;

        private void Awake()
        {
            primaryWeaponDropdown.ClearOptions();
            secondaryWeaponDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> weaponOptions = new List<TMP_Dropdown.OptionData>();
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                weaponOptions.Add(new TMP_Dropdown.OptionData(weaponOption.weapon.name, weaponOption.weaponIcon));
            }

            primaryWeaponDropdown.AddOptions(weaponOptions);
            secondaryWeaponDropdown.AddOptions(weaponOptions);

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.Singleton.LocalClientId);

            int primaryIndex = weaponOptions.FindIndex(item => item.text == playerData.character.loadoutPreset1.weapon1ItemId);
            int secondaryIndex = weaponOptions.FindIndex(item => item.text == playerData.character.loadoutPreset1.weapon2ItemId);

            primaryWeaponDropdown.SetValueWithoutNotify(primaryIndex);
            secondaryWeaponDropdown.SetValueWithoutNotify(secondaryIndex);
        }

        public void OnWeaponChange()
        {
            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(PlayerDataManager.Singleton.GetPlayerData(NetworkManager.Singleton.LocalClientId).character));
        }
    }
}