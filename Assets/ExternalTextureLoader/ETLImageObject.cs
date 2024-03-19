using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Vi.Core;

public class ETLImageObject : MonoBehaviour
{
  public string itemID;
  [SerializeField] Image imageTarget;
  public string WWWImageSource;
  [Header("External Links")]
  public bool cacheable;

  public bool imageClickable;
  public string webLink;

  public void ChangeImageFile(Texture2D netImage)
  {
    Sprite netSprite = Sprite.Create(netImage, new Rect(0, 0, netImage.width, netImage.height), new Vector2(0.5f, 0.5f));
    imageTarget.sprite = netSprite;
  }

  public void LoadImageFromWeb()
  {
        if (cacheable)
        {
      StartCoroutine(ExternalFileLoaderWeb.DoImageWebRequestCacheables(itemID, cacheable, ChangeImageFile));
    }
        else
        {
      StartCoroutine(ExternalFileLoaderWeb.DoImageWebRequest(WWWImageSource, ChangeImageFile));
    }
    }
  public void Start()
  {
    LoadImageFromWeb();
  }


}
