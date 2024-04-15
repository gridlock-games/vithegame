using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WeaponOptionElement : MonoBehaviour
    {
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Text weaponNameText;

        public void Initialize(CharacterReference.WeaponOption weaponOption)
        {
            weaponIconImage.sprite = weaponOption.weaponIcon;
            weaponNameText.text = weaponOption.name;
        }

        public void Initialize(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            weaponIconImage.sprite = null;
            weaponIconImage.color = Color.gray;
            weaponNameText.text = wearableEquipmentOption.name;
        }
    }
}