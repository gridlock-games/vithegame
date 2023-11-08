using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;
using UnityEngine.UI;
using Unity.Netcode;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        public InputField mouseXSensitivityInput;
        public InputField mouseYSensitivityInput;

        private MovementHandler movementHandler;

        private void Start()
        {
            movementHandler = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<MovementHandler>();

            mouseXSensitivityInput.text = movementHandler.GetLookSensitivity().x.ToString();
            mouseYSensitivityInput.text = movementHandler.GetLookSensitivity().y.ToString();
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
            movementHandler.SetLookSensitivity(new Vector2(mouseXSens, mouseYSens));
            PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens);
            PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens);
        }
    }
}