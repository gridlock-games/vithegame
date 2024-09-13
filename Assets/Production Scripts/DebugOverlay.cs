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
        consoleEnabled = FasterPlayerPrefs.Singleton.GetBool("ConsoleEnabled");
        fpsEnabled = FasterPlayerPrefs.Singleton.GetBool("FPSEnabled");
        pingEnabled = FasterPlayerPrefs.Singleton.GetBool("PingEnabled");

        if (!consoleEnabled)
        {
            myLog = "";
            consoleLogText.text = "";
        }
        Debug.unityLogger.logEnabled = Application.isEditor | consoleEnabled;
        debugCanvas.enabled = consoleEnabled | fpsEnabled | pingEnabled;
        consoleParent.enabled = consoleEnabled;

        if (!fpsEnabled) { fpsText.text = ""; }

        if (!pingEnabled)
        {
            pingText.text = "";
            dividerText.text = "";
        }
    }

    private bool consoleEnabled;
    private bool fpsEnabled;
    private bool pingEnabled;

  //UNfortunately this is pretty much the easier way to get the logs.
  //  public string RetreveDebugLog()
  //{
  //  return consoleLogText.text;
  //}
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
    }
}