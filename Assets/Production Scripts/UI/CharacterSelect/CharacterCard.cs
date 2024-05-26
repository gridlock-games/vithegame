using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class CharacterCard : MonoBehaviour
    {
        [SerializeField] private GameObject characterParent;
        [SerializeField] private GameObject addParent;
        [SerializeField] private GameObject lockedParent;
        [SerializeField] private Image selectedImage;
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

        public void SetAsSelected()
        {
            if (!IsCharacter) { return; }
            isSelected = true;
        }

        private void OnEnable()
        {
            selectedImage.fillAmount = isSelected ? 1 : 0;
            rt.sizeDelta = isSelected ? originalSizeDelta * selectedSizeMutliplier : originalSizeDelta;
        }

        private const float selectedSizeMutliplier = 1.13f;
        private const float selectedImageAnimationSpeed = 4;
        private bool isSelected;
        private void Update()
        {
            selectedImage.fillAmount = Mathf.Lerp(selectedImage.fillAmount, isSelected ? 1 : 0, Time.deltaTime * selectedImageAnimationSpeed);
            rt.sizeDelta = Vector2.Lerp(rt.sizeDelta, isSelected ? originalSizeDelta * selectedSizeMutliplier : originalSizeDelta, Time.deltaTime * selectedImageAnimationSpeed);
        }
    }
}