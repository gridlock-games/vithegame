using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WeaponDisplayElement : MonoBehaviour
    {
        [SerializeField] private Image gearIcon;
        [SerializeField] private Image selectedBorder;
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        private CharacterReference.WeaponOption weaponOption = null;
        public void Initialize(CharacterReference.WeaponOption weaponOption)
        {
            gearIcon.sprite = weaponOption?.weaponIcon;
            this.weaponOption = weaponOption;
        }

        private readonly static Color glowOnColor = new Color(1, 1, 1, 1);
        private readonly static Color glowOffColor = new Color(1, 1, 1, 0);

        private void OnEnable()
        {
            selectedBorder.color = pointerIsHoveringOnThisObject & weaponOption != null ? glowOnColor : glowOffColor;
            overlayCanvasGroup.alpha = pointerIsHoveringOnThisObject & weaponOption != null ? 1 : 0;
        }

        private const float selectedImageAnimationSpeed = 2;
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