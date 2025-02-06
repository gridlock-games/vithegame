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
        [SerializeField] private TMP_Dropdown targetFrameRateDropdown;
        [SerializeField] private Slider dpiScaleSlider;
        [SerializeField] private Slider fieldOfViewSlider;
        [Header("Graphics Settings")]
        [SerializeField] private Toggle adaptivePerformanceToggle;
        [SerializeField] private TMP_Dropdown graphicsPresetDropdown;
        [SerializeField] private TMP_Dropdown shadowsDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Toggle hdrToggle;
        [SerializeField] private Toggle postProcessingToggle;
        [SerializeField] private GameObject shadowsWarning;
        [SerializeField] private GameObject graphicsRestartWarning;
        [Header("Action Buttons")]
        [SerializeField] private Button applyChangesButton;
        [SerializeField] private Button discardChangesButton;
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


        private int originalGraphicsDropdownValue;

        protected override void Awake()
        {
            base.Awake();
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

            // Resolution Dropdown
            List<string> resolutionOptions = new List<string>();

            int currentResIndex = -1;
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                // If the resolution is 16:9
                // (Screen.resolutions[i].width * 9 / Screen.resolutions[i].height) == 16 & 
                if (Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                {
                    resolutionOptions.Add(Screen.resolutions[i].ToString());
                    supportedResolutions.Add(Screen.resolutions[i]);
                }

                if (Screen.fullScreenMode == FullScreenMode.Windowed)
                {
                    if (Screen.resolutions[i].width == Screen.width & Screen.resolutions[i].height == Screen.height
                       & Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
                else
                {
                    if (Screen.resolutions[i].width == Screen.currentResolution.width & Screen.resolutions[i].height == Screen.currentResolution.height
                       & Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
            }

            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResIndex;

            targetFrameRateInput.text = FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate").ToString();

            // Full screen mode dropdown
            // Dropdown Options are assigned in inspector since these don't vary
            fsModes[0] = FullScreenMode.ExclusiveFullScreen;
            fsModes[1] = FullScreenMode.FullScreenWindow;
            fsModes[2] = FullScreenMode.Windowed;
            int fsModeIndex = Array.IndexOf(fsModes, Screen.fullScreenMode);
            fullscreenModeDropdown.value = fsModeIndex;

            fieldOfViewSlider.value = FasterPlayerPrefs.Singleton.GetFloat("FieldOfView");
            fieldOfViewSlider.onValueChanged.AddListener(SetFieldOfView);

            dpiScaleSlider.value = FasterPlayerPrefs.Singleton.GetFloat("DPIScalingFactor");
            dpiScaleSlider.GetComponent<SliderEndEditEvent>().EndDrag += SetDPIScale;

            // Graphics Quality dropdown
            graphicsPresetDropdown.AddOptions(QualitySettings.names.ToList());
            graphicsPresetDropdown.value = QualitySettings.GetQualityLevel();
            originalGraphicsDropdownValue = graphicsPresetDropdown.value;

            pipeline = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;

            hdrToggle.isOn = pipeline.supportsHDR;

            postProcessingToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");

            List<int> fpsOptionsAsInt = new List<int>();
            List<string> fpsOptions = new List<string>();
            for (int i = 30; i <= 120; i+=30)
            {
                fpsOptions.Add(i.ToString());
                fpsOptionsAsInt.Add(i);
            }

            targetFrameRateDropdown.ClearOptions();
            targetFrameRateDropdown.AddOptions(fpsOptions);

            int closestValue = fpsOptionsAsInt.Aggregate((x, y) => Math.Abs(x - FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate")) < Math.Abs(y - FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate")) ? x : y);
            int closestIndex = fpsOptionsAsInt.IndexOf(closestValue);
            
            targetFrameRateDropdown.value = closestIndex;

            targetFrameRateDropdown.onValueChanged.AddListener(ChangeTargetFrameRateFromDropdown);

            shadowsDropdown.ClearOptions();
            shadowsDropdown.AddOptions(shadowsOptions);

            // TODO Find shadows dropdown value
            int shadowsValue = 0;
            if (!pipeline.supportsMainLightShadows)
            {
                shadowsValue = 0;
            }
            else if (pipeline.shadowCascadeCount == 4)
            {
                shadowsValue = 3;
            }
            else if (pipeline.mainLightShadowmapResolution == 1024)
            {
                shadowsValue = 2;
            }
            else if (pipeline.mainLightShadowmapResolution == 256 && pipeline.shadowDistance > 0)
            {
                shadowsValue = 1;
            }

            shadowsDropdown.value = shadowsValue;
            shadowsDropdown.onValueChanged.AddListener((value) => RefreshWarningDisplays());

            RefreshWarningDisplays();
            SetOriginalVariables();
        }

        List<string> shadowsOptions = new List<string>()
        {
            "Off",
            "Low",
            "Medium",
            "High"
        };

        private void SetFieldOfView(float value)
        {
            FasterPlayerPrefs.Singleton.SetFloat("FieldOfView", value);
        }

        private void SetDPIScale(float sliderValue)
        {
            FasterPlayerPrefs.Singleton.SetFloat("DPIScalingFactor", sliderValue);
        }

        private void Update()
        {
            bool changesPresent = originalFullScreenMode != fsModes[fullscreenModeDropdown.value]
                | originalResolution.width != supportedResolutions[resolutionDropdown.value].width
                | originalResolution.height != supportedResolutions[resolutionDropdown.value].height
                | originalResolution.refreshRateRatio.value != supportedResolutions[resolutionDropdown.value].refreshRateRatio.value
                | originalGraphicsPreset != graphicsPresetDropdown.value
                | originalVSyncState != (vsyncToggle.isOn ? 1 : 0)
                | originalHDR != hdrToggle.isOn
                | originalPostProcessing != postProcessingToggle.isOn
                | originalShadowsState != shadowsDropdown.value;

            applyChangesButton.interactable = changesPresent;
            discardChangesButton.interactable = changesPresent;
        }

        // Display settings
        private FullScreenMode originalFullScreenMode;
        private Resolution originalResolution;
        // Graphics settings
        private int originalGraphicsPreset;
        private float originalRenderScaleValue;
        private int originalVSyncState;
        private bool originalHDR;
        private bool originalPostProcessing;
        private int originalShadowsState;
        private void SetOriginalVariables()
        {
            originalFullScreenMode = fsModes[fullscreenModeDropdown.value];
            originalResolution = supportedResolutions[resolutionDropdown.value];

            originalGraphicsPreset = graphicsPresetDropdown.value;
            originalRenderScaleValue = pipeline.renderScale;
            originalVSyncState = QualitySettings.vSyncCount;
            originalHDR = pipeline.supportsHDR;
            originalPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");

            originalShadowsState = shadowsDropdown.value;
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
                Screen.SetResolution(res.width, res.height, fsMode, res.refreshRateRatio);
            }

            // Graphics settings
            if (QualitySettings.GetQualityLevel() != graphicsPresetDropdown.value) { QualitySettings.SetQualityLevel(graphicsPresetDropdown.value, false); }
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
            pipeline.supportsHDR = hdrToggle.isOn;
            FasterPlayerPrefs.Singleton.SetBool("PostProcessingEnabled", postProcessingToggle.isOn);

            int renderDistance = 200;
            switch (graphicsPresetDropdown.value)
            {
                case 0:
                    renderDistance = 100;
                    break;
                case 1:
                    renderDistance = Application.isMobilePlatform ? 150 : 500;
                    break;
                case 2:
                    renderDistance = Application.isMobilePlatform ? 200 : 1000;
                    break;
                default:
                    Debug.LogWarning("Unsure what render distance to assign! " + graphicsPresetDropdown.value);
                    break;
            }
            FasterPlayerPrefs.Singleton.SetInt("RenderDistance", renderDistance);

            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                SetDPIScale(dpiScaleSlider.value);
            }

            int shadowsValue = shadowsDropdown.value;
            if (graphicsPresetDropdown.value == 0)
            {
                shadowsValue = 0;
                if (shadowsDropdown.value != shadowsValue)
                {
                    shadowsDropdown.value = shadowsValue;
                }
            }

            // Apply shadows quality
            switch (shadowsValue)
            {
                case 0: // Off
                    pipeline.mainLightShadowmapResolution = 256;
                    pipeline.additionalLightsShadowmapResolution = 256;

                    pipeline.shadowDistance = 0;
                    pipeline.shadowCascadeCount = 1;
                    pipeline.cascadeBorder = 5;
                    break;
                case 1: // Low
                    pipeline.mainLightShadowmapResolution = 256;
                    pipeline.additionalLightsShadowmapResolution = 256;

                    pipeline.shadowDistance = 50;
                    pipeline.shadowCascadeCount = 1;
                    pipeline.cascadeBorder = 5;
                    break;
                case 2: // Medium
                    pipeline.mainLightShadowmapResolution = 1024;
                    pipeline.additionalLightsShadowmapResolution = 512;

                    pipeline.shadowDistance = 50;
                    pipeline.shadowCascadeCount = 1;
                    pipeline.cascadeBorder = 5;
                    break;
                case 3: // High
                    pipeline.mainLightShadowmapResolution = 4096;
                    pipeline.additionalLightsShadowmapResolution = 4096;

                    pipeline.shadowDistance = 100;
                    pipeline.shadowCascadeCount = 4;
                    pipeline.cascade4Split = new Vector3(6.466667f, 10.13333f, 76.46667f);
                    pipeline.cascadeBorder = 23.53333f;
                    break;
                default:
                    Debug.LogWarning("Unsure what shadows quality to assign! " + shadowsDropdown.value);
                    break;
            }

            RefreshWarningDisplays();
            SetOriginalVariables();
        }

        public void DiscardChanges()
        {
            // Display settings
            pipeline = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

            fullscreenModeDropdown.value = Array.IndexOf(fsModes, originalFullScreenMode);

            int currentResIndex = -1;
            List<string> resolutionOptions = new List<string>();
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                if (Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                {
                    resolutionOptions.Add(Screen.resolutions[i].ToString());
                    supportedResolutions.Add(Screen.resolutions[i]);
                }

                if (Screen.fullScreenMode == FullScreenMode.Windowed)
                {
                    if (Screen.resolutions[i].width == Screen.width & Screen.resolutions[i].height == Screen.height
                       & Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
                else
                {
                    if (Screen.resolutions[i].width == Screen.currentResolution.width & Screen.resolutions[i].height == Screen.currentResolution.height
                       & Mathf.Abs((float)Screen.currentResolution.refreshRateRatio.value - (float)Screen.resolutions[i].refreshRateRatio.value) < 3)
                    {
                        currentResIndex = resolutionOptions.IndexOf(Screen.resolutions[i].ToString());
                    }
                }
            }
            resolutionDropdown.value = currentResIndex;

            // Graphics Settings
            graphicsPresetDropdown.value = QualitySettings.GetQualityLevel();
            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;
            hdrToggle.isOn = pipeline.supportsHDR;
            postProcessingToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");

            shadowsDropdown.value = originalShadowsState;
        }

        public void OnQualitySettingsDropdownChange()
        {
            pipeline = (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(graphicsPresetDropdown.value);
            
            vsyncToggle.isOn = QualitySettings.vSyncCount != 0;
            hdrToggle.isOn = graphicsPresetDropdown.value > 0;

            switch (graphicsPresetDropdown.value)
            {
                case 0:
                    shadowsDropdown.value = 0;
                    postProcessingToggle.isOn = true;
                    break;
                case 1:
                    shadowsDropdown.value = 1;
                    postProcessingToggle.isOn = true;
                    break;
                case 2:
                    shadowsDropdown.value = 2;
                    postProcessingToggle.isOn = true;
                    break;
                default:
                    Debug.LogWarning("Unsure what post processing values to assign! " + graphicsPresetDropdown.value);
                    break;
            }

            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                switch (graphicsPresetDropdown.value)
                {
                    case 0:
                        dpiScaleSlider.value = 0.5f;
                        break;
                    case 1:
                        dpiScaleSlider.value = 0.7f;
                        break;
                    case 2:
                        dpiScaleSlider.value = 1;
                        break;
                    default:
                        Debug.LogWarning("Unsure what dpi scaling to assign! " + graphicsPresetDropdown.value);
                        break;
                }
            }

            RefreshWarningDisplays();
        }

        private void RefreshWarningDisplays()
        {
            graphicsRestartWarning.SetActive(graphicsPresetDropdown.value != originalGraphicsDropdownValue);
            shadowsWarning.SetActive(graphicsPresetDropdown.value == 0 & shadowsDropdown.value > 0);
        }

        public void ValidateTargetFrameRate()
        {
            targetFrameRateInput.text = Regex.Replace(targetFrameRateInput.text, @"[^0-9]", "");
        }

        public void ChangeTargetFrameRateFromDropdown(int index)
        {
            int targetFrameRate = int.Parse(targetFrameRateDropdown.options[targetFrameRateDropdown.value].text);

            FasterPlayerPrefs.Singleton.SetInt("TargetFrameRate", targetFrameRate);
        }

        public void ChangeTargetFrameRate()
        {
            int targetFrameRate = int.Parse(targetFrameRateInput.text);

            if (targetFrameRate < 30)
            {
                targetFrameRate = 30;
                targetFrameRateInput.text = "30";
            }

            FasterPlayerPrefs.Singleton.SetInt("TargetFrameRate", targetFrameRate);
            //NetSceneManager.SetTargetFrameRate();
        }
    }
}
