using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        [SerializeField] private Toggle invertLookToggle;
        [SerializeField] private InputField mouseXSensitivityInput;
        [SerializeField] private InputField mouseYSensitivityInput;
        [SerializeField] private InputField zoomMultiplierInput;

        private void Start()
        {
            mouseXSensitivityInput.text = PlayerPrefs.GetFloat("MouseXSensitivity").ToString();
            mouseYSensitivityInput.text = PlayerPrefs.GetFloat("MouseYSensitivity").ToString();
            invertLookToggle.isOn = bool.Parse(PlayerPrefs.GetString("InvertMouse"));
            ChangeMouseSensitivity();
        }

        public void SetInvertMouse()
        {
            PlayerPrefs.SetString("InvertMouse", invertLookToggle.isOn.ToString());
        }

        public void ChangeMouseSensitivity()
        {
            mouseXSensitivityInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
            mouseYSensitivityInput.text = Regex.Replace(mouseYSensitivityInput.text, @"[^0-9|.]", "");

            float mouseXSens = float.Parse(mouseXSensitivityInput.text);
            float mouseYSens = float.Parse(mouseYSensitivityInput.text);
            
            PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens);
            PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens);
        }

        public void ChangeZoomMultiplier()
        {
            zoomMultiplierInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");

            float zoomMultiplier = float.Parse(zoomMultiplierInput.text);

            PlayerPrefs.SetFloat("ZoomSensitivityMultiplier", zoomMultiplier);
        }
    }
}