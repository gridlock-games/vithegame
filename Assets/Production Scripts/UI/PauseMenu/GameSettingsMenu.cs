using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class GameSettingsMenu : Menu
    {
        [SerializeField] private Toggle consoleToggle;
        [SerializeField] private Toggle showFPSToggle;
        [SerializeField] private Toggle showPingToggle;

        private void SetPlayerPrefFromToggle(Toggle toggle, string playerPrefName)
        {
            PlayerPrefs.SetString(playerPrefName, toggle.isOn.ToString());
        }

        private void Start()
        {
            consoleToggle.isOn = bool.Parse(PlayerPrefs.GetString("ConsoleEnabled"));
            showFPSToggle.isOn = bool.Parse(PlayerPrefs.GetString("FPSEnabled"));
            showPingToggle.isOn = bool.Parse(PlayerPrefs.GetString("PingEnabled"));

            consoleToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(consoleToggle, "ConsoleEnabled"); });
            showFPSToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showFPSToggle, "FPSEnabled"); });
            showPingToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showPingToggle, "PingEnabled"); });
        }
    }
}