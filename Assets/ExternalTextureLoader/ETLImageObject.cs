using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ETLImageObject : MonoBehaviour
{
  [SerializeField] Image imageTarget;
  [Header("External Links")]
  public bool imageClickable;
  public string webLink;

  public void ChangeImageFile(Texture2D newImage)
  {
    
  }

  public void Start()
  {
    StartCoroutine(LoadImage("https://static.wixstatic.com/media/cf53d3_8d45dd773de948fdab2f8fc904f23583~mv2.png/v1/fill/w_240,h_228,al_c,q_85,usm_0.66_1.00_0.01,enc_auto/VI_Final%20Logo_whitea-01.png"));
  }

  IEnumerator LoadImage(string link)
  {
    UnityWebRequest request = UnityWebRequestTexture.GetTexture(link);
    yield return request.SendWebRequest();

    if (UnityWebRequest.Result.ProtocolError == request.result)
    {

    }
    else
    {
      Texture2D netTexture = ((DownloadHandlerTexture) request.downloadHandler).texture;
      Sprite netSprite = Sprite.Create(netTexture, new Rect(0,0, netTexture.width, netTexture.height), new Vector2(0.5f,0.5f));

      imageTarget.sprite = netSprite;
    }
  }
}
