using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Vi.UI
{
    public class ContentManager : Menu
    {
        [SerializeField] private DownloadGroup downloadGroup;
        [SerializeField] private Transform downloadGroupParent;
        [SerializeField] private DownloadableAssetGroup[] assetGroups;

        [System.Serializable]
        public struct DownloadableAssetGroup
        {
            public string name;
            public AssetReference defaultAssetToDownload;
            public DownloadableAsset[] assetReferences;
        }

        [System.Serializable]
        public struct DownloadableAsset
        {
            public string name;
            public AssetReference assetReference;
            public Sprite buttonIcon;
        }

        private void Start()
        {
            List<DownloadGroup> groupList = new List<DownloadGroup>();

            foreach (DownloadableAssetGroup downloadableAssetGroup in assetGroups)
            {
                DownloadGroup group = Instantiate(downloadGroup.gameObject, downloadGroupParent).GetComponent<DownloadGroup>();
                group.Initialize(downloadableAssetGroup);
                groupList.Add(group);
            }

            List<DownloadButton> buttonList = new List<DownloadButton>();

            foreach (DownloadGroup group in groupList)
            {
                foreach (DownloadButton button in group.downloadButtons)
                {
                    buttonList.Add(button);
                }
            }

            List<AssetReference> assetReferences = new List<AssetReference>();
            foreach (DownloadButton button in buttonList)
            {
                if (!assetReferences.Exists(item => item.RuntimeKey.ToString() == button.AssetReference.RuntimeKey.ToString())) { assetReferences.Add(button.AssetReference); }
            }

            foreach (AssetReference assetReference in assetReferences)
            {
                List<DownloadButton> list = buttonList.FindAll(item => item.AssetReference.RuntimeKey.ToString() == assetReference.RuntimeKey.ToString());
                foreach (DownloadButton downloadButton1 in list)
                {
                    foreach (DownloadButton downloadButton2 in list)
                    {
                        downloadButton1.AddMimicDownloadButtons(downloadButton2);
                    }
                }
            }
        }
    }
}