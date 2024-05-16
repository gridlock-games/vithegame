using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using Vi.Core;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class VideoSettingsMenu : Menu
    {
        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private InputField targetFrameRateInput;
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown graphicsPresetDropdown;
        [SerializeField] private Slider renderScaleSlider;
        [SerializeField] private TMP_Dropdown renderScalingModeDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown msaaDropdown;
        [SerializeField] private Toggle hdrToggle;
        [Header("Action Buttons")]
        [SerializeField] private Button applyChangesButton;
        [SerializeField] private Button discardChangesButton;
        [SerializeField] private Text fpsWarningText;

        private UniversalRenderPipelineAsset pipeline;
        private FullScreenMode[] fsModes = new FullScreenMode[3];
        private List<Resolution> supportedResolutions = new List<Resolution>();

        private void Awake()
        {
            applyChangesButton.interactable = false;
            discardChangesButton.interactable = false;

            fpsWarningText.text = "";

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

            targetFrameRateInput.text = Application.targetFrameRate.ToString();

            // Full screen mode dropdown
            // Dropdown Options are assigned in inspector since these don't vary
            fsModes[0] = FullScreenMode.ExclusiveFullScreen;
            fsModes[1] = FullScreenMode.FullScreenWindow;
            fsModes[2] = FullScreenMode.Windowed;
            int fsModeIndex = Array.IndexOf(fsModes, Screen.fullScreenMode);
            fullscreenModeDropdown.value = fsModeIndex;

            // Graphics Quality dropdown
            graphicsPresetDropdown.AddOptions(QualitySettings.names.ToList());
            graphicsPresetDropdown.value = QualitySettings.GetQualityLevel();

            pipeline = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            renderScaleSlider.value = pipeline.renderScale;

            renderScalingModeDropdown.ClearOptions();
            List<string> scalingModeOptions = new List<string>();
            foreach (UpscalingFilterSelection scalingMode in Enum.GetValues(typeof(UpscalingFilterSelection)))
            {
                switch (scalingMode)
                {
                    case UpscalingFilterSelection.Auto:
                        scalingModeOptions.Add("Automatic");
                        break;
                    case UpscalingFilterSelection.Linear:
                        scalingModeOptions.Add("Bilinear");
                        break;
                    case UpscalingFilterSelection.Point:
                        scalingModeOptions.Add("Nearest-Neighbor");
                        break;
                    case UpscalingFilterSelection.FSR:
                        scalingModeOptions.Add("FidelityFX Super Resolution 1.0");
                        break;
                }
            }
            renderScalingModeDropdown.AddOptions(scalingModeOptions);
            renderScalingModeDropdown.value = (int)pipeline.upscalingFilter;

            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;

            msaaDropdown.AddOptions(msaaCrosswalk.Keys.ToList());
            msaaDropdown.value = msaaCrosswalk.Keys.ToList().IndexOf(msaaCrosswalk.FirstOrDefault(x => x.Value == pipeline.msaaSampleCount).Key);

            hdrToggle.isOn = pipeline.supportsHDR;

            SetOriginalVariables();
        }

        private Dictionary<string, int> msaaCrosswalk = new Dictionary<string, int>()
        {
            { "Disabled", 0 },
            { "1x", 1 },
            { "2x", 2 },
            { "4x", 4 },
            { "8x", 8 }
        };

        private void Update()
        {
            bool changesPresent = originalFullScreenMode != fsModes[fullscreenModeDropdown.value]
                | originalResolution.width != supportedResolutions[resolutionDropdown.value].width
                | originalResolution.height != supportedResolutions[resolutionDropdown.value].height
                | originalResolution.refreshRate != supportedResolutions[resolutionDropdown.value].refreshRate
                | originalGraphicsPreset != graphicsPresetDropdown.value
                | originalRenderScaleValue != renderScaleSlider.value
                | originalScalingFilter != (UpscalingFilterSelection)renderScalingModeDropdown.value
                | originalVSyncState != (vsyncToggle.isOn ? 1 : 0)
                | originalMSAASampleCount != msaaCrosswalk[msaaDropdown.options[msaaDropdown.value].text]
                | originalHDR != hdrToggle.isOn;

            applyChangesButton.interactable = changesPresent;
            discardChangesButton.interactable = changesPresent;
        }

        // Display settings
        private FullScreenMode originalFullScreenMode;
        private Resolution originalResolution;
        // Graphics settings
        private int originalGraphicsPreset;
        private float originalRenderScaleValue;
        private UpscalingFilterSelection originalScalingFilter;
        private int originalVSyncState;
        private int originalMSAASampleCount;
        private bool originalHDR;
        private void SetOriginalVariables()
        {
            originalFullScreenMode = fsModes[fullscreenModeDropdown.value];
            originalResolution = supportedResolutions[resolutionDropdown.value];

            originalGraphicsPreset = graphicsPresetDropdown.value;
            originalRenderScaleValue = pipeline.renderScale;
            originalScalingFilter = pipeline.upscalingFilter;
            originalVSyncState = QualitySettings.vSyncCount;
            originalMSAASampleCount = pipeline.msaaSampleCount;
            originalHDR = pipeline.supportsHDR;
        }

        public void ApplyChanges()
        {
            // Display settings
            // Fullscreen Dropdown
            FullScreenMode fsMode = fsModes[fullscreenModeDropdown.value];

            // Resolution Dropdown
            // Options are assigned automatically in OpenSettingsMenu()
            if (supportedResolutions.Count > 1)
            {
                Resolution res = supportedResolutions[resolutionDropdown.value];
                Screen.SetResolution(res.width, res.height, fsMode, res.refreshRate);
            }

            // Graphics settings
            if (QualitySettings.GetQualityLevel() != graphicsPresetDropdown.value) { QualitySettings.SetQualityLevel(graphicsPresetDropdown.value, true); }
            pipeline.renderScale = renderScaleSlider.value;
            pipeline.upscalingFilter = (UpscalingFilterSelection)renderScalingModeDropdown.value;
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
            pipeline.msaaSampleCount = msaaCrosswalk[msaaDropdown.options[msaaDropdown.value].text];
            pipeline.supportsHDR = hdrToggle.isOn;

            vsyncToggle.interactable = true;

            SetOriginalVariables();

            fpsWarningText.text = "IF THE GAME FEELS CHOPPY AFTER CHANGES, RESTART YOUR GAME";
        }

        public void DiscardChanges()
        {
            // Display settings
            fullscreenModeDropdown.value = Array.IndexOf(fsModes, originalFullScreenMode);

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

            // Graphics Settings
            graphicsPresetDropdown.value = QualitySettings.GetQualityLevel();
            renderScaleSlider.value = pipeline.renderScale;
            renderScalingModeDropdown.value = (int)pipeline.upscalingFilter;
            vsyncToggle.interactable = true;
            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;
            msaaDropdown.value = msaaCrosswalk.Keys.ToList().IndexOf(msaaCrosswalk.FirstOrDefault(x => x.Value == pipeline.msaaSampleCount).Key);
            hdrToggle.isOn = pipeline.supportsHDR;
        }

        public void OnQualitySettingsDropdownChange()
        {
            UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(graphicsPresetDropdown.value);
            
            renderScaleSlider.value = pipeline.renderScale;
            renderScalingModeDropdown.value = (int)pipeline.upscalingFilter;
            vsyncToggle.interactable = QualitySettings.GetQualityLevel() == graphicsPresetDropdown.value;
            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;
            msaaDropdown.value = msaaCrosswalk.Keys.ToList().IndexOf(msaaCrosswalk.FirstOrDefault(x => x.Value == pipeline.msaaSampleCount).Key);
            hdrToggle.isOn = pipeline.supportsHDR;
        }

        public void ValidateTargetFrameRate()
        {
            targetFrameRateInput.text = Regex.Replace(targetFrameRateInput.text, @"[^0-9]", "");
        }

        public void ChangeTargetFrameRate()
        {
            int targetFrameRate = int.Parse(targetFrameRateInput.text);

            if (targetFrameRate < 30)
            {
                targetFrameRate = 30;
                targetFrameRateInput.text = "30";
            }

            Application.targetFrameRate = targetFrameRate;
            PersistentLocalObjects.Singleton.SetInt("TargetFrameRate", targetFrameRate);
        }
    }
}
