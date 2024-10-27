using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Core;

namespace Vi.UI
{
    public class ShopKeeperItem : MonoBehaviour
    {
        [SerializeField] private Text itemName;
        [SerializeField] private Text price;
        [SerializeField] private Image previewIcon;

        public int Price { get; private set; }
        public void InitializeAsArmor(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            previewIcon.sprite = wearableEquipmentOption.GetIcon(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender);
            itemName.text = wearableEquipmentOption.name;
            price.text = "2";
            Price = 2;

            GetComponent<Button>().onClick.AddListener(() => PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.AddItemToCharacterInventory(
                PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(), wearableEquipmentOption.itemWebId)));
        }

        public void InitializeAsWeapon(CharacterReference.WeaponOption weaponOption)
        {
            previewIcon.sprite = weaponOption.weaponIcon;
            itemName.text = weaponOption.name;
            price.text = "3";
            Price = 3;

            GetComponent<Button>().onClick.AddListener(() => PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.AddItemToCharacterInventory(
                PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(), weaponOption.itemWebId)));
        }
    }
}