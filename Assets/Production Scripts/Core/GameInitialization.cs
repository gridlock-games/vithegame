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
        [SerializeField] private Text downloadProgressBarText;
        [SerializeField] private Image downloadProgressBarImage;

        private void Start()
        {
            StartCoroutine(LoadScenes());
        }

        private IEnumerator LoadScenes()
        {
            AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(baseSceneReference);;
            yield return new WaitUntil(() => downloadSize.IsDone);

            if (downloadSize.Result > 0)
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

                    downloadProgressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                    yield return null;
                }

                Addressables.Release(downloadHandle);
            }

            downloadProgressBarImage.fillAmount = 1;
            downloadProgressBarText.text = "All Downloads Complete";

            Addressables.Release(downloadSize);

            yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
    }
}