using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.Netcode;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private GameObject debugCanvas;
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
            myLog = output + "\n" + stack + "\n" + myLog;

        if (myLog.Length > 1000)
        {
            myLog = myLog.Substring(0, 1000);
        }

        consoleLogText.text = myLog;
    }

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { return; }

        bool enableDisplay = bool.Parse(PlayerPrefs.GetString("DebugOverlayEnabled"));
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            PlayerPrefs.SetString("DebugOverlayEnabled", (!enableDisplay).ToString());
            myLog = "";
        }

        debugCanvas.SetActive(enableDisplay);

        if (enableDisplay)
        {
            fpsText.text = Mathf.RoundToInt(frameCount).ToString() + "FPS";
            Color fpsTextColor;
            if (frameCount >= Screen.currentResolution.refreshRate)
            {
                fpsTextColor = Color.green;
            }
            else if (frameCount >= Screen.currentResolution.refreshRate / 2)
            {
                fpsTextColor = Color.yellow;
            }
            else
            {
                fpsTextColor = Color.red;
            }
            fpsText.color = fpsTextColor;

            bool pingTextEvaluated = false;
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    ulong ping = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.LocalClientId);
                    pingText.text = ping.ToString() + "ms";
                    dividerText.text = "|";
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