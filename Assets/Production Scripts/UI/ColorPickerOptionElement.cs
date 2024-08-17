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
        [SerializeField] private Color defaultColor;
        [SerializeField] private Image colorPreview;

        public void Initialize(string playerPrefKey)
        {
            if (FasterPlayerPrefs.Singleton.HasColor(playerPrefKey))
            {

            }
            else
            {
                Debug.LogError(playerPrefKey + " is not present in player prefs!");
            }
        }

        public void SetColor(Color color)
        {
            colorPreview.color = color;
        }

        public void ResetColor()
        {

        }
    }
}