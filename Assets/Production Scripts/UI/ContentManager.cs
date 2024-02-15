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
        [SerializeField] private DownloadableAssetGroup[] assetGroups;

        [System.Serializable]
        private struct DownloadableAssetGroup
        {
            public string name;
            public AssetReference defaultAssetToDownload;
            public DownloadableAsset[] assetReferences;
        }

        [System.Serializable]
        private struct DownloadableAsset
        {
            public string name;
            public AssetReference assetReference;
        }
    }
}