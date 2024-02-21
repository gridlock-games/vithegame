using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class AssetDownloadManager : MonoBehaviour
    {
        private static AssetDownloadManager _singleton;

        public static AssetDownloadManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Debug.Log("Audio Manager is Null");
                }

                return _singleton;
            }
        }

        private void Awake()
        {
            _singleton = this;
        }
    }
}