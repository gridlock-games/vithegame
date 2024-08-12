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

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private Canvas consoleParent;
    [SerializeField] private Text consoleLogText;
    [SerializeField] private Text fpsText;
    [SerializeField] private Text dividerText;
    [SerializeField] private Text pingText;

    static string myLog = "";
    private string output;
    private string stack;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        debugCanvas.enabled = false;
        consoleLogText.text = myLog;
        DebugManager.instance.enableRuntimeUI = false;

        fpsText.text = "";
        dividerText.text = "";
        pingText.text = "";

        InvokeRepeating(nameof(RefreshFps), 0, 0.1f);
        InvokeRepeating(nameof(RefreshPing), 0, 0.1f);

        StartCoroutine(RefreshStatusAfter1Frame());

        # if PLATFORM_ANDROID
        if (!Application.isEditor)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log(Path.Join(Application.persistentDataPath, "myLog.raw"));
                Profiler.logFile = Path.Join(Application.persistentDataPath, "myLog.raw"); //Also supports passing "myLog.raw"
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;

                // Optional, if more memory is needed for the buffer
                //Profiler.maxUsedMemory = 256 * 1024 * 1024;
            }
        }
        # endif
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

    private void RefreshPing()
    {
        if (localPlayer)
        {
            pingValue = localPlayer.GetRoundTripTime();
        }
        else if (localSpectator)
        {
            pingValue = localSpectator.GetRoundTripTime();
        }
        else
        {
            pingValue = 0;
        }
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;

        myLog = type.ToString() + ": " + output + "\n" + stack + "\n" + myLog;

        if (myLog.Length > 1000)
        {
            myLog = myLog.Substring(0, 1000);
        }

        consoleLogText.text = myLog;
    }

    private int fpsValue;
    private ulong pingValue;

    private Attributes localPlayer;
    private Spectator localSpectator;
    private void FindLocalPlayer()
    {
        if (localPlayer) { return; }
        if (localSpectator) { return; }
        if (!PlayerDataManager.DoesExist()) { return; }
        localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
        NetworkObject specGO = PlayerDataManager.Singleton.GetLocalSpectatorObject().Value;
        if (specGO) { localSpectator = specGO.GetComponent<Spectator>(); }
    }

    private void RefreshStatus()
    {
        consoleEnabled = bool.Parse(FasterPlayerPrefs.Singleton.GetString("ConsoleEnabled"));
        fpsEnabled = bool.Parse(FasterPlayerPrefs.Singleton.GetString("FPSEnabled"));
        pingEnabled = bool.Parse(FasterPlayerPrefs.Singleton.GetString("PingEnabled"));
    }

    private bool consoleEnabled;
    private bool fpsEnabled;
    private bool pingEnabled;

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { return; }
        
        if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

        Debug.unityLogger.logEnabled = Application.isEditor | consoleEnabled;

        debugCanvas.enabled = consoleEnabled | fpsEnabled | pingEnabled;

        consoleParent.enabled = consoleEnabled;

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
                if (fpsValue >= Screen.currentResolution.refreshRate)
                {
                    fpsTextColor = Color.green;
                }
                else if (fpsValue >= Screen.currentResolution.refreshRate / 2)
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
        else
        {
            fpsText.text = "";
        }

        if (pingEnabled)
        {
            FindLocalPlayer();
            bool pingTextEvaluated = false;
            if (localPlayer | localSpectator)
            {
                pingText.text = pingValue.ToString() + "ms";
                dividerText.text = fpsEnabled ? "|" : "";
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

            if (!pingTextEvaluated)
            {
                pingText.text = "";
                dividerText.text = "";
                pingText.color = Color.green;
            }
        }
        else
        {
            pingText.text = "";
            dividerText.text = "";
        }
    }
}