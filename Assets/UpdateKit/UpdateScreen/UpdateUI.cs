using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpdateUI : MonoBehaviour
{
  [SerializeField] TextMeshProUGUI updateDataText;
  [SerializeField] string updatingText = "Updating";
  // Start is called before the first frame update

  void updateText(string textValue)
  {
    updateDataText.text = textValue;
  }
}
