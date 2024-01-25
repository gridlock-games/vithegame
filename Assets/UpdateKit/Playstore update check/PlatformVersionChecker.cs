using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;
using UnityEngine.Events;
using Unity.VisualScripting;

public class PlatformVersionChecker : MonoBehaviour
{
  public string NewVersionMessage = $"A new version of Vi Version is now available at playstore. Please update before proceeding.";

  [SerializeField] GameObject notificationWindowCanvas;
  Version serverVersion = new Version("1.0.0.1");

  [SerializeField] UnityEvent playAction;

  //Change this to bool if this will be done as part of the download process otherwise this should be a check upon game bootup.
  public void CheckGameVersion()
  {
    serverVersion = GetCurrentServerVersion();
    bool CheckForlatestVersion = MatchingVersion(serverVersion);
    if (CheckForlatestVersion)
    {
      //Destroy this checker since it no longer needed. (In most cases the server should forced users to reopen the app)
      Destroy(this.gameObject);
    }
    else
    {
      //Notify the user and disable any controls or deny access except the UI button.
      NotifyUser();
    }
  }
  Version GetCurrentBaseVersion()
  {
    string versionInfo = Application.version;
    return new Version(versionInfo);
  }

  
  Version GetCurrentServerVersion()
  {
    //Replace this with the data retrieved from API
    return new Version("1.0.0.1");
  }

  bool MatchingVersion(Version networkVersion)
  {
    var compairingResult = GetCurrentBaseVersion().CompareTo(networkVersion);
    if (compairingResult < 0) //user copy is Outdated
      return false;
    else
      return true;
  }

  public void NotifyUser()
  {
    GameObject notificationWindow =  notificationWindowCanvas.GetComponentInChildren<MessageNotificationObject>().gameObject;
    MessageNotificationObject messageNotification = notificationWindow.GetComponent<MessageNotificationObject>();
    messageNotification.ShowDialogueBox(NewVersionMessage, "Go to PlayStore", playAction);
    Instantiate(notificationWindowCanvas);
  }

  public void SentToStore()
  {
    PlatformDownloadPage.SentPeopleToStore();
  }
}
