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
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectedOverlayImage;
        public Button Button;

        public bool Initialized { get; private set; }

        public void InitializeAsColor(Color color)
        {
            Initialized = true;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(125, 125);
            iconImage.sprite = materialSprite;
            iconImage.color = color;
        }

        public void InitializeAsMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            Initialized = true;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(125, 125);
            iconImage.sprite = materialSprite;
            iconImage.color = characterMaterial.averageTextureColor;
        }

        public void InitializeAsEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            Initialized = true;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(400, 225);
            iconImage.sprite = wearableEquipmentOption.GetIcon(raceAndGender);
            iconImage.color = Color.white;
        }

        public void InitializeAsRemoveEquipment()
        {
            Initialized = true;
            selectedOverlayImage.enabled = false;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(125, 125);
            iconImage.sprite = removeEquipmentSprite;
            iconImage.color = Color.white;
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