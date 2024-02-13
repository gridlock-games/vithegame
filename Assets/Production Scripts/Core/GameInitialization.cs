using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Vi.Core
{
    public class GameInitialization : MonoBehaviour
    {
        [SerializeField] private SceneReference baseSceneReference;
        [SerializeField] private SceneReference mainMenuSceneReference;
        [SerializeField] private Text assetNumberText;
        [SerializeField] private Text downloadProgressBarText;
        [SerializeField] private Image downloadProgressBarImage;

        private void Start()
        {
            StartCoroutine(LoadScenes());
        }

        private IEnumerator LoadScenes()
        {
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

                Addressables.Release(downloadHandle);
            }

            assetNumberText.text = "";
            downloadProgressBarImage.fillAmount = 1;
            downloadProgressBarText.text = "All Downloads Complete";

            Addressables.Release(mainMenuDownloadSize);
            Addressables.Release(baseDownloadSize);

            yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
    }
}