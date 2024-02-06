using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class CDNNetworking : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  public (bool result, long downloadSize) CheckDownloadSize(string key)
  {
    // Check the download size
    AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync(key);
    long downloadSize = 0;
    if (getDownloadSize.Status == AsyncOperationStatus.Succeeded)
    {
      downloadSize = getDownloadSize.Result;
      return(true, downloadSize);
    }
    else
    {
      return(false, downloadSize);
    }
  }

  public IEnumerator DownloadExternalFiles(string key)
  {
    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(key, false);
    float progress = 0;

    yield return null;
  }
}
