using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        [SerializeField] private InputField mouseXSensitivityInput;
        [SerializeField] private InputField mouseYSensitivityInput;
        [SerializeField] private Toggle invertLookToggle;

        private void Start()
        {
            mouseXSensitivityInput.text = PlayerPrefs.GetFloat("MouseXSensitivity").ToString();
            mouseYSensitivityInput.text = PlayerPrefs.GetFloat("MouseYSensitivity").ToString();
            invertLookToggle.isOn = bool.Parse(PlayerPrefs.GetString("InvertMouse"));
            ChangeMouseSensitivity();
        }

        public void ChangeMouseSensitivity()
        {
            bool success = float.TryParse(mouseXSensitivityInput.text, out float mouseXSens);
            if (!success)
            {
                mouseXSensitivityInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
                return;
            }
            success = float.TryParse(mouseYSensitivityInput.text, out float mouseYSens);
            if (!success)
            {
                mouseYSensitivityInput.text = Regex.Replace(mouseYSensitivityInput.text, @"[^0-9|.]", "");
                return;
            }
            
            PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens);
            PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens);
        }

        public void SetInvertMouse()
        {
            PlayerPrefs.SetString("InvertMouse", invertLookToggle.isOn.ToString());
        }
    }
}