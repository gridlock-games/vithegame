using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;
using UnityEngine.AdaptivePerformance;

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

        // Uncomment for development build profiling
        //if (Debug.isDebugBuild)
        //{
        //    if (!Application.isEditor)
        //    {
        //        if (!UnityEngine.Profiling.Profiler.enabled)
        //        {
        //            Debug.Log("Enabling Profiler " + System.IO.Path.Join(Application.persistentDataPath, "myLog.raw"));
        //            UnityEngine.Profiling.Profiler.logFile = System.IO.Path.Join(Application.persistentDataPath, "myLog.raw"); //Also supports passing "myLog.raw"
        //            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
        //            UnityEngine.Profiling.Profiler.enabled = true;

        //            // Optional, if more memory is needed for the buffer
        //            //Profiler.maxUsedMemory = 256 * 1024 * 1024;
        //        }
        //    }
        //}
    }

    private IEnumerator RefreshStatusAfter1Frame()
    {
        yield return null;
        RefreshStatus();
    }

    void OnEnable()
    {
        Application.logMessageReceived += Log;
        AdaptivePerformanceManager.OnThermalChange += OnThermalEvent;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
        AdaptivePerformanceManager.OnThermalChange -= OnThermalEvent;
    }

    private void OnThermalEvent(ThermalMetrics ev)
    {
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

    private void RefreshStatus()
    {
        consoleEnabled = FasterPlayerPrefs.Singleton.GetBool("ConsoleEnabled");
        fpsEnabled = FasterPlayerPrefs.Singleton.GetBool("FPSEnabled");
        pingEnabled = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");
        packetLossEnabled = FasterPlayerPrefs.Singleton.GetBool("PacketLossEnabled");
        jitterEnabled = FasterPlayerPrefs.Singleton.GetBool("JitterEnabled");
        thermalEventsEnabled = FasterPlayerPrefs.Singleton.GetBool("ThermalEventsEnabled");

        if (!thermalEventsEnabled)
        {
            thermalWarningImage.enabled = false;
        }
        else if (AdaptivePerformanceManager.thermalWarningLevel == WarningLevel.ThrottlingImminent)
        {
            thermalWarningImage.enabled = true;
            thermalWarningImage.color = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
        }
        else if (AdaptivePerformanceManager.thermalWarningLevel == WarningLevel.Throttling)
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
        // Uncomment for profiling during a match
        //if (PlayerDataManager.DoesExist())
        //{
        //    if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None
        //    & Vi.Core.GameModeManagers.GameModeManager.Singleton)
        //    {
        //        if (Debug.isDebugBuild)
        //        {
        //            if (!Application.isEditor)
        //            {
        //                if (!UnityEngine.Profiling.Profiler.enabled)
        //                {
        //                    Debug.Log("Enabling Profiler " + System.IO.Path.Join(Application.persistentDataPath, "myLog.raw"));
        //                    UnityEngine.Profiling.Profiler.logFile = System.IO.Path.Join(Application.persistentDataPath, "myLog.raw"); //Also supports passing "myLog.raw"
        //                    UnityEngine.Profiling.Profiler.enableBinaryLog = true;
        //                    UnityEngine.Profiling.Profiler.enabled = true;

        //                    // Optional, if more memory is needed for the buffer
        //                    //Profiler.maxUsedMemory = 256 * 1024 * 1024;
        //                }
        //            }
        //        }
        //    }
        //}
        
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