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

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            selectedBorder.color = pointerIsHoveringOnThisObject & weaponOption != null ? glowOnColor : glowOffColor;
            overlayCanvasGroup.alpha = pointerIsHoveringOnThisObject & weaponOption != null ? 1 : 0;
        }

        private const float selectedImageAnimationSpeed = 4;
        Vector3[] worldCorners = new Vector3[4];
        private void Update()
        {
            bool isSelected = pointerIsHoveringOnThisObject & weaponOption != null;

            gearIcon.enabled = gearIcon.sprite;
            selectedBorder.color = Vector4.MoveTowards(selectedBorder.color, isSelected ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            overlayCanvasGroup.alpha = Mathf.MoveTowards(overlayCanvasGroup.alpha, isSelected ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);

            itemDescriptionParent.GetWorldCorners(worldCorners);

            if (worldCorners[1].y > Screen.height)
            {
                itemDescriptionParent.anchoredPosition = new Vector2(itemDescriptionParent.anchoredPosition.x, itemDescriptionParent.anchoredPosition.y - (worldCorners[1].y - Screen.height));
            }
            else if (worldCorners[0].y < 0)
            {
                itemDescriptionParent.anchoredPosition = new Vector2(itemDescriptionParent.anchoredPosition.x, itemDescriptionParent.anchoredPosition.y - worldCorners[0].y);
            }

            canvas.overrideSorting = isSelected;
            canvas.sortingOrder = 25;
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