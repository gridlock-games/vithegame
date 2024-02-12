using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AssetsUpdateCheck : MonoBehaviour
{
  [SerializeField] GameObject notificationWindowCanvas;
  public string cDNFilenotice = $"The game requires Additional Content to be downloaded. it is recomended to download over Wi-fi, Data charges may applied";
  UnityEvent sendToUpdate;

  [SerializeField] bool downloadTest = true;
  // Start is called before the first frame update
  void Start()
    {
    checkForCDNUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void checkForCDNUpdate()
  {
    if (downloadTest)
    {
      NotifyUser();
    }
    else
    {

    }
  }

  void NotifyUser()
  {
    GameObject notificationWindow = notificationWindowCanvas.GetComponentInChildren<MessageNotificationObject>().gameObject;
    MessageNotificationObject messageNotification = notificationWindow.GetComponent<MessageNotificationObject>();
    messageNotification.ShowDialogueBox(cDNFilenotice, "Download Update", sendToUpdate);
    Instantiate(notificationWindowCanvas);
  }
}
