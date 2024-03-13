using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Firebase;
using Firebase.Analytics;

namespace Vi.Core
{
    public class GameInitialization : MonoBehaviour
    {
        [SerializeField] private SceneReference baseSceneReference;
        [SerializeField] private SceneReference mainMenuSceneReference;
        [SerializeField] private Text assetNumberText;
        [SerializeField] private Text downloadProgressBarText;
        [SerializeField] private Image downloadProgressBarImage;

        public void ExitGame()
        {
            Application.Quit();
        }

        private void Start()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(continuationAction: task =>
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            });

            Application.targetFrameRate = Screen.currentResolution.refreshRate;
            StartCoroutine(LoadScenes());
            InitializePlayerPrefs();
        }

        private void InitializePlayerPrefs()
        {
            if (!PlayerPrefs.HasKey("InvertMouse")) { PlayerPrefs.SetString("InvertMouse", false.ToString()); }
            if (!PlayerPrefs.HasKey("MouseXSensitivity")) { PlayerPrefs.SetFloat("MouseXSensitivity", 0.2f); }
            if (!PlayerPrefs.HasKey("MouseYSensitivity")) { PlayerPrefs.SetFloat("MouseYSensitivity", 0.2f); }
            if (!PlayerPrefs.HasKey("ZoomSensitivityMultiplier")) { PlayerPrefs.SetFloat("ZoomSensitivityMultiplier", 1); }
            if (!PlayerPrefs.HasKey("ZoomMode")) { PlayerPrefs.SetString("ZoomMode", "TOGGLE"); }
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
                yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(baseSceneReference.SceneName));
                SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            }
        }
    }
}