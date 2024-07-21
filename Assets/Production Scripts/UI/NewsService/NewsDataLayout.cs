using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
  public class NewsDataLayout : MonoBehaviour
  {
    [SerializeField] TextMeshProUGUI newsArticleTMP;
    [SerializeField] TextMeshProUGUI newsTitleTMP;
    [SerializeField] Image newsArticleImage;

    public void UpdateArticleUI(string articleData, string articleTitle)
    {
      Debug.Log("Updating article" + articleTitle + articleData);
      newsTitleTMP.text = articleTitle.ToString();
      newsArticleTMP.text = articleData.ToString();
    }

    public void UpdateImageUI(Texture2D texture)
    {
      Sprite toConvert = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width / 2, texture.height / 2));
      newsArticleImage.sprite = toConvert;
    }
  }
}