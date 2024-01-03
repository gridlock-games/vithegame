using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class ExternalFileLoaderWeb
{
  public static IEnumerator DoImageWebRequest(string url, System.Action<Texture2D> callback)
  {
    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
    {
      yield return request.SendWebRequest();

      if (request.isNetworkError)
      {
        //Show alt image
      }
      else
      {
        callback(((DownloadHandlerTexture)request.downloadHandler).texture);
      }
    }
  }
}
