using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StorageCheck : MonoBehaviour
{
  [SerializeField] GameObject notificationWindowCanvas;
  public string FileSizeError = $"This device storage space is not suffience to download additional content.";
  UnityEvent refreshGame;
  // Start is called before the first frame update
  void Start()
    {
    refreshGame.AddListener(RestartSoftware);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  void NotifyUser()
  {
    GameObject notificationWindow = notificationWindowCanvas.GetComponentInChildren<MessageNotificationObject>().gameObject;
    MessageNotificationObject messageNotification = notificationWindow.GetComponent<MessageNotificationObject>();
    messageNotification.ShowDialogueBox(FileSizeError, "Retry", refreshGame);
    Instantiate(notificationWindowCanvas);
  }

  void RestartSoftware()
  {

  }
}
