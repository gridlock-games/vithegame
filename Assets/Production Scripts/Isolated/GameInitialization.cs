using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Firebase;
using Firebase.Analytics;
using Vi.Utility;
using UnityEngine.ResourceManagement.ResourceProviders;

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
            FasterPlayerPrefs.QuitGame();
        }

        private void Start()
        {
            InitializePlayerPrefs();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            NetSceneManager.SetTargetFrameRate();
            StartCoroutine(LoadScenes());

            if (!FasterPlayerPrefs.IsServerPlatform)
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(continuationAction: task =>
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                });
            }

            headerText.text = "Preparing Your Vi Experience";
        }

        private void InitializePlayerPrefs()
        {
            // Track if we just updated
            if (FasterPlayerPrefs.Singleton.HasString("LastApplicationVersion"))
            {
                if (FasterPlayerPrefs.Singleton.GetString("LastApplicationVersion") != Application.version)
                {
                    // Just updated, execute this code
                    if (FasterPlayerPrefs.Singleton.HasBool("PostProcessingEnabled")) { FasterPlayerPrefs.Singleton.DeleteKey("PostProcessingEnabled"); }
                }
            }

            if (!FasterPlayerPrefs.Singleton.HasBool("PointerEffects")) { FasterPlayerPrefs.Singleton.SetBool("PointerEffects", true); }

            FasterPlayerPrefs.Singleton.SetString("LastApplicationVersion", Application.version);

            if (!FasterPlayerPrefs.Singleton.HasBool("EnableAdaptivePerformance")) { FasterPlayerPrefs.Singleton.SetBool("EnableAdaptivePerformance", FasterPlayerPrefs.IsMobilePlatform); }
            
            if (!FasterPlayerPrefs.Singleton.HasBool("TutorialCompleted")) { FasterPlayerPrefs.Singleton.SetBool("TutorialCompleted", false); }
            FasterPlayerPrefs.Singleton.SetBool("TutorialInProgress", false);

            if (!FasterPlayerPrefs.Singleton.HasInt("TargetFrameRate"))
            {
                int targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value + 60);
                if (FasterPlayerPrefs.IsMobilePlatform) { targetFrameRate = 60; }
                FasterPlayerPrefs.Singleton.SetInt("TargetFrameRate", targetFrameRate);
            }

            if (!FasterPlayerPrefs.Singleton.HasInt("BotSpawnNumber")) { FasterPlayerPrefs.Singleton.SetInt("BotSpawnNumber", 3); }

            if (!FasterPlayerPrefs.Singleton.HasBool("AllowLocalhostServers")) { FasterPlayerPrefs.Singleton.SetBool("AllowLocalhostServers", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("AllowLANServers")) { FasterPlayerPrefs.Singleton.SetBool("AllowLANServers", false); }

            if (!FasterPlayerPrefs.Singleton.HasColor("CrosshairColor")) { FasterPlayerPrefs.Singleton.SetColor("CrosshairColor", Color.red); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("CrosshairSize")) { FasterPlayerPrefs.Singleton.SetFloat("CrosshairSize", 1); }
            if (!FasterPlayerPrefs.Singleton.HasInt("CrosshairStyle")) { FasterPlayerPrefs.Singleton.SetInt("CrosshairStyle", 26); }

            if (!FasterPlayerPrefs.Singleton.HasFloat("GyroscopicRotationSensitivity")) { FasterPlayerPrefs.Singleton.SetFloat("GyroscopicRotationSensitivity", 1); }

            if (!FasterPlayerPrefs.Singleton.HasBool("MobileLookJoystickActsLikeButton")) { FasterPlayerPrefs.Singleton.SetBool("MobileLookJoystickActsLikeButton", false); }

            if (!FasterPlayerPrefs.Singleton.HasBool("UIVibrationsEnabled")) { FasterPlayerPrefs.Singleton.SetBool("UIVibrationsEnabled", true); }
            if (!FasterPlayerPrefs.Singleton.HasBool("DeathVibrationEnabled")) { FasterPlayerPrefs.Singleton.SetBool("DeathVibrationEnabled", true); }
            if (!FasterPlayerPrefs.Singleton.HasBool("GameplayVibrationsEnabled")) { FasterPlayerPrefs.Singleton.SetBool("GameplayVibrationsEnabled", true); }

            if (!FasterPlayerPrefs.Singleton.HasInt("SpeakerMode")) { FasterPlayerPrefs.Singleton.SetInt("SpeakerMode", (int)AudioSettings.GetConfiguration().speakerMode); }
            if (!FasterPlayerPrefs.Singleton.HasInt("SampleRate")) { FasterPlayerPrefs.Singleton.SetInt("SampleRate", AudioSettings.GetConfiguration().sampleRate); }

            if (!FasterPlayerPrefs.Singleton.HasInt("Tokens")) { FasterPlayerPrefs.Singleton.SetInt("Tokens", 5); }

            if (FasterPlayerPrefs.Singleton.HasString("LastLoginTime"))
            {
                if (System.DateTime.TryParse(FasterPlayerPrefs.Singleton.GetString("LastLoginTime"), out System.DateTime lastLoginTime))
                {
                    if (lastLoginTime.Day != System.DateTime.UtcNow.Day)
                    {
                        FasterPlayerPrefs.Singleton.SetInt("Tokens", FasterPlayerPrefs.Singleton.GetInt("Tokens") + 5);
                    }
                }
                else
                {
                    Debug.LogError("Error while parsing datetime string " + FasterPlayerPrefs.Singleton.GetString("LastLoginTime"));
                }
            }
            else
            {
                FasterPlayerPrefs.Singleton.SetString("LastLoginTime", System.DateTime.UtcNow.ToString());
            }

            if (!FasterPlayerPrefs.Singleton.HasBool("InvertMouse")) { FasterPlayerPrefs.Singleton.SetBool("InvertMouse", false); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("MouseXSensitivity")) { FasterPlayerPrefs.Singleton.SetFloat("MouseXSensitivity", 0.2f); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("MouseYSensitivity")) { FasterPlayerPrefs.Singleton.SetFloat("MouseYSensitivity", 0.2f); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("ZoomSensitivityMultiplier")) { FasterPlayerPrefs.Singleton.SetFloat("ZoomSensitivityMultiplier", 1); }
            
            if (!FasterPlayerPrefs.Singleton.HasFloat("MobileLookJoystickSensitivity")) { FasterPlayerPrefs.Singleton.SetFloat("MobileLookJoystickSensitivity", 4); }
            if (!FasterPlayerPrefs.Singleton.HasBool("MobileMoveJoystickShouldReposition")) { FasterPlayerPrefs.Singleton.SetBool("MobileMoveJoystickShouldReposition", true); }

            if (!FasterPlayerPrefs.Singleton.HasString("ZoomMode")) { FasterPlayerPrefs.Singleton.SetString("ZoomMode", "TOGGLE"); }
            if (!FasterPlayerPrefs.Singleton.HasString("BlockingMode")) { FasterPlayerPrefs.Singleton.SetString("BlockingMode", "HOLD"); }
            if (!FasterPlayerPrefs.Singleton.HasString("OrbitalCameraMode")) { FasterPlayerPrefs.Singleton.SetString("OrbitalCameraMode", "HOLD"); }

            if (!FasterPlayerPrefs.Singleton.HasFloat("FieldOfView")) { FasterPlayerPrefs.Singleton.SetFloat("FieldOfView", 60); }

            FasterPlayerPrefs.Singleton.SetBool("DisableBots", false);
            FasterPlayerPrefs.Singleton.SetBool("BotsCanOnlyLightAttack", false);

            if (!FasterPlayerPrefs.Singleton.HasBool("AutoAim")) { FasterPlayerPrefs.Singleton.SetBool("AutoAim", true); }

            if (!FasterPlayerPrefs.Singleton.HasBool("ConsoleEnabled")) { FasterPlayerPrefs.Singleton.SetBool("ConsoleEnabled", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("FPSEnabled")) { FasterPlayerPrefs.Singleton.SetBool("FPSEnabled", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("PingEnabled")) { FasterPlayerPrefs.Singleton.SetBool("PingEnabled", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("PacketLossEnabled")) { FasterPlayerPrefs.Singleton.SetBool("PacketLossEnabled", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("JitterEnabled")) { FasterPlayerPrefs.Singleton.SetBool("JitterEnabled", false); }
            if (!FasterPlayerPrefs.Singleton.HasBool("ThermalEventsEnabled")) { FasterPlayerPrefs.Singleton.SetBool("ThermalEventsEnabled", false); }

            if (!FasterPlayerPrefs.Singleton.HasString("Rebinds")) { FasterPlayerPrefs.Singleton.SetString("Rebinds", ""); }

            if (!FasterPlayerPrefs.Singleton.HasBool("ShowHPTextInWorldSpaceLabels")) { FasterPlayerPrefs.Singleton.SetBool("ShowHPTextInWorldSpaceLabels", false); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("UIOpacity")) { FasterPlayerPrefs.Singleton.SetFloat("UIOpacity", 1); }

            if (!FasterPlayerPrefs.Singleton.HasFloat("MasterVolume"))
            {
                FasterPlayerPrefs.Singleton.SetFloat("MasterVolume", 0.4f);
                AudioListener.volume = 0.4f;
            }
            else
            {
                AudioListener.volume = FasterPlayerPrefs.Singleton.GetFloat("MasterVolume");
            }

            if (!FasterPlayerPrefs.Singleton.HasFloat("MusicVolume")) { FasterPlayerPrefs.Singleton.SetFloat("MusicVolume", 0.5f); }

            if (!FasterPlayerPrefs.Singleton.HasBool("PostProcessingEnabled")) { FasterPlayerPrefs.Singleton.SetBool("PostProcessingEnabled", true); }
            if (!FasterPlayerPrefs.Singleton.HasFloat("DPIScalingFactor")) { FasterPlayerPrefs.Singleton.SetFloat("DPIScalingFactor", FasterPlayerPrefs.IsMobilePlatform ? 0.5f : 1); }
            QualitySettings.resolutionScalingFixedDPIFactor = 1;

            foreach (KeyValuePair<string, Color> kvp in FasterPlayerPrefs.GetDefaultColorPrefs())
            {
                if (!FasterPlayerPrefs.Singleton.HasColor(kvp.Key)) { FasterPlayerPrefs.Singleton.SetColor(kvp.Key, kvp.Value); }
            }

            if (!FasterPlayerPrefs.Singleton.HasInt("RenderDistance"))
            {
                int renderDistance = 200;
                switch (QualitySettings.GetQualityLevel())
                {
                    case 0:
                        renderDistance = 100;
                        break;
                    case 1:
                        renderDistance = Application.isMobilePlatform ? 150 : 500;
                        break;
                    case 2:
                        renderDistance = Application.isMobilePlatform ? 200 : 1000;
                        break;
                    default:
                        Debug.LogWarning("Unsure what render distance to assign! " + QualitySettings.GetQualityLevel());
                        break;
                }

                FasterPlayerPrefs.Singleton.SetInt("RenderDistance", renderDistance);
            }

            if (!FasterPlayerPrefs.Singleton.HasString("LightAttackMode")) { FasterPlayerPrefs.Singleton.SetString("LightAttackMode", "HOLD"); }

            VerifyHoldPlayerPref("ZoomMode", 1);
            VerifyHoldPlayerPref("BlockingMode", 0);

            VerifyAttackModePlayerPref("LightAttackMode", 1);
        }

        private void VerifyHoldPlayerPref(string key, int defaultIndex)
        {
            if (!FasterPlayerPrefs.Singleton.HasKey(key)) { Debug.LogError("Calling VerifyHoldPlayerPref but the key isn't present! " + key); return; }

            if (!WeaponHandler.GetHoldToggleOptions().Contains(FasterPlayerPrefs.Singleton.GetString(key)))
            {
                if (defaultIndex < 0 | defaultIndex >= WeaponHandler.GetHoldToggleOptions().Count) { Debug.LogError("(Verify Hold Player Pref) Default Index is not in the list! " + key); return; }
                Debug.LogError(key + " is getting reset! " + FasterPlayerPrefs.Singleton.GetString(key));
                FasterPlayerPrefs.Singleton.SetString(key, WeaponHandler.GetHoldToggleOptions()[defaultIndex]);
            }
        }

        private void VerifyAttackModePlayerPref(string key, int defaultIndex)
        {
            if (!FasterPlayerPrefs.Singleton.HasKey(key)) { Debug.LogError("Calling VerifyHoldPlayerPref but the key isn't present! " + key); return; }

            if (!WeaponHandler.GetAttackModeOptions().Contains(FasterPlayerPrefs.Singleton.GetString(key)))
            {
                if (defaultIndex < 0 | defaultIndex >= WeaponHandler.GetAttackModeOptions().Count) { Debug.LogError("(Verify Hold Player Pref) Default Index is not in the list! " + key); return; }
                Debug.LogError(key + " is getting reset! " + FasterPlayerPrefs.Singleton.GetString(key));
                FasterPlayerPrefs.Singleton.SetString(key, WeaponHandler.GetAttackModeOptions()[defaultIndex]);
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
                downloadProgressBarText.text = "Loading Base Game Logic";
                AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);

                while (true)
                {
                    if (handle.IsDone)
                    {
                        downloadProgressBarImage.fillAmount = 0;
                        downloadProgressBarText.text = "100%";
                        break;
                    }
                    downloadProgressBarImage.fillAmount = handle.PercentComplete;
                    downloadProgressBarText.text = "Loading Base Game Logic " + (handle.PercentComplete * 100).ToString("F0") + "%";
                    yield return null;
                }

                Scene baseScene = SceneManager.GetSceneByName(baseSceneReference.SceneName);
                if (baseScene.isLoaded & baseScene.IsValid())
                {
                    if (SceneManager.SetActiveScene(baseScene))
                    {
                        SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                    }
                    else
                    {
                        headerText.text = "Restart The Game To Try Again";
                        downloadProgressBarText.text = "Error While Loading";
                    }
                }
                else
                {
                    headerText.text = "Restart The Game To Try Again";
                    downloadProgressBarText.text = "Error While Loading";
                }
            }
            else
            {
                headerText.text = "Restart The Game To Try Again";
                downloadProgressBarText.text = "Error While Loading";
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