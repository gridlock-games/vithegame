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
        private int playerDataId;
        public void Initialize(CharacterReference.WeaponOption initialOption, CharacterReference.WeaponOption otherWeapon, LoadoutManager.WeaponSlotType weaponType, LoadoutManager loadoutManager, int loadoutSlot, int playerDataId)
        {
            this.loadoutManager = loadoutManager;
            this.weaponType = weaponType;
            this.playerDataId = playerDataId;
            Button invokeThis = null;
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                WeaponOptionElement ele = Instantiate(weaponOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<WeaponOptionElement>();
                ele.Initialize(weaponOption);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeWeapon(button, weaponOption, loadoutSlot); });

                if (weaponOption.itemWebId != otherWeapon.itemWebId) { buttonList.Add(button); }
                else { button.interactable = false; }

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

            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            WebRequestManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);
            string inventoryItemId = WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.itemId == weaponOption.itemWebId).id;
            switch (weaponType)
            {
                case LoadoutManager.WeaponSlotType.Primary:
                    newLoadout.weapon1ItemId = inventoryItemId;
                    break;
                case LoadoutManager.WeaponSlotType.Secondary:
                    newLoadout.weapon2ItemId = inventoryItemId;
                    break;
                default:
                    Debug.LogError("Not sure how to handle weapon slot type " + weaponType);
                    break;
            }

            if (weaponPreviewObject) { Destroy(weaponPreviewObject); }
            if (weaponOption.weaponPreviewPrefab) { weaponPreviewObject = Instantiate(weaponOption.weaponPreviewPrefab); }
            
            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(playerData.character._id.ToString(), newLoadout));
            loadoutManager.ChangeWeapon(weaponType, inventoryItemId);

            playerData.character = playerData.character.ChangeLoadoutFromSlot(loadoutSlot, newLoadout);
            PlayerDataManager.Singleton.SetPlayerData(playerData);
        }

        private GameObject weaponPreviewObject;
        private void OnDestroy()
        {
            Destroy(weaponPreviewObject);
        }
    }
}