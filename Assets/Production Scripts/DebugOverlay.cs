using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private Canvas consoleParent;
    [SerializeField] private Text consoleLogText;
    [SerializeField] private Text fpsText;
    [SerializeField] private Text topDividerText;
    [SerializeField] private Text pingText;
    [SerializeField] private Text packetLossText;
    [SerializeField] private Text bottomDividerText;
    [SerializeField] private Text jitterText;
    [SerializeField] private Image thermalWarningImage;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        debugCanvas.enabled = false;
        consoleLogText.text = myLog;
        DebugManager.instance.enableRuntimeUI = false;

        fpsText.text = "";
        topDividerText.text = "";
        pingText.text = "";
        packetLossText.text = "";
        bottomDividerText.text = "";
        jitterText.text = "";

        InvokeRepeating(nameof(RefreshFps), 0, 0.1f);
        InvokeRepeating(nameof(RefreshPing), 0, 0.1f);

        StartCoroutine(RefreshStatusAfter1Frame());

        thermalWarningImage.enabled = false;

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

    private IAdaptivePerformance ap = null;

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

    private WarningLevel thermalWarningLevel = WarningLevel.NoWarning;
    void OnThermalEvent(ThermalMetrics ev)
    {
        thermalWarningLevel = ev.WarningLevel;

        if (adaptivePerformanceEnabled & FasterPlayerPrefs.IsMobilePlatform)
        {
            float invertedTemperatureLevel = 1 - ev.TemperatureLevel;
            
            // Adaptive resolution scale
            SetDPIScale(DPIScalingCurve.EvaluateNormalizedTime(invertedTemperatureLevel) * DPIScale);

            // Adaptive LOD
            SetLODBias(LODBiasCurve.EvaluateNormalizedTime(invertedTemperatureLevel) * maxLODBias);

            // Texture mip maps
            ChangeTextureMipMaps(ev.WarningLevel, ev.TemperatureLevel);

            // Adaptive frame rate
            if (ev.WarningLevel == WarningLevel.Throttling)
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
            AudioManager.AudioCullingDistance = audioCullingDistanceCurve.EvaluateNormalizedTime(invertedTemperatureLevel) * maxAudioCullingDistance;
        }

        Debug.Log("Thermal Warning Level: " + ev.WarningLevel);
        Debug.Log("Temperature Level: " + ev.TemperatureLevel + " Temperature Trend: " + ev.TemperatureTrend);

        if (!thermalEventsEnabled) { return; }

        switch (ev.WarningLevel)
        {
            case WarningLevel.NoWarning:
                thermalWarningImage.enabled = false;
                break;
            case WarningLevel.ThrottlingImminent:
                thermalWarningImage.enabled = true;
                thermalWarningImage.color = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
                break;
            case WarningLevel.Throttling:
                thermalWarningImage.enabled = true;
                thermalWarningImage.color = Color.red;
                break;
        }
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

        if (warningLevel == WarningLevel.Throttling)
        {
            QualitySettings.globalTextureMipmapLimit = 3;
        }
        else if (warningLevel == WarningLevel.ThrottlingImminent)
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

        if (NetSceneManager.DoesExist())
        {
            if (!NetSceneManager.Singleton.ShouldSpawnPlayerCached)
            {
                value = Mathf.Max(value, 0.9f);
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
                SetDPIScale(0.9f);
                ChangeTextureMipMaps(WarningLevel.NoWarning, 0);
            }
        }
    }

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

    private IEnumerator RefreshStatusAfter1Frame()
    {
        yield return null;
        RefreshStatus();
    }

    void OnEnable()
    {
        Application.logMessageReceived += Log;
        QualitySettings.activeQualityLevelChanged += QualitySettings_activeQualityLevelChanged;
        EventDelegateManager.sceneUnloaded += OnSceneUnload;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
        QualitySettings.activeQualityLevelChanged -= QualitySettings_activeQualityLevelChanged;
        EventDelegateManager.sceneUnloaded -= OnSceneUnload;
    }

    private void RefreshFps() { fpsValue = (int)(1f / Time.unscaledDeltaTime); }

    private int[] pingHistory;
    private int pingHistoryIndex;
    private ulong jitterValue;
    private void RefreshPing()
    {
        if (networkTransport & NetworkManager.Singleton)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                pingValue = networkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
                if (pingHistory == default) { pingHistory = new int[32]; }
                pingHistory[pingHistoryIndex] = (int)pingValue;
                pingHistoryIndex++;
                if (pingHistoryIndex == pingHistory.Length) { pingHistoryIndex = 0; }

                float totalJitter = 0;
                // Calculate the difference between consecutive latencies
                for (int i = 1; i < pingHistory.Length; i++)
                {
                    totalJitter += Mathf.Abs(pingHistory[i] - pingHistory[i - 1]);
                }
                // Return the average jitter (mean of the differences)
                jitterValue = (ulong)(totalJitter / (pingHistory.Length - 1));
            }
            else
            {
                pingValue = 0;
                pingHistoryIndex = 0;
                if (pingHistory != default) { pingHistory = default; }
                jitterValue = 0;
            }
        }
        else
        {
            pingValue = 0;
            pingHistoryIndex = 0;
            if (pingHistory != default) { pingHistory = default; }
            jitterValue = 0;
        }
    }

    static string myLog = "";
    private string output;
    private string stack;

    public void Log(string logString, string stackTrace, LogType type)
    {
        if (!consoleEnabled) { return; }

        output = logString;
        stack = stackTrace;

        if (type.ToString() == LogType.Error.ToString() | logString.Contains("Exception") | type.ToString() == "Exception")
        {
            myLog = "[" + Time.time.ToString("F2") + "] " + type.ToString() + ": " + output + "\n" + stack + "\n" + myLog;
        }
        else
        {
            myLog = "[" + Time.time.ToString("F2") + "] " + type.ToString() + ": " + output + "\n" + myLog;
        }

        if (myLog.Length > 3000)
        {
            myLog = myLog.Substring(0, 3000);
        }

        consoleLogText.text = myLog;
    }

    private int fpsValue;
    private ulong pingValue;

    private UnityTransport networkTransport;
    private void FindNetworkTransport()
    {
        if (networkTransport) { return; }
        if (NetworkManager.Singleton)
        {
            networkTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }
    }

    private bool thermalEventsEnabled;
    private bool adaptivePerformanceEnabled;
    private float DPIScale;
    private int targetFrameRate;

    private void RefreshStatus()
    {
        consoleEnabled = FasterPlayerPrefs.Singleton.GetBool("ConsoleEnabled");
        fpsEnabled = FasterPlayerPrefs.Singleton.GetBool("FPSEnabled");
        pingEnabled = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");
        packetLossEnabled = FasterPlayerPrefs.Singleton.GetBool("PacketLossEnabled");
        jitterEnabled = FasterPlayerPrefs.Singleton.GetBool("JitterEnabled");
        thermalEventsEnabled = FasterPlayerPrefs.Singleton.GetBool("ThermalEventsEnabled");
        adaptivePerformanceEnabled = FasterPlayerPrefs.Singleton.GetBool("EnableAdaptivePerformance");
        DPIScale = FasterPlayerPrefs.Singleton.GetFloat("DPIScalingFactor");
        targetFrameRate = FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate");

        if (!adaptivePerformanceEnabled)
        {
            QualitySettings.lodBias = maxLODBias;
            QualitySettings.globalTextureMipmapLimit = 0;
            NetSceneManager.SetTargetFrameRate();
        }

        if (!thermalEventsEnabled)
        {
            thermalWarningImage.enabled = false;
        }
        else if (thermalWarningLevel == WarningLevel.ThrottlingImminent)
        {
            thermalWarningImage.enabled = true;
            thermalWarningImage.color = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
        }
        else if (thermalWarningLevel == WarningLevel.Throttling)
        {
            thermalWarningImage.enabled = true;
            thermalWarningImage.color = Color.red;
        }

        if (!consoleEnabled)
        {
            myLog = "";
            consoleLogText.text = "";
        }

        Debug.unityLogger.logEnabled = Application.isEditor | consoleEnabled | WebRequestManager.IsServerBuild();
        debugCanvas.enabled = consoleEnabled | fpsEnabled | pingEnabled | packetLossEnabled | jitterEnabled | thermalEventsEnabled;
        consoleParent.enabled = consoleEnabled;

        if (!fpsEnabled) { fpsText.text = ""; }

        if (!pingEnabled)
        {
            pingText.text = "";
            topDividerText.text = "";
        }

        if (!packetLossEnabled)
        {
            packetLossText.text = "";
        }

        if (!jitterEnabled)
        {
            jitterText.text = "";
            bottomDividerText.text = "";
        }
    }

    private bool consoleEnabled;
    private bool fpsEnabled;
    private bool pingEnabled;
    private bool packetLossEnabled;
    private bool jitterEnabled;

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { return; }

        if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

        if (fpsEnabled)
        {
            fpsText.text = fpsValue.ToString() + "FPS";
            Color fpsTextColor;
            float targetFrameRateToConsider = Mathf.Min((float)Screen.currentResolution.refreshRateRatio.value, Application.targetFrameRate);
            if (fpsValue >= targetFrameRateToConsider - 2)
            {
                fpsTextColor = Color.green;
            }
            else if (fpsValue >= (targetFrameRateToConsider / 2) - 2)
            {
                fpsTextColor = Color.yellow;
            }
            else
            {
                fpsTextColor = Color.red;
            }
            fpsText.color = fpsTextColor;
        }

        if (pingEnabled)
        {
            FindNetworkTransport();
            bool pingTextEvaluated = false;
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    pingText.text = pingValue.ToString() + "ms";
                    topDividerText.text = fpsEnabled ? "|" : "";
                    Color pingTextColor;
                    if (pingValue >= 80)
                    {
                        pingTextColor = Color.red;
                    }
                    else if (pingValue >= 50)
                    {
                        pingTextColor = Color.yellow;
                    }
                    else
                    {
                        pingTextColor = Color.green;
                    }
                    pingText.color = pingTextColor;
                    pingTextEvaluated = true;
                }
            }

            if (!pingTextEvaluated)
            {
                pingText.text = "";
                topDividerText.text = "";
                pingText.color = Color.green;
            }
        }

        if (packetLossEnabled)
        {
            if (NetworkMetricManager.Singleton)
            {
                packetLossText.text = (NetworkMetricManager.Singleton.PacketLoss * 100).ToString("F0") + "%";

                Color packetLossTextColor;
                if (NetworkMetricManager.Singleton.PacketLoss >= 0.05f)
                {
                    packetLossTextColor = Color.red;
                }
                else if (NetworkMetricManager.Singleton.PacketLoss >= 0.02f)
                {
                    packetLossTextColor = Color.yellow;
                }
                else
                {
                    packetLossTextColor = Color.green;
                }
                packetLossText.color = packetLossTextColor;
            }
            else
            {
                packetLossText.text = "";
            }
        }

        if (jitterEnabled)
        {
            bool jitterTextEvaluated = false;
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    jitterText.text = jitterValue.ToString() + "ms";
                    bottomDividerText.text = packetLossEnabled ? "|" : "";

                    Color jitterTextColor;
                    if (jitterValue >= 30)
                    {
                        jitterTextColor = Color.red;
                    }
                    else if (jitterValue >= 10)
                    {
                        jitterTextColor = Color.yellow;
                    }
                    else
                    {
                        jitterTextColor = Color.green;
                    }
                    jitterText.color = jitterTextColor;
                    jitterTextEvaluated = true;
                }
            }

            if (!jitterTextEvaluated)
            {
                jitterText.text = "";
                bottomDividerText.text = "";
                jitterText.color = Color.green;
            }
        }
    }
}