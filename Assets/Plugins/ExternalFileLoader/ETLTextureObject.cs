using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace jomarcentermjm.ExternalFileHandler
{
  public class ETLTextureObject : MonoBehaviour
  {
    [SerializeField] Renderer objectTarget;
    [SerializeField] int materialID;
    public string WWWImageSource;
    public string textureName;
    [Header("External Links")]

    public bool imageClickable;
    public string webLink;

    public void ChangeImageFile(Texture2D netImage)
    {
      var forUpdating = objectTarget.materials;
      forUpdating[materialID].SetTexture(textureName, netImage);
      objectTarget.materials = forUpdating;
    }

    public void LoadImageFromWeb(string url)
    {
      StartCoroutine(ExternalFileLoaderWeb.Singleton.DoImageWebRequest(url, ChangeImageFile));
    }
    public void Start()
    {
      LoadImageFromWeb(WWWImageSource);
    }
  }
}