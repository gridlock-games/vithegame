using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.UI;
using GameCreator.Camera;
using TMPro;

namespace LightPat.UI
{
    public class ControlSettingsMenu : Menu
    {
        public TMP_InputField mouseXSensitivityInput;
        public TMP_InputField mouseYSensitivityInput;

        private void Start()
        {
            Vector2 sensitivity = ((CameraMotorTypeAdventure)CameraMotor.MAIN_MOTOR.cameraMotorType).sensitivity.GetValue(gameObject);
            mouseXSensitivityInput.text = sensitivity.x.ToString();
            mouseYSensitivityInput.text = sensitivity.y.ToString();
        }

        public void ChangeMouseSensitivity()
        {
            bool success = float.TryParse(mouseXSensitivityInput.text, out float mouseXSens);
            if (!success) return;
            success = float.TryParse(mouseXSensitivityInput.text, out float mouseYSens);
            if (!success) return;

            ((CameraMotorTypeAdventure)CameraMotor.MAIN_MOTOR.cameraMotorType).sensitivity = new GameCreator.Variables.Vector2Property(new Vector2(mouseXSens, mouseYSens));
        }
    }
}