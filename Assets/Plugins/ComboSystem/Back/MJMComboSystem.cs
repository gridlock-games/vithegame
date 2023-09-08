namespace MJM
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.Events;

  [Serializable]
  public class ComboEvent : UnityEvent<int> { }

  public class MJMComboSystem : MonoBehaviour
  {
    //UI
    private int comboCount;
    public ComboEvent onCounterUpdate;

    public void AddCount(int value)
    {
      comboCount += value;
      onCounterUpdate.Invoke(comboCount);
    }

    public void ResetCount()
    {
      comboCount = 0;
      onCounterUpdate.Invoke(comboCount);
    }

    public int GetCount()
    {
      return comboCount;
    }
  }
}
