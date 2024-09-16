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
using Vi.Utility;
using Vi.Player;

namespace Vi.UI
{
    public class VideoSettingsMenu : Menu
    {
        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private InputField targetFrameRateInput;
        [SerializeField] private Slider dpiScaleSlider;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private InputField renderDistanceInput;
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown graphicsPresetDropdown;
        [SerializeField] private Slider renderScaleSlider;
        [SerializeField] private TMP_Dropdown renderScalingModeDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown msaaDropdown;
        [SerializeField] private Toggle hdrToggle;
        [SerializeField] private Toggle postProcessingToggle;
        [Header("Action Buttons")]
        [SerializeField] private Button applyChangesButton;
        [SerializeField] private Button discardChangesButton;
        [SerializeField] private Text fpsWarningText;
        [Header("Display Settings Resizing By Platform")]
        //[SerializeField] private GridLayoutGroup scrollGrid;
        [SerializeField] private RectTransform displaySettingsGroup;
        [SerializeField] private GameObject fullScreenModeElement;
        [SerializeField] private GameObject resolutionElement;
        [SerializeField] private GameObject dpiScalingElement;

        private UniversalRenderPipelineAsset pipeline;
        private FullScreenMode[] fsModes = new FullScreenMode[3];
        private List<Resolution> supportedResolutions = new List<Resolution>();

        private static readonly List<RuntimePlatform> platformsToAllowResolutionChangesOn = new List<RuntimePlatform>()
        {
            RuntimePlatform.WindowsPlayer,
            RuntimePlatform.OSXPlayer,
            RuntimePlatform.LinuxPlayer,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.OSXEditor,
            RuntimePlatform.LinuxEditor
        };

        private void Awake()
        {
            if (platformsToAllowResolutionChangesOn.Contains(Application.platform))
            {
                dpiScalingElement.SetActive(false);

                displaySettingsGroup.sizeDelta = new Vector2(displaySettingsGroup.sizeDelta.x, displaySettingsGroup.sizeDelta.y - 125);

                foreach (Transform child in displaySettingsGroup.parent)
                {
                    RectTransform rt = (RectTransform)child;
                    if (rt == displaySettingsGroup) { continue; }
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + 125);
                }
            }
            else
            {
                fullScreenModeElement.SetActive(false);
                resolutionElement.SetActive(false);

                //scrollGrid.cellSize = new Vector2(scrollGrid.cellSize.x, scrollGrid.cellSize.y - 125 * 2);
                displaySettingsGroup.sizeDelta = new Vector2(displaySettingsGroup.sizeDelta.x, displaySettingsGroup.sizeDelta.y - 125 * 2);

                foreach (Transform child in displaySettingsGroup.parent)
                {
                    RectTransform rt = (RectTransform)child;
                    if (rt == displaySettingsGroup) { continue; }
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + 125 * 2);
                }
            }

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
            renderDistanceInput.text = FasterPlayerPrefs.Singleton.GetInt("RenderDistance").ToString();

            // Full screen mode dropdown
            // Dropdown Options are assigned in inspector since these don't vary
            fsModes[0] = FullScreenMode.ExclusiveFullScreen;
            fsModes[1] = FullScreenMode.FullScreenWindow;
            fsModes[2] = FullScreenMode.Windowed;
            int fsModeIndex = Array.IndexOf(fsModes, Screen.fullScreenMode);
            fullscreenModeDropdown.value = fsModeIndex;

            fieldOfViewSlider.value = FasterPlayerPrefs.Singleton.GetFloat("FieldOfView");
            fieldOfViewSlider.onValueChanged.AddListener(SetFieldOfView);

            dpiScaleSlider.value = QualitySettings.resolutionScalingFixedDPIFactor;
            dpiScaleSlider.GetComponent<SliderEndEditEvent>().EndDrag += SetDPIScale;

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

            postProcessingToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");

            SetOriginalVariables();
        }

        private void SetFieldOfView(float value)
        {
            FasterPlayerPrefs.Singleton.SetFloat("FieldOfView", value);
        }

        private void SetDPIScale(float sliderValue)
        {
            QualitySettings.resolutionScalingFixedDPIFactor = sliderValue;
            FasterPlayerPrefs.Singleton.SetFloat("DPIScalingFactor", sliderValue);
        }

        private readonly Dictionary<string, int> msaaCrosswalk = new Dictionary<string, int>()
        {
            { "Disabled", 1 },
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
                | originalHDR != hdrToggle.isOn
                | originalPostProcessing != postProcessingToggle.isOn;

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
        private bool originalPostProcessing;
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
            originalPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
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
            FasterPlayerPrefs.Singleton.SetBool("PostProcessingEnabled", postProcessingToggle.isOn);

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
            postProcessingToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
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
            postProcessingToggle.isOn = graphicsPresetDropdown.value > 0;
        }

        public void ValidateRenderDistance()
        {
            renderDistanceInput.text = Regex.Replace(renderDistanceInput.text, @"[^0-9]", "");
        }

        public void ChangeRenderDistance()
        {
            int renderDistance = int.Parse(renderDistanceInput.text);
            if (renderDistance < 10)
            {
                renderDistance = 10;
                renderDistanceInput.text = "10";
            }
            FasterPlayerPrefs.Singleton.SetInt("RenderDistance", renderDistance);
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
            FasterPlayerPrefs.Singleton.SetInt("TargetFrameRate", targetFrameRate);
        }
    }
}
