using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
namespace Vi.UI
{
  public class NewsButtonUI : MonoBehaviour
  {

    public int newsArticleID = 0;
    [SerializeField] Text newsTitleText;
    [SerializeField] Image newArticleIcon;

    public NewsButtonUI(string titleText, Sprite articleIcon)
    {
      newsTitleText.text = titleText;
      newArticleIcon.sprite = articleIcon;
    }

    public void updateContents(string titleText, Sprite articleIcon)
      {
      newsTitleText.text = titleText;
      newArticleIcon.sprite = articleIcon;
      }
  }
}