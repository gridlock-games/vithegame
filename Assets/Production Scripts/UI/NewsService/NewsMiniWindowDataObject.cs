using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Vi.UI
{
  public class NewsMiniWindowDataObject : MonoBehaviour
  {
    [SerializeField] Text newsText;
    [SerializeField] Image newsColor;

    public void updateContent(string nText, Color nColor)
    {
      newsText.text = nText;
      newsColor.color = nColor;
    }

  }
}