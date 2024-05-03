using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Core;

namespace Vi.UI
{
    public class RuntimeWeaponCard : MonoBehaviour
    {
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image weaponSlotTypeColor;
        [SerializeField] private RectTransform ammoParent;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text magSizeText;

        private Dictionary<LoadoutManager.WeaponSlotType, Color> slotTypeColors = new Dictionary<LoadoutManager.WeaponSlotType, Color>()
        {
            { LoadoutManager.WeaponSlotType.Primary, new Color(128 / (float)255, 128 / (float)255, 128 / (float)255) },
            { LoadoutManager.WeaponSlotType.Secondary, new Color(124 / (float)255, 252 / (float)255, 0) }
        };

        private LoadoutManager loadoutManager;
        private Weapon weapon;

        public void Initialize(LoadoutManager loadoutManager, Weapon weapon, LoadoutManager.WeaponSlotType weaponSlotType)
        {
            this.loadoutManager = loadoutManager;
            this.weapon = weapon;

            weaponSlotTypeColor.color = slotTypeColors[weaponSlotType];
            weaponIcon.sprite = System.Array.Find(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions(), item => item.weapon.name == weapon.name.Replace("(Clone)", "")).weaponIcon;
            ammoParent.gameObject.SetActive(weapon.ShouldUseAmmo());

            if (weapon.ShouldUseAmmo()) { magSizeText.text = weapon.GetMaxAmmoCount().ToString(); }
        }

        private Image backgroundImage;
        private Color originalBackgroundColor;
        private Color flashAttackColor = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
        private const float backgroundColorTransitionSpeed = 16;

        private void Start()
        {
            backgroundImage = GetComponent<Image>();
            originalBackgroundColor = backgroundImage.color;
            flashAttackColor.a = originalBackgroundColor.a;
        }

        private void Update()
        {
            if (!loadoutManager) { return; }

            if (weapon.ShouldUseAmmo()) { ammoText.text = loadoutManager.GetAmmoCount(weapon).ToString(); }

            backgroundImage.color = Color.Lerp(backgroundImage.color, loadoutManager.WeaponNameThatCanFlashAttack == weapon.name ? flashAttackColor : originalBackgroundColor, Time.deltaTime * backgroundColorTransitionSpeed);
        }
    }
}