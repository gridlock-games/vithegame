namespace MJM
{

  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.UI;
  public class ComboCounterUI : MonoBehaviour
  {
    [SerializeField] private Text comboCounter;

    public void ComboTextUpdate(int value)
    {
      comboCounter.text = value.ToString();
    }
  }
}