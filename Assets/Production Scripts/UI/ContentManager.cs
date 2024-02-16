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
            foreach (DownloadableAssetGroup downloadableAssetGroup in assetGroups)
            {
                DownloadGroup group = Instantiate(downloadGroup.gameObject, downloadGroupParent).GetComponent<DownloadGroup>();
                group.Initialize(downloadableAssetGroup);
            }
        }
    }
}