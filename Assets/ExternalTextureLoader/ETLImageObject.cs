using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ETLImageObject : MonoBehaviour
{
  [SerializeField] Image imageTarget;
  public string WWWImageSource;
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
    LoadImageFromWeb(WWWImageSource);
  }


}
