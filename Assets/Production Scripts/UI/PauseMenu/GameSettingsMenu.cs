using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.Utility;
using TMPro;
using Unity.Netcode;
using Vi.Player;

namespace Vi.UI
{
    public class GameSettingsMenu : Menu
    {
        [SerializeField] private Toggle autoAimToggle;
        [SerializeField] private Toggle consoleToggle;
        [SerializeField] private Toggle showFPSToggle;
        [SerializeField] private Toggle showPingToggle;
        [SerializeField] private Toggle showPacketLossToggle;
        [SerializeField] private Toggle showJitterToggle;
        [SerializeField] private Slider UIOpacitySlider;
        [SerializeField] private Toggle showTextInWorldSpaceLabelsToggle;
        [SerializeField] private GameObject channelDropdownParent;
        [SerializeField] private TMP_Dropdown channelDropdown;

        private void SetPlayerPrefFromToggle(Toggle toggle, string playerPrefName)
        {
            FasterPlayerPrefs.Singleton.SetBool(playerPrefName, toggle.isOn);
        }

        private void SetPlayerPrefFromSlider(Slider slider, string playerPrefName)
        {
            FasterPlayerPrefs.Singleton.SetFloat(playerPrefName, slider.value);
        }

        private void Awake()
        {
            autoAimToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
            consoleToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("ConsoleEnabled");
            showFPSToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("FPSEnabled");
            showPingToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");
            showPacketLossToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PacketLossEnabled");
            showJitterToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("JitterEnabled");
            showTextInWorldSpaceLabelsToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("ShowHPTextInWorldSpaceLabels");

            autoAimToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(autoAimToggle, "AutoAim"); });
            consoleToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(consoleToggle, "ConsoleEnabled"); });
            showFPSToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showFPSToggle, "FPSEnabled"); });
            showPingToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showPingToggle, "PingEnabled"); });
            showPacketLossToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showPacketLossToggle, "PacketLossEnabled"); });
            showJitterToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showJitterToggle, "JitterEnabled"); });
            showTextInWorldSpaceLabelsToggle.onValueChanged.AddListener(delegate { SetPlayerPrefFromToggle(showTextInWorldSpaceLabelsToggle, "ShowHPTextInWorldSpaceLabels"); });

            UIOpacitySlider.value = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            UIOpacitySlider.minValue = Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer ? 0.1f : 0;

            UIOpacitySlider.onValueChanged.AddListener(delegate { SetPlayerPrefFromSlider(UIOpacitySlider, "UIOpacity"); });
        }

        private void Start()
        {
            if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.Singleton.LocalClientId)
                & PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None
                & !NetworkManager.Singleton.IsServer)
            {
                channelDropdownParent.SetActive(true);

                channelDropdown.ClearOptions();
                List<string> channelOptions = new List<string>();
                List<int> channelCountList = PlayerDataManager.Singleton.GetChannelCountList();
                for (int i = 0; i < channelCountList.Count; i++)
                {
                    channelOptions.Add("Channel " + (i + 1).ToString() + " - " + channelCountList[i].ToString() + " Players");
                }
                channelDropdown.AddOptions(channelOptions);
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData((int)NetworkManager.Singleton.LocalClientId);
                channelDropdown.value = playerData.channel;
                channelDropdown.onValueChanged.AddListener(delegate { OnChannelDropdownChange(); });
            }
            else
            {
                channelDropdownParent.SetActive(false);
            }
        }

        private void Update()
        {
            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame)
            {
                if (channelDropdown.gameObject.activeInHierarchy)
                {
                    channelDropdown.ClearOptions();
                    List<string> channelOptions = new List<string>();
                    List<int> channelCountList = PlayerDataManager.Singleton.GetChannelCountList();
                    for (int i = 0; i < channelCountList.Count; i++)
                    {
                        channelOptions.Add("Channel " + (i + 1).ToString() + " - " + channelCountList[i].ToString() + " Players");
                    }
                    channelDropdown.AddOptions(channelOptions);
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData((int)NetworkManager.Singleton.LocalClientId);
                    channelDropdown.SetValueWithoutNotify(playerData.channel);
                }
            }
        }

        private void OnChannelDropdownChange()
        {
            var playerData = PlayerDataManager.Singleton.GetPlayerData((int)NetworkManager.Singleton.LocalClientId);
            playerData.channel = channelDropdown.value;
            PlayerDataManager.Singleton.SetPlayerData(playerData);
        }
    }
}