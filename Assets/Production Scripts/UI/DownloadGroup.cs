using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

namespace Vi.UI
{
    public class DownloadGroup : MonoBehaviour
    {
        [SerializeField] private DownloadButton downloadButton;
        [SerializeField] private Transform buttonParent;
        [SerializeField] private Text headerText;

        public List<DownloadButton> downloadButtons { get; private set; } = new List<DownloadButton>();

        public void Initialize(ContentManager.DownloadableAssetGroup downloadableAssetGroup)
        {
            headerText.text = downloadableAssetGroup.name;
            foreach (ContentManager.DownloadableAsset downloadableAsset in downloadableAssetGroup.assetReferences)
            {
                DownloadButton button = Instantiate(downloadButton.gameObject, buttonParent).GetComponent<DownloadButton>();
                button.Initialize(downloadableAsset, downloadableAssetGroup.defaultAssetToDownload);
                downloadButtons.Add(button);
            }
        }
    }
}