using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Firebase;
using Firebase.Analytics;
using System.Linq;

namespace Vi.Core
{
    public class GameInitialization : MonoBehaviour
    {
        [SerializeField] private SceneReference baseSceneReference;
        [SerializeField] private SceneReference mainMenuSceneReference;
        [SerializeField] private Text assetNumberText;
        [SerializeField] private Text downloadProgressBarText;
        [SerializeField] private Image downloadProgressBarImage;
        [SerializeField] private Text headerText;

        public void ExitGame()
        {
            Application.Quit();
        }

        private void Start()
        {
            InitializePlayerPrefs();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = PersistentLocalObjects.Singleton.GetInt("TargetFrameRate");
            StartCoroutine(LoadScenes());

            if (!WebRequestManager.IsServerBuild())
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(continuationAction: task =>
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                });
            }

            headerText.text = "Preparing Your Vi Experience";
        }

        private static readonly List<string> holdToggleOptions = new List<string>() { "HOLD", "TOGGLE" };
        private void InitializePlayerPrefs()
        {
            if (!PersistentLocalObjects.Singleton.HasKey("TargetFrameRate")) { PersistentLocalObjects.Singleton.SetInt("TargetFrameRate", Screen.currentResolution.refreshRate + 60); }

            if (!PersistentLocalObjects.Singleton.HasKey("InvertMouse")) { PersistentLocalObjects.Singleton.SetString("InvertMouse", false.ToString()); }
            if (!PersistentLocalObjects.Singleton.HasKey("MouseXSensitivity")) { PersistentLocalObjects.Singleton.SetFloat("MouseXSensitivity", 0.2f); }
            if (!PersistentLocalObjects.Singleton.HasKey("MouseYSensitivity")) { PersistentLocalObjects.Singleton.SetFloat("MouseYSensitivity", 0.2f); }
            if (!PersistentLocalObjects.Singleton.HasKey("ZoomSensitivityMultiplier")) { PersistentLocalObjects.Singleton.SetFloat("ZoomSensitivityMultiplier", 1); }
            if (!PersistentLocalObjects.Singleton.HasKey("MobileLookJoystickSensitivity")) { PersistentLocalObjects.Singleton.SetFloat("MobileLookJoystickSensitivity", 4); }
            if (!PersistentLocalObjects.Singleton.HasKey("ZoomMode")) { PersistentLocalObjects.Singleton.SetString("ZoomMode", "TOGGLE"); }
            if (!PersistentLocalObjects.Singleton.HasKey("BlockingMode")) { PersistentLocalObjects.Singleton.SetString("BlockingMode", "HOLD"); }
            
            if (!PersistentLocalObjects.Singleton.HasKey("DisableBots")) { PersistentLocalObjects.Singleton.SetString("DisableBots", false.ToString()); }

            if (!PersistentLocalObjects.Singleton.HasKey("AutoAim")) { PersistentLocalObjects.Singleton.SetString("AutoAim", true.ToString()); }

            if (!PersistentLocalObjects.Singleton.HasKey("ConsoleEnabled")) { PersistentLocalObjects.Singleton.SetString("ConsoleEnabled", false.ToString()); }
            if (!PersistentLocalObjects.Singleton.HasKey("FPSEnabled")) { PersistentLocalObjects.Singleton.SetString("FPSEnabled", false.ToString()); }
            if (!PersistentLocalObjects.Singleton.HasKey("PingEnabled")) { PersistentLocalObjects.Singleton.SetString("PingEnabled", false.ToString()); }

            if (!PersistentLocalObjects.Singleton.HasKey("Rebinds")) { PersistentLocalObjects.Singleton.SetString("Rebinds", ""); }

            if (!PersistentLocalObjects.Singleton.HasKey("UIOpacity")) { PersistentLocalObjects.Singleton.SetFloat("UIOpacity", 1); }

            if (!PersistentLocalObjects.Singleton.HasKey("MasterVolume"))
            {
                PersistentLocalObjects.Singleton.SetFloat("MasterVolume", 0.75f);
                AudioListener.volume = 0.75f;
            }
            else
            {
                AudioListener.volume = PersistentLocalObjects.Singleton.GetFloat("MasterVolume");
            }

            VerifyHoldPlayerPref("ZoomMode", 1);
            VerifyHoldPlayerPref("BlockingMode", 0);
        }

        private void VerifyHoldPlayerPref(string key, int defaultIndex)
        {
            if (!PersistentLocalObjects.Singleton.HasKey(key)) { Debug.LogError("Calling VerifyHoldPlayerPref but the key isn't present! " + key); return; }

            if (!holdToggleOptions.Contains(PersistentLocalObjects.Singleton.GetString(key)))
            {
                if (defaultIndex < 0 | defaultIndex >= holdToggleOptions.Count) { Debug.LogError("(Verify Hold Player Pref) Default Index is not in the list! " + key); return; }
                PersistentLocalObjects.Singleton.SetString(key, holdToggleOptions[defaultIndex]);
            }
        }

        private IEnumerator LoadScenes()
        {
            bool downloadsSuccessful = true;

            assetNumberText.text = "";
            downloadProgressBarImage.fillAmount = 1;
            downloadProgressBarText.text = "Querying server for updates";

            AsyncOperationHandle<long> baseDownloadSize = Addressables.GetDownloadSizeAsync(baseSceneReference);
            yield return new WaitUntil(() => baseDownloadSize.IsDone);

            AsyncOperationHandle<long> mainMenuDownloadSize = Addressables.GetDownloadSizeAsync(mainMenuSceneReference);
            yield return new WaitUntil(() => mainMenuDownloadSize.IsDone);
            
            if (baseDownloadSize.Result > 0)
            {
                AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(baseSceneReference);

                float lastRateTime = -1;
                float downloadRate = 0;
                float lastBytesAmount = 0;
                float totalMB = downloadHandle.GetDownloadStatus().TotalBytes * 0.000001f;
                while (!downloadHandle.IsDone)
                {
                    downloadProgressBarImage.fillAmount = downloadHandle.GetDownloadStatus().Percent;

                    float downloadedMB = downloadHandle.GetDownloadStatus().DownloadedBytes * 0.000001f;
                    
                    if (Time.time - lastRateTime >= 1)
                    {
                        downloadRate = downloadedMB - lastBytesAmount;
                        lastBytesAmount = downloadedMB;
                        lastRateTime = Time.time;
                    }

                    assetNumberText.text = mainMenuDownloadSize.Result == 0 ? "Downloading asset 1 of 1" : "Downloading asset 1 of 2";
                    downloadProgressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                    yield return null;
                }

                downloadsSuccessful = downloadHandle.Status == AsyncOperationStatus.Succeeded & downloadsSuccessful;

                Addressables.Release(downloadHandle);
            }

            if (mainMenuDownloadSize.Result > 0)
            {
                AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(mainMenuSceneReference);

                float lastRateTime = -1;
                float downloadRate = 0;
                float lastBytesAmount = 0;
                float totalMB = downloadHandle.GetDownloadStatus().TotalBytes * 0.000001f;
                while (!downloadHandle.IsDone)
                {
                    downloadProgressBarImage.fillAmount = downloadHandle.GetDownloadStatus().Percent;

                    float downloadedMB = downloadHandle.GetDownloadStatus().DownloadedBytes * 0.000001f;

                    if (Time.time - lastRateTime >= 1)
                    {
                        downloadRate = downloadedMB - lastBytesAmount;
                        lastBytesAmount = downloadedMB;
                        lastRateTime = Time.time;
                    }

                    assetNumberText.text = baseDownloadSize.Result == 0 ? "Downloading asset 1 of 1" : "Downloading asset 2 of 2";
                    downloadProgressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                    yield return null;
                }

                downloadsSuccessful = downloadHandle.Status == AsyncOperationStatus.Succeeded & downloadsSuccessful;

                Addressables.Release(downloadHandle);
            }

            assetNumberText.text = downloadsSuccessful ? "" : "Servers Offline or Your Connection is Bad";
            downloadProgressBarImage.fillAmount = 1;
            downloadProgressBarText.text = downloadsSuccessful ? "All Downloads Complete" : "Downloads Unsuccessful";

            Addressables.Release(mainMenuDownloadSize);
            Addressables.Release(baseDownloadSize);

            if (downloadsSuccessful)
            {
                headerText.text = "Loading Main Menu";
                yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(baseSceneReference.SceneName));
                }
                catch
                {
                    headerText.text = "Could Not Load Main Menu";
                    yield break;
                }
                SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            }
            else
            {
                headerText.text = "Restart The Game To Try Again";
            }
        }

        private float lastTextChangeTime;
        private void Update()
        {
            if (headerText.text == "Restart The Game To Try Again") { return; }

            if (Time.time - lastTextChangeTime > 0.5f)
            {
                lastTextChangeTime = Time.time;
                switch (headerText.text.Split(".").Length)
                {
                    case 1:
                        headerText.text = headerText.text.Replace(".", "") + ".";
                        break;
                    case 2:
                        headerText.text = headerText.text.Replace(".", "") + "..";
                        break;
                    case 3:
                        headerText.text = headerText.text.Replace(".", "") + "...";
                        break;
                    case 4:
                        headerText.text = headerText.text.Replace(".", "");
                        break;
                }
            }
        }
    }
}