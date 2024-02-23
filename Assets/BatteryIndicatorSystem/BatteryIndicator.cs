using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BatteryIndicator : MonoBehaviour
{
  [Range(0, 100)]
  public float minimumPlayPercentage = 10.0f;

  public bool requiredMinimumPercentage = false;
  public Image batteryIconOutline;
  public Image batteryIconBackground;

  public Color normalBetteryColor;
  public Color lowBetteryColor;

  public GameObject batteryNotificationBG;
  public TextMeshProUGUI batteryMessageObject;
  public string lowBatteryWarningmessage = "Low Battery";

  public float batteryCheckinterval = 5.00f;

  // Start is called before the first frame update
  private void Start()
  {
    if (SystemInfo.batteryStatus != BatteryStatus.Unknown)
    {
      StartCoroutine("BatteryCheckCycle");
    }
  }

  // Update is called once per frame
  private void Update()
  {
  }

  //Handles Check if the battery low
  public bool LowBatteryCheck()
  {
    if (requiredMinimumPercentage == true)
    {
      if (GetBatteryPercentage() <= minimumPlayPercentage && SystemInfo.batteryStatus == BatteryStatus.Discharging)
      {
        return false;
      }
      else
      {
        return true;
      }
    }
    else
    {
      return true;
    }
  }

  public void UpdateBatteryUI()
  {
    batteryIconBackground.fillAmount = SystemInfo.batteryLevel;
    if (LowBatteryCheck())
    {
      changeUIColor(normalBetteryColor);
    }
    else
    {
      changeUIColor(lowBetteryColor);
    }
  }

  public float GetBatteryPercentage()
  {
    float percentage = SystemInfo.batteryLevel * 100;
    return percentage;
  }

  public void changeUIColor(Color newColor)
  {
    batteryIconOutline.color = newColor;
    batteryIconBackground.color = newColor;
  }

  private IEnumerator BatteryCheckCycle()
  {
    while (true)
    {
      UpdateBatteryUI();
      if (LowBatteryCheck() && SystemInfo.batteryStatus == BatteryStatus.Discharging)
      {
        batteryNotificationBG.SetActive(true);
      }
      else
      {
        batteryNotificationBG.SetActive(false);
      }
      yield return new WaitForSeconds(batteryCheckinterval);
    }
  }
}