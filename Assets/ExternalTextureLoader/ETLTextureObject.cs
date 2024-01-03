using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ETLTextureObject : MonoBehaviour
{
  [SerializeField] Renderer objectTarget;
  [SerializeField] int materialID;
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
    StartCoroutine(ExternalFileLoaderWeb.DoImageWebRequest(url, ChangeImageFile));
  }
  public void Start()
  {
    LoadImageFromWeb("https://static.wixstatic.com/media/cf53d3_8094bda33cc54dc7860b13c4cbd83e88~mv2.jpg/v1/fill/w_169,h_169,al_c,q_80,usm_0.66_1.00_0.01,enc_auto/guin_02_edited_edited.jpg");
  }
}
