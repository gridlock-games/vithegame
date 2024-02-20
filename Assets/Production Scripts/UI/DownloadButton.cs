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
                button.Button.interactable = Button.interactable;
            }
        }

        public void Initialize(ContentManager.DownloadableAsset downloadableAsset)
        {
            AssetReference = downloadableAsset.assetReference;
            StartCoroutine(Init());
            iconImage.sprite = downloadableAsset.buttonIcon;
            itemText.text = downloadableAsset.name;
        }

        private IEnumerator Init()
        {
            AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(AssetReference);
            yield return new WaitUntil(() => downloadSize.IsDone);

            Button.onClick.RemoveAllListeners();
            if (downloadSize.Result > 0)
            {
                progressBarText.text = (downloadSize.Result * 0.000001f).ToString("F2") + " MB";
                progressBarImage.fillAmount = 1;
                Button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset()); });
                downloadIcon.gameObject.SetActive(true);
            }
            else // We already have this asset downloaded
            {
                progressBarText.text = "Downloaded";
                progressBarImage.fillAmount = 1;
                Button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset()); });
                deleteIcon.gameObject.SetActive(true);
            }

            Button.interactable = true;
        }

        public Button Button { get; private set; }
        private void Awake()
        {
            downloadIcon.gameObject.SetActive(false);
            deleteIcon.gameObject.SetActive(false);

            Button = GetComponent<Button>();
            Button.interactable = false;
            progressBarText.text = "";
        }

        private IEnumerator DownloadAsset()
        {
            Button.interactable = false;
            downloadIcon.gameObject.SetActive(false);
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(AssetReference);
            MimicButtons();

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
                MimicButtons();
                yield return null;
            }

            progressBarText.text = "Downloaded";

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset()); });
            deleteIcon.gameObject.SetActive(true);
            Button.interactable = true;

            MimicButtons();
            Addressables.Release(downloadHandle);
        }

        private IEnumerator DeleteAsset()
        {
            Button.interactable = false;
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

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset()); });
            downloadIcon.gameObject.SetActive(true);
            Button.interactable = true;

            MimicButtons();
            Addressables.Release(clearCacheHandle);
        }
    }
}