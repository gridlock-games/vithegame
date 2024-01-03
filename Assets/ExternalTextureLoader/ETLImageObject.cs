using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ETLImageObject : MonoBehaviour
{
  [SerializeField] Image imageTarget;
  [Header("External Links")]

  public bool imageClickable;
  public string webLink;

  public void ChangeImageFile(Texture2D netImage)
  {
    Sprite netSprite = Sprite.Create(netImage, new Rect(0, 0, netImage.width, netImage.height), new Vector2(0.5f, 0.5f));
    imageTarget.sprite = netSprite;
  }

  public void LoadImageFromWeb(string url)
  {
    StartCoroutine(ExternalFileLoaderWeb.DoImageWebRequest(url, ChangeImageFile));
  }
  public void Start()
  {
    LoadImageFromWeb("https://static.wixstatic.com/media/cf53d3_a1bf3cfaa530451abd649042c52ac7ee~mv2.jpg/v1/crop/x_13,y_0,w_355,h_355/fill/w_169,h_169,al_c,q_80,usm_0.66_1.00_0.01,enc_auto/gretel_icon_edited.jpg");
  }


}
