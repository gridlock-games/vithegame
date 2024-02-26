using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using Vi.Core;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Vi.UI
{
    public class DisplaySettingsMenu : Menu
    {
        [Header("Dropdowns")]
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown fullscreenModeDropdown;
        [Header("URP Settings")]
        public Slider renderScaleSlider;
        [Header("Buttons")]
        public Button applyChangesButton;
        public Button discardChangesButton;

        private UniversalRenderPipelineAsset pipeline;
        private FullScreenMode[] fsModes = new FullScreenMode[3];
        private List<Resolution> supportedResolutions = new List<Resolution>();

        private FullScreenMode originalFullScreenMode;
        private Resolution originalResolution;
        private float originalRenderScaleValue;
        private void Awake()
        {
            applyChangesButton.interactable = false;
            discardChangesButton.interactable = false;

            pipeline = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            renderScaleSlider.value = pipeline.renderScale;

            // Resolution Dropdown
            List<string> resolutionOptions = new List<string>();

            int currentResIndex = -1;
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                // If the resolution is 16:9
                // (Screen.resolutions[i].width * 9 / Screen.resolutions[i].height) == 16 & 
                if (Mathf.Abs(Screen.currentResolution.refreshRate - Screen.resolutions[i].refreshRate) < 3)
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

            SetOriginalVariables();
        }

        private void Update()
        {
            bool changesPresent = originalFullScreenMode != fsModes[fullscreenModeDropdown.value]
                //| originalQualityLevel != graphicsQualityDropdown.value
                | originalResolution.width != supportedResolutions[resolutionDropdown.value].width
                | originalResolution.height != supportedResolutions[resolutionDropdown.value].height
                | originalResolution.refreshRate != supportedResolutions[resolutionDropdown.value].refreshRate
                | originalRenderScaleValue != renderScaleSlider.value;

            applyChangesButton.interactable = changesPresent;
            discardChangesButton.interactable = changesPresent;
        }

        private void SetOriginalVariables()
        {
            originalFullScreenMode = Screen.fullScreenMode;
            originalResolution = supportedResolutions[resolutionDropdown.value];
            originalRenderScaleValue = pipeline.renderScale;
        }

        public void ApplyChanges()
        {
            // Fullscreen Dropdown
            FullScreenMode fsMode = fsModes[fullscreenModeDropdown.value];

            // Resolution Dropdown
            // Options are assigned automatically in OpenSettingsMenu()
            Resolution res = supportedResolutions[resolutionDropdown.value];

            //QualitySettings.SetQualityLevel(graphicsQualityDropdown.value, true);

            Screen.SetResolution(res.width, res.height, fsMode, res.refreshRate);

            pipeline.renderScale = renderScaleSlider.value;

            SetOriginalVariables();
        }

        public void DiscardChanges()
        {
            renderScaleSlider.value = pipeline.renderScale;

            fullscreenModeDropdown.value = Array.IndexOf(fsModes, originalFullScreenMode);
            //graphicsQualityDropdown.value = QualitySettings.GetQualityLevel();

            int currentResIndex = -1;
            List<string> resolutionOptions = new List<string>();
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                if (Mathf.Abs(Screen.currentResolution.refreshRate - Screen.resolutions[i].refreshRate) < 3)
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
            resolutionDropdown.value = currentResIndex;
        }
    }
}
