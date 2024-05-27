using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using UnityEngine.EventSystems;

namespace Vi.UI
{
    public class CharacterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject characterParent;
        [SerializeField] private GameObject addParent;
        [SerializeField] private GameObject lockedParent;
        [SerializeField] private Image selectedOverlay;
        [SerializeField] private Image selectedBorder;
        [SerializeField] private Image selectedGlow;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;

        public Button Button;

        public bool IsCharacter { get; private set; }

        private RectTransform rt;
        private Vector2 originalSizeDelta;

        private void Awake()
        {
            rt = (RectTransform)transform;
            originalSizeDelta = rt.sizeDelta;
        }

        public void InitializeAsAddButton()
        {
            Button.interactable = true;
            isSelected = false;
            IsCharacter = false;
            characterParent.SetActive(false);
            addParent.SetActive(true);
            lockedParent.SetActive(false);
        }

        public void InitializeAsLockedCharacter()
        {
            Button.interactable = false;
            isSelected = false;
            IsCharacter = false;
            characterParent.SetActive(false);
            addParent.SetActive(false);
            lockedParent.SetActive(true);
        }

        public void InitializeAsCharacter(WebRequestManager.Character character)
        {
            Button.interactable = true;
            isSelected = false;
            IsCharacter = true;
            characterParent.SetActive(true);
            addParent.SetActive(false);
            lockedParent.SetActive(false);

            nameText.text = character.name.ToString();
            levelText.text = "Lv." + character.experience.ToString();
        }

        public void SetSelectedState(bool isNotSelected)
        {
            if (!IsCharacter) { return; }
            isSelected = !isNotSelected;
            Button.interactable = isNotSelected;
        }

        private void OnEnable()
        {
            selectedOverlay.fillAmount = isSelected ? 1 : 0;
            selectedBorder.fillAmount = isSelected ? 1 : 0;
            selectedGlow.color = isSelected ? glowOnColor : glowOffColor;
            rt.sizeDelta = isSelected ? originalSizeDelta * selectedSizeMutliplier : originalSizeDelta;
        }

        private readonly static Color glowOnColor = new Color(1, 1, 1, 1);
        private readonly static Color glowOffColor = new Color(1, 1, 1, 0);

        private const float selectedSizeMutliplier = 1.13f;
        private const float selectedImageAnimationSpeed = 8;
        private bool isSelected;
        private void Update()
        {
            selectedOverlay.fillAmount = Mathf.Lerp(selectedOverlay.fillAmount, isSelected | pointerIsHoveringOnThisObject ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);
            selectedBorder.fillAmount = Mathf.Lerp(selectedBorder.fillAmount, isSelected | pointerIsHoveringOnThisObject ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);
            selectedGlow.color = Color.Lerp(selectedGlow.color, isSelected | pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            rt.sizeDelta = Vector2.Lerp(rt.sizeDelta, isSelected | pointerIsHoveringOnThisObject ? originalSizeDelta * selectedSizeMutliplier : originalSizeDelta, Time.deltaTime * selectedImageAnimationSpeed);
        }

        private bool pointerIsHoveringOnThisObject;
        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            if (!IsCharacter) { return; }
            pointerIsHoveringOnThisObject = true;
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            if (!IsCharacter) { return; }
            pointerIsHoveringOnThisObject = false;
        }
    }
}