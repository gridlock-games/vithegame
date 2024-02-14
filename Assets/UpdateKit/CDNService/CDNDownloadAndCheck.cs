using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using static UnityEngine.AddressableAssets.Addressables;

public class CDNDownloadAndCheck : MonoBehaviour
{
  [SerializeField]
  public UnityEvent<float> ProgressEvent;

  [SerializeField]
  public UnityEvent<bool> CompletionEvent;

  [SerializeField]
  public UnityEvent<bool> FailureEvent;

  private bool completeDownloadSuccess = false;

  public (bool checkSuccessful, List<string> toDownload, float totalFileSize) CheckingForUpdate(List<string> key)
  {
    float totalBytes = 0;
    bool successfulUpdateCheck = true;
    List<string> downloadList = new List<string>();
    for (int i = 0; i < key.Count; i++)
    {
      var result = CheckDownloadSize(key[i]);
      if (result.downloadSize > 0 && result.checkingResult == true)
      {
        totalBytes += result.downloadSize;
        downloadList.Add(key[i]);
      }
      else if (result.checkingResult == false)
      {
        //Send a Error that update checking has failed
        FailureEvent.Invoke(false);
        successfulUpdateCheck = false;
        break;
      }
    }
    //If there content on downlist as well totalBytes is > 0 then report that the game need updating.
    // if successful Update check is false then the checks failed and required game restarts
    return (successfulUpdateCheck, downloadList, totalBytes);
  }

  public (bool checkingResult, long downloadSize) CheckDownloadSize(string key)
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

  public IEnumerator DownloadExternalFiles(List<IResourceLocation> locations)
  {
    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(locations, false);
    while (downloadHandle.Status == AsyncOperationStatus.None)
    {
      float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
      float progress = 0;
      if (percentageComplete > progress * 1.1) // Report at most every 10% or so
      {
        progress = percentageComplete; // More accurate %
        ProgressEvent.Invoke(progress);
      }
      if (downloadHandle.Status == AsyncOperationStatus.Failed)
      {
        FailureEvent.Invoke(true);
      }
      if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
      {
        completeDownloadSuccess = true;
      }
      yield return null;
    }
    Addressables.Release(downloadHandle);
    if (completeDownloadSuccess == true)
    {
      CompletionEvent.Invoke(true);
    }
  }

  public IEnumerator DownloadExternalFiles(IEnumerable keys, MergeMode mode)
  {
    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(keys, mode);
    while (downloadHandle.Status == AsyncOperationStatus.None)
    {
      float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
      float progress = 0;
      if (percentageComplete > progress * 1.1) // Report at most every 10% or so
      {
        progress = percentageComplete; // More accurate %
        ProgressEvent.Invoke(progress);
      }
      if (downloadHandle.Status == AsyncOperationStatus.Failed)
      {
        FailureEvent.Invoke(true);
      }
      if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
      {
        completeDownloadSuccess = true;
      }
      yield return null;
    }
    Addressables.Release(downloadHandle);
    if (completeDownloadSuccess == true)
    {
      CompletionEvent.Invoke(true);
    }
  }
}