using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using System.Linq;
using UnityEngine.InputSystem;

namespace Vi.Player
{
    [RequireComponent(typeof(RuntimeNetStatsMonitor))]
    public class DebugOverlay : MonoBehaviour
    {
        private bool ignoreInfo;
        private bool ignoreWarnings;
        private bool ignoreErrors;

        static string myLog = "";
        private string output;
        private string stack;
        private bool enableDisplay;

        private RuntimeNetStatsMonitor runtimeNetStatsMonitor;

        private void Start()
        {
            enableDisplay = Debug.isDebugBuild & !Application.isEditor;
            DontDestroyOnLoad(gameObject);
            runtimeNetStatsMonitor = GetComponent<RuntimeNetStatsMonitor>();
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
                myLog = output + "\n" + myLog;

            if (myLog.Length > 1000)
            {
                myLog = myLog.Substring(0, 1000);
            }
        }

        void OnGUI()
        {
            if (!enableDisplay) { return; }
            GUI.TextArea(new Rect(10, 10, Screen.width / 3 - 10, Screen.height / 3 - 10), myLog);
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 24;
            GUI.Label(new Rect(0, Screen.height - 25, 100, 10), "FPS: " + Mathf.RoundToInt(frameCount).ToString(), style);
            GUI.UnfocusWindow();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) | Keyboard.current[Key.Backquote].wasPressedThisFrame)
            {
                enableDisplay = !enableDisplay;
                myLog = "";
            }
            runtimeNetStatsMonitor.Visible = enableDisplay & Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer;
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
}