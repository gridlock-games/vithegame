using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(RuntimeNetStatsMonitor))]
public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private GameObject debugCanvas;
    [SerializeField] private Text fpsText;

    private bool ignoreInfo;
    private bool ignoreWarnings;
    private bool ignoreErrors;

    static string myLog = "";
    private string output;
    private string stack;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        debugCanvas.SetActive(false);
        DebugManager.instance.enableRuntimeUI = false;
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
        if (ignoreInfo) { if (type == LogType.Log) { return; } }
        if (ignoreWarnings) { if (type == LogType.Warning) { return; } }
        if (ignoreErrors) { if (type == LogType.Error) { return; } }

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
            fpsText.text = "FPS: " + Mathf.RoundToInt(frameCount).ToString();
            Color fpsTextColor = Color.white;
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