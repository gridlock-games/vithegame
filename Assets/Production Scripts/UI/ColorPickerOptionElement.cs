using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColorPickerUI;
using Vi.Utility;

namespace Vi.Core
{
    public class ColorPickerOptionElement : MonoBehaviour
    {
        [SerializeField] private Image colorPreview;
        [SerializeField] private GameObject colorPickerPrefab;
        [SerializeField] private string playerPrefKey;

        private bool initialized;
        private Canvas canvas;
        private void OnEnable()
        {
            canvas = GetComponentInParent<Canvas>();
            if (FasterPlayerPrefs.Singleton.HasColor(playerPrefKey))
            {
                initialized = true;
                SetColor(FasterPlayerPrefs.Singleton.GetColor(playerPrefKey));
            }
            else
            {
                Debug.LogError(playerPrefKey + " is not present in player prefs! " + name);
            }
        }

        private void OnDisable()
        {
            if (colorPickerInstance) { Destroy(colorPickerInstance.gameObject); }
        }

        private void Update()
        {
            if (!initialized) { return; }
            if (colorPickerInstance)
            {
                SetColor(colorPickerInstance.color);
            }
        }

        private ColorPicker colorPickerInstance;
        public void OpenColorPicker()
        {
            if (!initialized) { return; }
            colorPickerInstance = Instantiate(colorPickerPrefab, canvas.transform).GetComponentInChildren<ColorPicker>();
            colorPickerInstance.color = colorPreview.color;
        }

        private void SetColor(Color color)
        {
            colorPreview.color = color;
            FasterPlayerPrefs.Singleton.SetColor(playerPrefKey, color);
        }

        public void ResetColor()
        {
            if (!initialized) { return; }
            SetColor(FasterPlayerPrefs.GetDefaultColor(playerPrefKey));
        }
    }
}