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
        public Image progressBarImage;
        [SerializeField] private Image iconImage;
        public Text progressBarText;
        [SerializeField] private Text itemText;
        public Image downloadIcon;
        public Image deleteIcon;

        public AssetReference AssetReference { get; private set; }
        private AssetReference defaultAssetReference;
        private List<DownloadButton> mimicDownloadButtons = new List<DownloadButton>();
        public void AddMimicDownloadButtons(DownloadButton downloadButton)
        {
            if (downloadButton == this) { return; }
            mimicDownloadButtons.Add(downloadButton);
        }

        private void MimicButtons()
        {
            foreach (DownloadButton button in mimicDownloadButtons)
            {
                button.progressBarImage.fillAmount = progressBarImage.fillAmount;
                button.progressBarText.text = progressBarText.text;
                button.downloadIcon.gameObject.SetActive(downloadIcon.gameObject.activeSelf);
                button.deleteIcon.gameObject.SetActive(deleteIcon.gameObject.activeSelf);
                button.button.interactable = this.button.interactable;
            }
        }

        public void Initialize(ContentManager.DownloadableAsset downloadableAsset, AssetReference defaultAssetReference)
        {
            AssetReference = downloadableAsset.assetReference;
            this.defaultAssetReference = defaultAssetReference;
            StartCoroutine(Init());
            iconImage.sprite = downloadableAsset.buttonIcon;
            itemText.text = downloadableAsset.name;
        }

        private IEnumerator Init()
        {
            AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(AssetReference);
            AsyncOperationHandle<long> defaultAssetDownloadSize = Addressables.GetDownloadSizeAsync(defaultAssetReference);
            yield return new WaitUntil(() => downloadSize.IsDone);
            yield return new WaitUntil(() => defaultAssetDownloadSize.IsDone);
            
            button.onClick.RemoveAllListeners();
            if (downloadSize.Result > 0 | defaultAssetDownloadSize.Result > 0)
            {
                progressBarText.text = ((downloadSize.Result + defaultAssetDownloadSize.Result) * 0.000001f).ToString("F2") + " MB";
                progressBarImage.fillAmount = 1;
                button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset()); });
                downloadIcon.gameObject.SetActive(true);
            }
            else // We already have this asset downloaded
            {
                progressBarText.text = "Downloaded";
                progressBarImage.fillAmount = 1;
                button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset()); });
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

        private IEnumerator DownloadAsset()
        {
            button.interactable = false;
            downloadIcon.gameObject.SetActive(false);
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(AssetReference);
            AsyncOperationHandle defaultDownloadHandle = Addressables.DownloadDependenciesAsync(defaultAssetReference);
            MimicButtons();

            float lastRateTime = -1;
            float downloadRate = 0;
            float lastBytesAmount = 0;
            float totalMB = (downloadHandle.GetDownloadStatus().TotalBytes + defaultDownloadHandle.GetDownloadStatus().TotalBytes) * 0.000001f;
            while (!downloadHandle.IsDone | !defaultDownloadHandle.IsDone)
            {
                progressBarImage.fillAmount = (downloadHandle.GetDownloadStatus().Percent + defaultDownloadHandle.GetDownloadStatus().Percent) / 2;
                float downloadedMB = (downloadHandle.GetDownloadStatus().DownloadedBytes + defaultDownloadHandle.GetDownloadStatus().TotalBytes) * 0.000001f;
                if (Time.time - lastRateTime >= 1)
                {
                    downloadRate = downloadedMB - lastBytesAmount;
                    lastBytesAmount = downloadedMB;
                    lastRateTime = Time.time;
                }
                progressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                MimicButtons();
                yield return null;
            }

            if (downloadHandle.Status == AsyncOperationStatus.Succeeded & defaultDownloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                progressBarText.text = "Downloaded";

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset()); });
                deleteIcon.gameObject.SetActive(true);
            }
            else
            {
                progressBarText.text = "Error While Downloading";
            }

            button.interactable = true;

            MimicButtons();
            Addressables.Release(downloadHandle);
            Addressables.Release(defaultDownloadHandle);
        }

        private IEnumerator DeleteAsset()
        {
            button.interactable = false;
            deleteIcon.gameObject.SetActive(false);
            AsyncOperationHandle<bool> clearCacheHandle = Addressables.ClearDependencyCacheAsync(AssetReference, false);
            MimicButtons();

            while (!clearCacheHandle.IsDone)
            {
                progressBarImage.fillAmount = clearCacheHandle.PercentComplete;
                progressBarText.text = "Deleting asset...";
                MimicButtons();
                yield return null;
            }

            AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(AssetReference);
            yield return new WaitUntil(() => downloadSize.IsDone);
            progressBarText.text = (downloadSize.Result * 0.000001f).ToString("F2") + " MB";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset()); });
            downloadIcon.gameObject.SetActive(true);
            button.interactable = true;

            MimicButtons();
            Addressables.Release(clearCacheHandle);
        }
    }
}