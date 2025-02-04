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

        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void InitializeAsAddButton()
        {
            Button.interactable = true;
            isSelected = false;
            IsCharacter = false;
            characterParent.SetActive(false);
            addParent.SetActive(true);
            lockedParent.SetActive(false);

            SetSelectedState(true);
        }

        public void InitializeAsLockedCharacter()
        {
            Button.interactable = false;
            isSelected = false;
            IsCharacter = false;
            characterParent.SetActive(false);
            addParent.SetActive(false);
            lockedParent.SetActive(true);

            SetSelectedState(true);
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

            if (WebRequestManager.Singleton.TryGetCharacterAttributesInLookup(character._id.ToString(), out WebRequestManager.CharacterStats stats))
            {
                levelText.text = "Lv." + stats.level.ToString();
            }
            else
            {
                levelText.text = "Lv." + character.level.ToString();
            }

            SetSelectedState(true);
        }

        public void SetSelectedState(bool isNotSelected)
        {
            if (!IsCharacter) { return; }
            isSelected = !isNotSelected;
            Button.interactable = isNotSelected;
        }

        private void OnEnable()
        {
            selectedOverlay.color = isSelected ? glowOnColor : glowOffColor;
            selectedBorder.color = isSelected ? glowOnColor : glowOffColor;
            selectedGlow.color = isSelected ? glowOnColor : glowOffColor;
            transform.localScale = isSelected ? originalScale * selectedSizeMutliplier : originalScale;
        }

        private readonly static Color glowOnColor = new Color(1, 1, 1, 1);
        private readonly static Color glowOffColor = new Color(1, 1, 1, 0);

        private const float selectedSizeMutliplier = 1.1f;
        private const float selectedImageAnimationSpeed = 8;
        private bool isSelected;
        private void Update()
        {
            selectedOverlay.color = Color.Lerp(selectedOverlay.color, isSelected | pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            selectedBorder.color = Color.Lerp(selectedBorder.color, isSelected | pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            selectedGlow.color = Color.Lerp(selectedGlow.color, isSelected | pointerIsHoveringOnThisObject ? glowOnColor : glowOffColor, Time.deltaTime * selectedImageAnimationSpeed);
            transform.localScale = Vector3.Lerp(transform.localScale, isSelected | pointerIsHoveringOnThisObject ? originalScale * selectedSizeMutliplier : originalScale, Time.deltaTime * selectedImageAnimationSpeed);
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