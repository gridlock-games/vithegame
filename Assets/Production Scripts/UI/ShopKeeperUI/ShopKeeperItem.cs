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

        public string ItemId { get; private set; }

        public bool IsWeapon { get; private set; }
        public CharacterReference.WeaponOption weaponOption { get; private set; }

        public bool IsArmor { get; private set; }
        public CharacterReference.WearableEquipmentOption equipmentOption { get; private set; }

        public int Price { get; private set; }
        public void InitializeAsArmor(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            previewIcon.sprite = wearableEquipmentOption.GetIcon(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender);
            itemName.text = wearableEquipmentOption.name;
            Price = 3;
            price.text = Price.ToString();

            IsArmor = true;
            equipmentOption = wearableEquipmentOption;
            ItemId = wearableEquipmentOption.itemWebId;
        }

        public void InitializeAsWeapon(CharacterReference.WeaponOption weaponOption)
        {
            previewIcon.sprite = weaponOption.weaponIcon;
            itemName.text = weaponOption.name;
            Price = 5;
            price.text = Price.ToString();

            IsWeapon = true;
            this.weaponOption = weaponOption;
            ItemId = weaponOption.itemWebId;
        }

        public Selectable[] Selectables { get { return _selectables; } }
        private Selectable[] _selectables;
        private void Awake()
        {
            _selectables = GetComponentsInChildren<Selectable>();
        }
    }
}