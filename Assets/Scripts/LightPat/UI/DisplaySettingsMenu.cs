using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using LightPat.Core;

namespace LightPat.UI
{
    public class DisplaySettingsMenu : Menu
    {
        [Header("Dropdowns")]
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown fullscreenModeDropdown;
        public TMP_Dropdown graphicsQualityDropdown;
        [Header("Config Labels")]
        public TextMeshProUGUI currentResolutionDisplay;
        public TextMeshProUGUI currentFullscreenModeDisplay;
        public TextMeshProUGUI currentGraphicsQualityDisplay;

        private FullScreenMode[] fsModes = new FullScreenMode[3];
        private List<Resolution> supportedResolutions = new List<Resolution>();

        private void Start()
        {
            // Resolution Dropdown
            List<string> resolutionOptions = new List<string>();

            int currentResIndex = 0;
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                // If the resolution is 16:9
                if ((Screen.resolutions[i].width * 9 / Screen.resolutions[i].height) == 16 & Mathf.Abs(Screen.currentResolution.refreshRate - Screen.resolutions[i].refreshRate) < 3)
                {
                    resolutionOptions.Add(Screen.resolutions[i].ToString());
                    supportedResolutions.Add(Screen.resolutions[i]);
                }

                if (Screen.fullScreenMode == FullScreenMode.Windowed)
                {
                    if (Screen.resolutions[i].width == Screen.width & Screen.resolutions[i].height == Screen.height
                       & Mathf.Abs(Screen.currentResolution.refreshRate - Screen.resolutions[i].refreshRate) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
                else
                {
                    if (Screen.resolutions[i].width == Screen.currentResolution.width & Screen.resolutions[i].height == Screen.currentResolution.height
                       & Mathf.Abs(Screen.currentResolution.refreshRate - Screen.resolutions[i].refreshRate) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
            }

            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResIndex;

            // Full screen mode dropdown
            // Dropdown Options are assigned in inspector since these don't vary
            fsModes[0] = FullScreenMode.ExclusiveFullScreen;
            fsModes[1] = FullScreenMode.FullScreenWindow;
            fsModes[2] = FullScreenMode.Windowed;
            int fsModeIndex = Array.IndexOf(fsModes, Screen.fullScreenMode);
            fullscreenModeDropdown.value = fsModeIndex;

            // Graphics Quality dropdown
            graphicsQualityDropdown.AddOptions(QualitySettings.names.ToList());
            graphicsQualityDropdown.value = QualitySettings.GetQualityLevel();

            // Display Current Config
            currentResolutionDisplay.SetText("Current Resolution: " + resolutionDropdown.options[currentResIndex].text);
            currentFullscreenModeDisplay.SetText("Current Fullscreen Mode: " + fullscreenModeDropdown.options[fsModeIndex].text);
            currentGraphicsQualityDisplay.SetText("Current Graphics Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }

        public void ApplyChanges()
        {
            // Fullscreen Dropdown
            FullScreenMode fsMode = fsModes[fullscreenModeDropdown.value];

            // Resolution Dropdown
            // Options are assigned automatically in OpenSettingsMenu()
            Resolution res = supportedResolutions[resolutionDropdown.value];

            QualitySettings.SetQualityLevel(graphicsQualityDropdown.value, true);

            Screen.SetResolution(res.width, res.height, fsMode, res.refreshRate);

            currentResolutionDisplay.SetText("Current Resolution: " + resolutionDropdown.options[resolutionDropdown.value].text);
            currentFullscreenModeDisplay.SetText("Current Fullscreen Mode: " + fullscreenModeDropdown.options[fullscreenModeDropdown.value].text);
            currentGraphicsQualityDisplay.SetText("Current Graphics Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }
    }
}
