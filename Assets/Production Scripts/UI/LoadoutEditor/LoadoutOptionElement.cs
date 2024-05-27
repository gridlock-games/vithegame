using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class LoadoutOptionElement : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text itemNameText;

        public void InitializeWeapon(CharacterReference.WeaponOption weaponOption)
        {
            iconImage.sprite = weaponOption.weaponIcon;
            iconImage.color = Color.white;
            itemNameText.text = weaponOption.name;
        }

        public void InitializeEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            if (wearableEquipmentOption == null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.red;
                itemNameText.text = "None";
            }
            else
            {
                iconImage.sprite = wearableEquipmentOption.GetIcon(raceAndGender);
                iconImage.color = Color.white;
                itemNameText.text = wearableEquipmentOption.name;
            }
        }
    }
}