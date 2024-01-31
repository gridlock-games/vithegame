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
            if (NetworkManager.Singleton.LocalClient.PlayerObject)
            {
                movementHandler = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<MovementHandler>();

                mouseXSensitivityInput.text = movementHandler.GetLookSensitivity().x.ToString();
                mouseYSensitivityInput.text = movementHandler.GetLookSensitivity().y.ToString();
            }
            else if (PlayerPrefs.HasKey("MouseXSensitivity") & PlayerPrefs.HasKey("MouseYSensitivity"))
            {
                mouseXSensitivityInput.text = PlayerPrefs.GetFloat("MouseXSensitivity").ToString();
                mouseYSensitivityInput.text = PlayerPrefs.GetFloat("MouseYSensitivity").ToString();
            }
            else
            {
                mouseXSensitivityInput.text = "0.2";
                mouseYSensitivityInput.text = "0.2";
            }
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
            if (movementHandler) { movementHandler.SetLookSensitivity(new Vector2(mouseXSens, mouseYSens)); }
            
            PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens);
            PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens);
        }
    }
}