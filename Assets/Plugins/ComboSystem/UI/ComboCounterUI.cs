namespace MJM
{

  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.Events;
  using UnityEngine.UI;
  public class ComboCounterUI : MonoBehaviour
  {
    [SerializeField] private Text comboCounter;

    public UnityEvent triggerUpate;
    public void ComboTextUpdate(int value)
    {
      comboCounter.text = value.ToString();

      if (triggerUpate != null)
      {
        triggerUpate.Invoke();
      }
    }
  }
}