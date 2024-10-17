using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;
using Vi.Player;
using Unity.Netcode;
using UnityEngine.Profiling;
using System.IO;
using Vi.Core.CombatAgents;
using Unity.Netcode.Transports.UTP;

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

#if PLATFORM_ANDROID && !UNITY_EDITOR
        if (Debug.isDebugBuild)
        {
            Debug.Log(Path.Join(Application.persistentDataPath, "myLog.raw"));
            Profiler.logFile = Path.Join(Application.persistentDataPath, "myLog.raw"); //Also supports passing "myLog.raw"
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;

            // Optional, if more memory is needed for the buffer
            //Profiler.maxUsedMemory = 256 * 1024 * 1024;
        }
#endif
    }

    private IEnumerator RefreshStatusAfter1Frame()
    {
        yield return null;
        RefreshStatus();
    }

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
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
            myLog = type.ToString() + ": " + output + "\n" + stack + "\n" + myLog;
        }
        else
        {
            myLog = type.ToString() + ": " + output + "\n" + myLog;
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

    private void RefreshStatus()
    {
        consoleEnabled = FasterPlayerPrefs.Singleton.GetBool("ConsoleEnabled");
        fpsEnabled = FasterPlayerPrefs.Singleton.GetBool("FPSEnabled");
        pingEnabled = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");
        packetLossEnabled = FasterPlayerPrefs.Singleton.GetBool("PacketLossEnabled");
        jitterEnabled = FasterPlayerPrefs.Singleton.GetBool("JitterEnabled");

        if (!consoleEnabled)
        {
            myLog = "";
            consoleLogText.text = "";
        }

        Debug.unityLogger.logEnabled = Application.isEditor | consoleEnabled | WebRequestManager.IsServerBuild();
        debugCanvas.enabled = consoleEnabled | fpsEnabled | pingEnabled;
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
            if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (fpsValue < 30)
                {
                    fpsTextColor = Color.red;
                }
                else if (fpsValue < 45)
                {
                    fpsTextColor = Color.yellow;
                }
                else
                {
                    fpsTextColor = Color.green;
                }
            }
            else
            {
                if (fpsValue >= Screen.currentResolution.refreshRateRatio.value)
                {
                    fpsTextColor = Color.green;
                }
                else if (fpsValue >= Screen.currentResolution.refreshRateRatio.value / 2)
                {
                    fpsTextColor = Color.yellow;
                }
                else
                {
                    fpsTextColor = Color.red;
                }
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