using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vi.UI
{
    public class ArmorDisplayElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image selectedBorder;
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        private readonly static Color glowOnColor = new Color(1, 1, 1, 1);
        private readonly static Color glowOffColor = new Color(1, 1, 1, 0);

        private void OnEnable()
        {
            selectedBorder.color = pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor;
            overlayCanvasGroup.alpha = pointerIsHoveringOnThisObject ? 1 : 0;
        }

        private const float selectedImageAnimationSpeed = 2;
        private void Update()
        {
            selectedBorder.color = Vector4.MoveTowards(selectedBorder.color, pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            overlayCanvasGroup.alpha = Mathf.MoveTowards(overlayCanvasGroup.alpha, pointerIsHoveringOnThisObject ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);
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