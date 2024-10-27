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

        public bool IsWeapon { get; private set; }
        public CharacterReference.WeaponOption weaponOption { get; private set; }

        public bool IsArmor { get; private set; }
        public CharacterReference.WearableEquipmentOption equipmentOption { get; private set; }

        public int Price { get; private set; }
        public void InitializeAsArmor(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            previewIcon.sprite = wearableEquipmentOption.GetIcon(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender);
            itemName.text = wearableEquipmentOption.name;
            price.text = "2";
            Price = 2;

            IsArmor = true;
            equipmentOption = wearableEquipmentOption;
        }

        public void InitializeAsWeapon(CharacterReference.WeaponOption weaponOption)
        {
            previewIcon.sprite = weaponOption.weaponIcon;
            itemName.text = weaponOption.name;
            price.text = "3";
            Price = 3;

            IsWeapon = true;
            this.weaponOption = weaponOption;
        }

        public Selectable[] Selectables { get { return _selectables; } }
        private Selectable[] _selectables;
        private void Awake()
        {
            _selectables = GetComponentsInChildren<Selectable>();
        }
    }
}