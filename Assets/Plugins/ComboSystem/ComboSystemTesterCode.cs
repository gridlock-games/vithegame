using MJM;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ComboSystemTesterCode : MonoBehaviour
{

  public float timer;
  public float timerDefault = 10;
  public MJMComboSystem cs;
  public Text timerText;

  // Start is called before the first frame update
  void Start()
    {
    timer = timerDefault;
    }

    // Update is called once per frame
    void Update()
    {
    if (cs.GetCount() >= 1)
    {
      timer -= Time.deltaTime;
      timerText.text = timer.ToString();
    }
    if (timer <= 0)
    {
      ResetCombo();
    }
    }

  public void ResetCombo()
  {
    cs.ResetCount();
    Resettimer();
  }
  public void Resettimer()
  {
    timer = timerDefault;
  }

  public void CallHit()
  {
    cs.AddCount(1);
      Resettimer();
  }
}
