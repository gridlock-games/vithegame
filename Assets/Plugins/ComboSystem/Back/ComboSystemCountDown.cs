namespace MJM
{
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.Events;
  using UnityEngine.UI;

  public class ComboSystemCountDown : MonoBehaviour
  {

    public float timer;
    public float timerDefault = 10;
    public UnityEvent comboCountdownOver;
    public MJMComboSystem cs;
    void Update()
    {
      if (cs.GetCount() >= 1)
      {
        timer -= Time.deltaTime;
      }
      if (timer <= 0)
      {
        ResetCombo();
      }
    }

    public void ResetCombo()
    {
      comboCountdownOver.Invoke();
      Resettimer();
    }
    public void Resettimer()
    {
      timer = timerDefault;
    }
  }
}