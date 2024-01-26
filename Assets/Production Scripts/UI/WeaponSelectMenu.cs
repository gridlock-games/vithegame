using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WeaponSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private WeaponOptionElement weaponOptionPrefab;

        public void Initialize(CharacterReference.WeaponOption weaponOption)
        {

        }

        private void Start()
        {
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                WeaponOptionElement ele = Instantiate(weaponOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<WeaponOptionElement>();
                ele.Initialize(weaponOption);
            }
        }
    }
}