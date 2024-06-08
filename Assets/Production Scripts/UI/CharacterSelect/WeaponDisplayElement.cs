using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WeaponDisplayElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image gearIcon;
        [SerializeField] private Image selectedBorder;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform itemDescriptionParent;
        [SerializeField] private Image[] abilityImages;

        private CharacterReference.WeaponOption weaponOption = null;
        public void Initialize(CharacterReference.WeaponOption weaponOption)
        {
            gearIcon.sprite = weaponOption?.weaponIcon;
            this.weaponOption = weaponOption;

            for (int i = 0; i < weaponOption.weapon.GetAbilities().Count; i++)
            {
                abilityImages[i].sprite = weaponOption.weapon.GetAbilities()[i].abilityImageIcon;
            }
        }

        private readonly static Color glowOnColor = new Color(1, 1, 1, 1);
        private readonly static Color glowOffColor = new Color(1, 1, 1, 0);

        private void OnEnable()
        {
            selectedBorder.color = pointerIsHoveringOnThisObject & weaponOption != null ? glowOnColor : glowOffColor;
            overlayCanvasGroup.alpha = pointerIsHoveringOnThisObject & weaponOption != null ? 1 : 0;
        }

        private const float selectedImageAnimationSpeed = 4;
        private void Update()
        {
            gearIcon.enabled = gearIcon.sprite;
            selectedBorder.color = Vector4.MoveTowards(selectedBorder.color, pointerIsHoveringOnThisObject & weaponOption != null ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            overlayCanvasGroup.alpha = Mathf.MoveTowards(overlayCanvasGroup.alpha, pointerIsHoveringOnThisObject & weaponOption != null ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);
        
            
        }

        private bool pointerIsHoveringOnThisObject;
        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            pointerIsHoveringOnThisObject = true;
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            pointerIsHoveringOnThisObject = false;
        }
    }
}