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
            Vector2 sensitivity = Vector2.zero;
            if (CameraMotor.MAIN_MOTOR == null)
            {
                sensitivity = Camera.main.gameObject.GetComponent<SpectatorCamera>().sensitivity;
            }
            else
            {
                sensitivity = ((CameraMotorTypeAdventure)CameraMotor.MAIN_MOTOR.cameraMotorType).sensitivity.GetValue(gameObject);
            }

            mouseXSensitivityInput.text = sensitivity.x.ToString();
            mouseYSensitivityInput.text = sensitivity.y.ToString();
        }

        public void ChangeMouseSensitivity()
        {
            bool success = float.TryParse(mouseXSensitivityInput.text, out float mouseXSens);
            if (!success) return;
            success = float.TryParse(mouseYSensitivityInput.text, out float mouseYSens);
            if (!success) return;

            if (CameraMotor.MAIN_MOTOR == null)
            {
                Camera.main.gameObject.GetComponent<SpectatorCamera>().sensitivity = new Vector2(mouseXSens, mouseYSens);
            }
            else
            {
                ((CameraMotorTypeAdventure)CameraMotor.MAIN_MOTOR.cameraMotorType).sensitivity = new GameCreator.Variables.Vector2Property(new Vector2(mouseXSens, mouseYSens));
            }
        }
    }
}