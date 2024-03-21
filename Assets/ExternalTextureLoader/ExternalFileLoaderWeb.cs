using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Vi.Core;
using static ExternalFileLoaderWeb;
using static GameCreator.Behavior.Behavior;

public class ExternalFileLoaderWeb : MonoBehaviour
{
  private static ExternalFileLoaderWeb _singleton;

  public static ExternalFileLoaderWeb Singleton
  {
    get
    {
      return _singleton;
    }
  }

  private void Awake()
  {
    _singleton = this;
  }
  public WebExternalAssets webExternalAssets { get; private set; }

  private class ExternalAssets
  {
    public WebExternalAssets webExternalAssets;
  }

  public class WebExternalAssets
  {
    public string key;
    public string url;
    public string dateModified;
  }


  private const string APIURL = "154.90.35.191/";

  public void CreateDirectory(string savePath)
  {
    if (!Directory.Exists(savePath))
    {
      Directory.CreateDirectory(savePath);
    }
  }

  private void SaveImage(byte[] rawImageData, string itemID)
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

  private bool checkIfLatestCopy(string fileLocation, string itemID, string LatestDateModify)
  {
    DateTime dt = File.GetLastWriteTime(fileLocation);
    DateTime ldt = DateTime.Parse(LatestDateModify);
    //insert code from API
    int result = DateTime.Compare(dt, ldt);
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

  public IEnumerator DoImageWebRequestCacheables(string itemID, bool chaceable, System.Action<Texture2D> callback)
  {
    UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/getAsset/" + itemID);
    yield return getRequest.SendWebRequest();

    if (getRequest.result != UnityWebRequest.Result.Success)
    {
      Debug.LogError("Get Request Error in WebRequestManager.ExternalAssetsGetAssets() " + getRequest.error + APIURL + "game/getAsset/" + itemID);
      getRequest.Dispose();
      yield break;
    }

    WebExternalAssets gea = JsonConvert.DeserializeObject<WebExternalAssets>(getRequest.downloadHandler.text);
    getRequest.Dispose();

    string savePath = Application.persistentDataPath + "/cache";

    //Check if its cached and uptodate
    if (File.Exists(savePath + '/' + itemID + ".png"))
    {
      if (checkIfLatestCopy(savePath + '/' + itemID + ".png", itemID, gea.dateModified))
      {
        Debug.Log("item exist");
        byte[] bytes = File.ReadAllBytes(savePath + '/' + itemID + ".png");
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);
        callback(texture);
        yield break;
      }
    }

    var url = gea.url;
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

  public IEnumerator DoImageWebRequestID(string itemID, System.Action<Texture2D> callback)
  {
    Debug.Log("Getting Request");
    UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/getAsset/" + itemID);
    yield return getRequest.SendWebRequest();

    if (getRequest.result != UnityWebRequest.Result.Success)
    {
      Debug.LogError("Get Request Error in WebRequestManager.ExternalAssetsGetAssets() " + getRequest.error + APIURL + "game/getAsset/" + itemID);
      getRequest.Dispose();
      //yield break;
    }

    WebExternalAssets gea = JsonConvert.DeserializeObject<WebExternalAssets>(getRequest.downloadHandler.text);
    webExternalAssets = gea;
    getRequest.Dispose();

    var url = gea.url;

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

  public IEnumerator DoImageWebRequest(string url, System.Action<Texture2D> callback)
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

  public IEnumerator DoTextWebRequestID(string itemID, System.Action<String> callback)
  {
    Debug.Log("Getting Request");
    UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/getAsset/" + itemID);
    yield return getRequest.SendWebRequest();

    if (getRequest.result != UnityWebRequest.Result.Success)
    {
      Debug.LogError("Get Request Error in WebRequestManager.ExternalAssetsGetAssets() " + getRequest.error + APIURL + "game/getAsset/" + itemID);
      callback("There was an error retrieving the file");
      getRequest.Dispose();
      yield break;
    }

    WebExternalAssets gea = JsonConvert.DeserializeObject<WebExternalAssets>(getRequest.downloadHandler.text);
    getRequest.Dispose();

    Debug.Log("Doing Web loading");
    //Call API to retreve Data
    var url = gea.url;

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
      yield return request.SendWebRequest();
      if (request.isNetworkError)
      {
        callback("There was an error retrieving the file");
      }
      else
      {
        callback(request.downloadHandler.text);
      }
    }
  }
}


//private IEnumerable ExternalAssetsGetAssets(string key)
//{
//  Debug.Log("Getting Request");
//  UnityWebRequest getRequest = UnityWebRequest.Get(APIURL + "game/getAsset/" + key);
//  yield return getRequest.SendWebRequest();

//  if (getRequest.result != UnityWebRequest.Result.Success)
//  {
//    Debug.LogError("Get Request Error in WebRequestManager.ExternalAssetsGetAssets() " + getRequest.error + APIURL + "game/getAsset/" + key);
//    getRequest.Dispose();
//    //yield break;
//  }

//  WebExternalAssets gea = JsonConvert.DeserializeObject<WebExternalAssets>(getRequest.downloadHandler.text);
//  webExternalAssets = gea;
//  getRequest.Dispose();
//}