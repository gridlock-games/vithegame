using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DemoDeepLinkPassthru : MonoBehaviour
{
  public TextMeshProUGUI outputText;

  public void updateOutputText(string sText)
  {
    outputText.text = sText;
  }
}
