using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class GameSettingsMenu : Menu
    {
        [SerializeField] private Toggle autoAimToggle;
        [SerializeField] private Toggle consoleToggle;
        [SerializeField] private Toggle showFPSToggle;
        [SerializeField] private Toggle showPingToggle;
        [SerializeField] private Slider UIOpacitySlider;

        private void SetPlayerPrefFromToggle(Toggle toggle, string playerPrefName)
        {
            PersistentLocalObjects.Singleton.SetString(playerPrefName, toggle.isOn.ToString());
        }

        private void SetPlayerPrefFromSlider(Slider slider, string playerPrefName)
        {
            PersistentLocalObjects.Singleton.SetFloat(playerPrefName, slider.value);
        }

        private void Start()
        {
            autoAimToggle.isOn = bool.Parse(PersistentLocalObjects.Singleton.GetString("AutoAim"));
            consoleToggle.isOn = bool.Parse(PersistentLocalObjects.Singleton.GetString("ConsoleEnabled"));
            showFPSToggle.isOn = bool.Parse(PersistentLocalObjects.Singleton.GetString("FPSEnabled"));
            showPingToggle.isOn = bool.Parse(PersistentLocalObjects.Singleton.GetString("PingEnabled"));

            autoAimToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(autoAimToggle, "AutoAim"); });
            consoleToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(consoleToggle, "ConsoleEnabled"); });
            showFPSToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showFPSToggle, "FPSEnabled"); });
            showPingToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showPingToggle, "PingEnabled"); });

            UIOpacitySlider.value = PersistentLocalObjects.Singleton.GetFloat("UIOpacity");
            UIOpacitySlider.minValue = Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer ? 0.1f : 0;

            UIOpacitySlider.onValueChanged.AddListener(delegate { SetPlayerPrefFromSlider(UIOpacitySlider, "UIOpacity"); });
        }
    }
}