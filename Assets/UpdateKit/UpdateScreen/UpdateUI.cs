using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateUI : MonoBehaviour
{
  [SerializeField] TextMeshProUGUI updateDataText;
  [SerializeField] string updatingText = "Updating";
  [SerializeField] Image downloadBarImage;
  // Start is called before the first frame update

  void updateText(string textValue)
  {
    updateDataText.text = textValue;
  }

  private void updateDownloadBar(float value)
  {
    downloadBarImage.fillAmount = Mathf.Lerp(0,1, value);
  }
}
