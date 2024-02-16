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

            progressBarText.text = downloadSize.Result.ToString();

            button.onClick.AddListener(delegate { StartCoroutine(DownloadAsset(assetReference)); });
            button.interactable = true;
        }

        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.interactable = false;
            progressBarText.text = "";
        }

        private IEnumerator DownloadAsset(AssetReference assetReference)
        {
            button.interactable = false;
            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(assetReference);
            yield return new WaitUntil(() => handle.IsDone);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { StartCoroutine(DeleteAsset(assetReference)); });
            button.interactable = true;
        }

        private IEnumerator DeleteAsset(AssetReference assetReference)
        {
            button.interactable = false;
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
            button.interactable = true;
        }
    }
}