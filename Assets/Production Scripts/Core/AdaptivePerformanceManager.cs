using UnityEngine;
using UnityEngine.AdaptivePerformance;
using Vi.Utility;
using UnityEngine.Events;
using System.Collections;

namespace Vi.Core
{
    public class AdaptivePerformanceManager : MonoBehaviour
    {
        public static AdaptivePerformanceManager Singleton
        {
            get
            {
                if (_singleton == null) { Debug.LogError("Adaptive Performance Manager is null"); }
                return _singleton;
            }
        }

        private static AdaptivePerformanceManager _singleton;

        private void Awake()
        {
            _singleton = this;
        }

        private IAdaptivePerformance ap = null;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(RefreshStatusAfter1Frame());

            ap = Holder.Instance;
            if (ap != null)
            {
                if (!ap.Active)
                {
                    Debug.LogWarning("Adapative Performance is Disabled!");
                    return;
                }

                ap.ThermalStatus.ThermalEvent += OnThermalEvent;
                ap.PerformanceStatus.PerformanceBottleneckChangeEvent += PerformanceStatus_PerformanceBottleneckChangeEvent;
            }
        }

        private void OnEnable()
        {
            QualitySettings.activeQualityLevelChanged += QualitySettings_activeQualityLevelChanged;
            EventDelegateManager.sceneUnloaded += OnSceneUnload;

            QualitySettings_activeQualityLevelChanged(0, QualitySettings.GetQualityLevel());
        }

        private void OnDisable()
        {
            QualitySettings.activeQualityLevelChanged -= QualitySettings_activeQualityLevelChanged;
            EventDelegateManager.sceneUnloaded -= OnSceneUnload;
        }

        private float DPIScale;
        private bool adaptivePerformanceEnabled;
        private int targetFrameRate;

        private IEnumerator RefreshStatusAfter1Frame()
        {
            yield return null;
            RefreshStatus();
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private void RefreshStatus()
        {
            bool previousAdaptivePerformanceState = adaptivePerformanceEnabled;
            adaptivePerformanceEnabled = FasterPlayerPrefs.Singleton.GetBool("EnableAdaptivePerformance");
            DPIScale = FasterPlayerPrefs.Singleton.GetFloat("DPIScalingFactor");
            targetFrameRate = FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate");

            if (!adaptivePerformanceEnabled & previousAdaptivePerformanceState)
            {
                QualitySettings.lodBias = maxLODBias;
                QualitySettings.globalTextureMipmapLimit = 0;

                // Don't set these cause we're in a menu
                //QualitySettings.resolutionScalingFixedDPIFactor = DPIScale;
                //NetSceneManager.SetTargetFrameRate();
            }

            if (adaptivePerformanceEnabled)
            {
                RefreshThermalSettings();
            }
        }

        public void RefreshThermalSettings()
        {
            OnThermalEvent(lastEV);
        }

        private void PerformanceStatus_PerformanceBottleneckChangeEvent(PerformanceBottleneckChangeEventArgs bottleneckEventArgs)
        {
            Debug.Log("Performance Bottleneck Changed " + bottleneckEventArgs.PerformanceBottleneck);
        }

        private void QualitySettings_activeQualityLevelChanged(int previousQuality, int currentQuality)
        {
            Debug.Log($"Quality Level has been changed from {QualitySettings.names[previousQuality]} to {QualitySettings.names[currentQuality]}");

            switch (currentQuality)
            {
                case 0:
                    maxLODBias = 1;
                    break;
                case 1:
                    maxLODBias = 1.5f;
                    break;
                case 2:
                    maxLODBias = 2;
                    break;
                default:
                    Debug.LogWarning("I don't know how to set the max LOD bias! " + currentQuality);
                    break;
            }
        }

        private float maxLODBias = 1;

        [Header("Adaptive Performance, X is 1 - temperature level")]
        [Header("Left is hot, Right is cold")]
        [SerializeField] private AnimationCurve DPIScalingCurve;
        [SerializeField] private AnimationCurve LODBiasCurve;
        [SerializeField] private AnimationCurve audioCullingDistanceCurve;

        public static WarningLevel thermalWarningLevel { get; private set; } = WarningLevel.NoWarning;
        public static ThermalMetrics lastEV { get; private set; }
        void OnThermalEvent(ThermalMetrics ev)
        {
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                Debug.Log("Thermal Warning Level: " + ev.WarningLevel);
                Debug.Log("Temperature Level: " + ev.TemperatureLevel + " Temperature Trend: " + ev.TemperatureTrend);
            }
            
            lastEV = ev;
            thermalWarningLevel = ev.WarningLevel;

            if (!adaptivePerformanceEnabled) { return; }
            if (!FasterPlayerPrefs.IsMobilePlatform) { return; }
            if (FindMainCamera.MainCamera)
            {
                if (!FindMainCamera.MainCamera.enabled) { return; }
            }

            float invertedTemperatureLevel = 1 - ev.TemperatureLevel;

            // Adaptive resolution scale
            SetDPIScale(DPIScalingCurve.Evaluate(invertedTemperatureLevel) * DPIScale);

            // Adaptive LOD
            SetLODBias(LODBiasCurve.Evaluate(invertedTemperatureLevel) * maxLODBias);

            // Texture mip maps
            ChangeTextureMipMaps(ev.WarningLevel, ev.TemperatureLevel);

            // Adaptive frame rate
            if (ev.WarningLevel == WarningLevel.Throttling & ev.TemperatureLevel > 1.5f)
            {
                int newTargetFrameRate = 30;
                if (targetFrameRate > 60) { newTargetFrameRate = 60; }
                SetTargetFrameRate(newTargetFrameRate);
            }
            else
            {
                NetSceneManager.SetTargetFrameRate();
            }

            float maxAudioCullingDistance = 100;
            AudioManager.AudioCullingDistance = audioCullingDistanceCurve.Evaluate(invertedTemperatureLevel) * maxAudioCullingDistance;

            OnThermalChange?.Invoke(ev);
        }

        public static UnityAction<ThermalMetrics> OnThermalChange;

        private void SetLODBias(float value)
        {
            if (!FasterPlayerPrefs.IsMobilePlatform) { return; }

            Debug.Log("Setting LOD Bias " + value);
            QualitySettings.lodBias = value;
        }

        private void SetTargetFrameRate(int value)
        {
            if (!FasterPlayerPrefs.IsMobilePlatform) { return; }

            Debug.Log("Setting Target Frame Rate " + value);
            Application.targetFrameRate = value;
        }

        private void ChangeTextureMipMaps(WarningLevel warningLevel, float temperatureLevel)
        {
            if (NetSceneManager.DoesExist())
            {
                if (!NetSceneManager.Singleton.ShouldSpawnPlayerCached)
                {
                    QualitySettings.globalTextureMipmapLimit = 0;
                    return;
                }
            }

            if (temperatureLevel > 1.75f)
            {
                QualitySettings.globalTextureMipmapLimit = 3;
            }
            else if (temperatureLevel > 1.4f)
            {
                QualitySettings.globalTextureMipmapLimit = 2;
            }
            else if (temperatureLevel > 1)
            {
                QualitySettings.globalTextureMipmapLimit = 1;
            }
            else
            {
                QualitySettings.globalTextureMipmapLimit = 0;
            }
        }

        private void SetDPIScale(float value)
        {
            if (!FasterPlayerPrefs.IsMobilePlatform) { return; }

            Debug.Log("Setting DPI Scale " + value);

            // DPI must remain above 0.5
            value = Mathf.Max(value, 0.5f);

            if (NetSceneManager.DoesExist())
            {
                if (!NetSceneManager.Singleton.ShouldSpawnPlayerCached)
                {
                    value = Mathf.Max(value, 1);
                }
            }

            QualitySettings.resolutionScalingFixedDPIFactor = value;
        }

        private void OnSceneUnload()
        {
            if (NetSceneManager.DoesExist())
            {
                if (!NetSceneManager.GetShouldSpawnPlayer())
                {
                    if (QualitySettings.resolutionScalingFixedDPIFactor < 1)
                    {
                        SetDPIScale(1);
                    }
                    ChangeTextureMipMaps(WarningLevel.NoWarning, 0);
                }
            }
        }
    }
}