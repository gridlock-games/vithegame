using System.Collections;
using System.Collections.Generic;
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

    // Start is called before the first frame update
    void Start()
    {
        //Check if device has battery
        if
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  //Handles Check if the battery low
    public bool LowBatteryCheck()
  {
        if (requiredMinimumPercentage == true)
        {
            if (GetBatteryPercentage() <= minimumPlayPercentage && SystemInfo.batteryStatus == BatteryStatus.Charging)
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


}
