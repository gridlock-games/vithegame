using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Vi.Core;
using static Vi.Core.WebRequestManager;

public static class ExternalFileLoaderWeb
{
  public static void CreateDirectory(string savePath)
  {
    if (!Directory.Exists(savePath))
    {
      Directory.CreateDirectory(savePath);
    }
  }

  private static void SaveImage(byte[] rawImageData, string itemID)
  {
    Debug.Log("saving file");
    string savePath = Application.persistentDataPath + "/cache";
    try
    {
      // check if directory exists, if not create it
      CreateDirectory(savePath);
      File.WriteAllBytes(savePath + '/' + itemID + ".png", rawImageData);
    }
    catch (Exception e)
    {
      Debug.Log(e.Message);
    }
  }

  //todo: Impletement API code
  public static GameExternalAssets RetreveAPIData(string itemID)
  {
    return WebRequestManager.Singleton.getExternalAssets(itemID);
  }

  private static bool checkIfLatestCopy(string fileLocation, string itemID)
  {
    DateTime dt = File.GetLastWriteTime(fileLocation);


    //insert code from API
    int result = DateTime.Compare(dt, DateTime.Now);
    if (result == 0)
    {
      return true;
    }
    else if (result < 0)
    {
      return true;  
    }
    else
    {
      return false;
    }
  }

  public static IEnumerator DoImageWebRequestCacheables(string itemID, bool chaceable, System.Action<Texture2D> callback)
  {
    string savePath = Application.persistentDataPath + "/cache";
    //Check if its chaceable
    if (File.Exists(savePath + '/' + itemID + ".png"))
    {
      if (checkIfLatestCopy(savePath + '/' + itemID + ".png", itemID))
      {
        Debug.Log("item exist");
        byte[] bytes = File.ReadAllBytes(savePath + '/' + itemID + ".png");
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);
        callback(texture);
      }
    }


    else
    {
      GameExternalAssets assetsInfo = RetreveAPIData(itemID);
      var url = assetsInfo.url;
      //Call Image downloader to retreve data, save it to storage then callback as texture
      using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
      {
        Debug.Log("Getting from Network");
        yield return request.SendWebRequest();

        if (request.isNetworkError)
        {
          //Show alt image
          Debug.Log("Cannot download Image");
        }
        else
        {
          if (chaceable)
          {
            CreateDirectory(savePath);
            byte[] results = request.downloadHandler.data;
            SaveImage(results, itemID);
          }
          callback(((DownloadHandlerTexture)request.downloadHandler).texture);
        }
      }
    }
  }

  public static IEnumerator DoImageWebRequestID(string itemID, System.Action<Texture2D> callback)
  {
    //Call API to retreve Data
    GameExternalAssets assetsInfo = RetreveAPIData(itemID);
    var url = assetsInfo.url;

    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
    {
      yield return request.SendWebRequest();

      if (request.isNetworkError)
      {
        //Show alt image
        Debug.Log("Cannot download Image");
      }
      else
      {
        callback(((DownloadHandlerTexture)request.downloadHandler).texture);
      }
    }
  }

  public static IEnumerator DoImageWebRequest(string url, System.Action<Texture2D> callback)
  {
    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
    {
      yield return request.SendWebRequest();
      if (request.isNetworkError)
      {
        //Show alt image
        Debug.Log("Cannot download Image");
      }
      else
      {
        callback(((DownloadHandlerTexture)request.downloadHandler).texture);
      }
    }
  }

}
