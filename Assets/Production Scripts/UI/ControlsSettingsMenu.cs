using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;
using System.Collections.Generic;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        [SerializeField] private Toggle invertLookToggle;
        [SerializeField] private InputField mouseXSensitivityInput;
        [SerializeField] private InputField mouseYSensitivityInput;
        [SerializeField] private InputField zoomMultiplierInput;
        [SerializeField] private TMP_Dropdown zoomModeDropdown;

        private List<string> holdToggleOptions = new List<string>() { "HOLD", "TOGGLE" };

        private void Start()
        {
            invertLookToggle.isOn = bool.Parse(PlayerPrefs.GetString("InvertMouse"));
            mouseXSensitivityInput.text = PlayerPrefs.GetFloat("MouseXSensitivity").ToString();
            mouseYSensitivityInput.text = PlayerPrefs.GetFloat("MouseYSensitivity").ToString();
            zoomMultiplierInput.text = PlayerPrefs.GetFloat("ZoomSensitivityMultiplier").ToString();

            zoomModeDropdown.AddOptions(holdToggleOptions);
            zoomModeDropdown.value = holdToggleOptions.IndexOf(PlayerPrefs.GetString("ZoomMode"));
        }

        public void SetInvertMouse()
        {
            PlayerPrefs.SetString("InvertMouse", invertLookToggle.isOn.ToString());
        }

        public void ChangeMouseXSensitivity()
        {
            mouseXSensitivityInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
            float mouseXSens = float.Parse(mouseXSensitivityInput.text);
            PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens);
        }

        public void ChangeMouseYSensitivity()
        {
            mouseYSensitivityInput.text = Regex.Replace(mouseYSensitivityInput.text, @"[^0-9|.]", "");
            float mouseYSens = float.Parse(mouseYSensitivityInput.text);
            PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens);
        }

        public void ChangeZoomMultiplier()
        {
            zoomMultiplierInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
            float zoomMultiplier = float.Parse(zoomMultiplierInput.text);
            PlayerPrefs.SetFloat("ZoomSensitivityMultiplier", zoomMultiplier);
        }

        public void ChangeZoomMode()
        {
            PlayerPrefs.SetString("ZoomMode", holdToggleOptions[zoomModeDropdown.value]);
        }
    }
}