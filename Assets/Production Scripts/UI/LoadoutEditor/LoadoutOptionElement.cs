using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class LoadoutOptionElement : MonoBehaviour
    {
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Text weaponNameText;

        public void InitializeWeapon(CharacterReference.WeaponOption weaponOption)
        {
            weaponIconImage.sprite = weaponOption.weaponIcon;
            weaponNameText.text = weaponOption.name;
        }

        public void InitializeEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            if (wearableEquipmentOption == null)
            {
                weaponIconImage.sprite = null;
                weaponIconImage.color = Color.red;
                weaponNameText.text = "None";
            }
            else
            {
                weaponIconImage.sprite = wearableEquipmentOption.GetIcon(raceAndGender);
                weaponIconImage.color = Color.white;
                weaponNameText.text = wearableEquipmentOption.name;
            }
        }
    }
}