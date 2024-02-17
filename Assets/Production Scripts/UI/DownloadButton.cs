using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.UI
{
    public class DownloadButton : MonoBehaviour
    {
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text progressBarText;
        [SerializeField] private Text itemText;
        [SerializeField] private Image downloadIcon;
        [SerializeField] private Image deleteIcon;

        public void Initialize(ContentManager.DownloadableAsset downloadableAsset)
        {
            StartCoroutine(Init(downloadableAsset.assetReference));
            iconImage.sprite = downloadableAsset.buttonIcon;
            itemText.text = downloadableAsset.name;
        }

        private IEnumerator Init(AssetReference assetReference)
        {
            AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(assetReference);
            yield return new WaitUntil(() => downloadSize.IsDone);

            button.onClick.RemoveAllListeners();
            if (downloadSize.Result > 0)
            {
                progressBarText.text = (downloadSize.Result * 0.000001f).ToString("F2") + " MB";
                progressBarImage.fillAmount = 1;
                button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset(assetReference)); });
                downloadIcon.gameObject.SetActive(true);
            }
            else // We already have this asset downloaded
            {
                progressBarText.text = "Downloaded";
                progressBarImage.fillAmount = 1;
                button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset(assetReference)); });
                deleteIcon.gameObject.SetActive(true);
            }

            button.interactable = true;
        }

        private Button button;
        private void Awake()
        {
            downloadIcon.gameObject.SetActive(false);
            deleteIcon.gameObject.SetActive(false);

            button = GetComponent<Button>();
            button.interactable = false;
            progressBarText.text = "";
        }

        private IEnumerator DownloadAsset(AssetReference assetReference)
        {
            button.interactable = false;
            downloadIcon.gameObject.SetActive(false);
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(assetReference);
            yield return new WaitUntil(() => downloadHandle.IsDone);

            float lastRateTime = -1;
            float downloadRate = 0;
            float lastBytesAmount = 0;
            float totalMB = downloadHandle.GetDownloadStatus().TotalBytes * 0.000001f;
            while (!downloadHandle.IsDone)
            {
                progressBarImage.fillAmount = downloadHandle.GetDownloadStatus().Percent;

                float downloadedMB = downloadHandle.GetDownloadStatus().DownloadedBytes * 0.000001f;

                if (Time.time - lastRateTime >= 1)
                {
                    downloadRate = downloadedMB - lastBytesAmount;
                    lastBytesAmount = downloadedMB;
                    lastRateTime = Time.time;
                }

                progressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                yield return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset(assetReference)); });
            deleteIcon.gameObject.SetActive(true);
            button.interactable = true;
        }

        private IEnumerator DeleteAsset(AssetReference assetReference)
        {
            button.interactable = false;
            deleteIcon.gameObject.SetActive(false);
            Addressables.ClearDependencyCacheAsync(assetReference);

            while (true)
            {
                AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(assetReference);
                yield return new WaitUntil(() => downloadSize.IsDone);

                if (downloadSize.Result == 0) { break; }

                yield return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset(assetReference)); });
            downloadIcon.gameObject.SetActive(true);
            button.interactable = true;
        }
    }
}