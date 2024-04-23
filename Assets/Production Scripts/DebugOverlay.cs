using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Core;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private GameObject debugCanvas;
    [SerializeField] private GameObject consoleParent;
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
        debugCanvas.SetActive(false);
        consoleLogText.text = myLog;
        DebugManager.instance.enableRuntimeUI = false;

        fpsText.text = "";
        dividerText.text = "";
        pingText.text = "";

        InvokeRepeating(nameof(RefreshFps), 0, 0.1f);
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

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { return; }

        bool consoleEnabled = bool.Parse(PlayerPrefs.GetString("ConsoleEnabled"));
        bool fpsEnabled = bool.Parse(PlayerPrefs.GetString("FPSEnabled"));
        bool pingEnabled = bool.Parse(PlayerPrefs.GetString("PingEnabled"));

        debugCanvas.SetActive(consoleEnabled | fpsEnabled | pingEnabled);

        consoleParent.SetActive(consoleEnabled);

        if (fpsEnabled)
        {
            fpsText.text = fpsValue.ToString() + "FPS";
            Color fpsTextColor;
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
            fpsText.color = fpsTextColor;
        }
        else
        {
            fpsText.text = "";
        }

        if (pingEnabled)
        {
            bool pingTextEvaluated = false;
            if (PlayerDataManager.Singleton)
            {
                KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (kvp.Value)
                {
                    ulong ping = kvp.Value.GetRoundTripTime();
                    pingText.text = ping.ToString() + "ms";
                    dividerText.text = fpsEnabled ? "|" : "";
                    Color pingTextColor;
                    if (ping >= 80)
                    {
                        pingTextColor = Color.red;
                    }
                    else if (ping >= 50)
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