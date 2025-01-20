using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class CharacterCustomizationButton : MonoBehaviour
    {
        [SerializeField] private Sprite materialSprite;
        [SerializeField] private Sprite removeEquipmentSprite;
        [SerializeField] private Sprite resetSprite;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectedOverlayImage;
        public Button Button;

        public bool Initialized { get; private set; }

        public bool IsPreview { get; private set; }

        public void InitializeAsColor(Color color, bool isPreview = false)
        {
            IsPreview = isPreview;
            Initialized = true;
            iconImage.sprite = materialSprite;
            iconImage.color = color;
        }

        public void InitializeAsMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            Initialized = true;
            iconImage.sprite = materialSprite;
            iconImage.color = characterMaterial.averageTextureColor;
        }

        public void InitializeAsEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            Initialized = true;
            iconImage.sprite = wearableEquipmentOption.GetIcon(raceAndGender);
            iconImage.color = Color.white;
        }

        public void InitializeAsRemoveEquipment()
        {
            Initialized = true;
            selectedOverlayImage.enabled = false;
            iconImage.sprite = removeEquipmentSprite;
            iconImage.color = Color.white;
        }

        public void InitializeAsResetButton()
        {
            Initialized = true;
            selectedOverlayImage.enabled = false;
            iconImage.sprite = resetSprite;
            iconImage.color = Color.white;
            iconImage.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        }

        private Canvas canvas;
        private void Start()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = Initialized;
        }

        private bool isSelected;
        public void SetSelectedState(bool isNotSelected)
        {
            isSelected = !isNotSelected;
            Button.interactable = isNotSelected;
        }

        private const float selectedOverlayImageTransitionSpeed = 8;
        private void Update()
        {
            canvas.enabled = Initialized;
            selectedOverlayImage.fillAmount = Mathf.Lerp(selectedOverlayImage.fillAmount, isSelected ? 1 : 0, Time.deltaTime * selectedOverlayImageTransitionSpeed);
        }
    }
}