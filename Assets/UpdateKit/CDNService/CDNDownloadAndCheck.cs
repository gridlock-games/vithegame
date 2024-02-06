using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CDNDownloadAndCheck : MonoBehaviour
{
  [SerializeField]
  private UnityEvent<float> ProgressEvent;

  [SerializeField]
  private UnityEvent<bool> CompletionEvent;

  [SerializeField]
  private UnityEvent<bool> FailureEvent;

  private bool completeDownloadSuccess = false;

  public (bool result, long downloadSize) CheckDownloadSize(string key)
  {
    // Check the download size
    AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync(key);
    long downloadSize = 0;
    if (getDownloadSize.Status == AsyncOperationStatus.Succeeded)
    {
      downloadSize = getDownloadSize.Result;
      return (true, downloadSize);
    }
    else
    {
      return (false, downloadSize);
    }
  }

  public IEnumerator DownloadExternalFiles(List<string> key)
  {
    for (int i = 0; i < key.Count; i++)
    {
      AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(key, false);
      float progress = 0;
      while (downloadHandle.Status == AsyncOperationStatus.None)
      {
        float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
        if (percentageComplete > progress * 1.1) // Report at most every 10% or so
        {
          progress = percentageComplete; // More accurate %
          ProgressEvent.Invoke(progress);
        }
        if (downloadHandle.Status == AsyncOperationStatus.Failed)
        {
          FailureEvent.Invoke(true);
        }
        if (downloadHandle.Status == AsyncOperationStatus.Succeeded && i >= key.Count)
        {
          completeDownloadSuccess = true;
        }
        yield return null;
      }
      Addressables.Release(downloadHandle);
    }

    if (completeDownloadSuccess == true)
    {
      CompletionEvent.Invoke(true);
    }
  }

  public IEnumerator DownloadExternalFilesSingle(string key)
  {
    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(key, false);
    float progress = 0;

    while (downloadHandle.Status == AsyncOperationStatus.None)
    {
      float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
      if (percentageComplete > progress * 1.1) // Report at most every 10% or so
      {
        progress = percentageComplete; // More accurate %
        ProgressEvent.Invoke(progress);
      }
      yield return null;
    }

    switch (downloadHandle.Status)
    {
      case AsyncOperationStatus.Succeeded:
        //alert the download is successful
        break;

      case AsyncOperationStatus.Failed:
        //alert the download has failed
        break;

      default:
        break;
    }
    Addressables.Release(downloadHandle); //Release the operation handle
  }
}
}