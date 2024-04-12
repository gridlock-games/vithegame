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
    }

    void OnEnable()
    {
        Application.logMessageReceived += Log;
        fpsCounterCoroutine = StartCoroutine(FPSCounter());
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;

        if (type == LogType.Error)
            myLog = output + "\n" + stack + "\n" + myLog;
        else
            myLog = output + "\n" + myLog;

        if (myLog.Length > 1000)
        {
            myLog = myLog.Substring(0, 1000);
        }

        consoleLogText.text = myLog;
    }

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { return; }

        bool consoleEnabled = bool.Parse(PlayerPrefs.GetString("ConsoleEnabled"));
        bool fpsEnabled = bool.Parse(PlayerPrefs.GetString("FPSEnabled"));
        bool pingEnabled = bool.Parse(PlayerPrefs.GetString("PingEnabled"));

        //if (Input.GetKeyDown(KeyCode.BackQuote))
        //{
        //    PlayerPrefs.SetString("DebugOverlayEnabled", (!enableDisplay).ToString());
        //    myLog = "";
        //}

        debugCanvas.SetActive(consoleEnabled | fpsEnabled | pingEnabled);

        consoleParent.SetActive(consoleEnabled);

        if (fpsEnabled)
        {
            fpsText.text = Mathf.RoundToInt(frameCount).ToString() + "FPS";
            Color fpsTextColor;
            if (Mathf.RoundToInt(frameCount) >= Screen.currentResolution.refreshRate)
            {
                fpsTextColor = Color.green;
            }
            else if (Mathf.RoundToInt(frameCount) >= Screen.currentResolution.refreshRate / 2)
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

    private Coroutine fpsCounterCoroutine;
    private float frameCount;
    private IEnumerator FPSCounter()
    {
        if (fpsCounterCoroutine != null)
            StopCoroutine(fpsCounterCoroutine);
        while (true)
        {
            frameCount = 1f / Time.unscaledDeltaTime;
            yield return new WaitForSeconds(0.1f);
        }
    }
}