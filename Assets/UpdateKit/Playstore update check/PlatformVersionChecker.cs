using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Globalization;

public class PlatformVersionChecker : MonoBehaviour
{
  public string NewVersionMessage = "A new version of Vi Version {0} is now available at {1}. Please update before proceeding.";

  bool MatchingVersion(Version networkVersion)
  {
    var compairingResult = GetCurrentBaseVersion().CompareTo(networkVersion);
    if (compairingResult < 0) //user copy is Outdated
      return false;
    else
      return true;
  }

  //Change this to bool if this will be done as part of the download process otherwise this should be a check upon game bootup.
  void CheckGameVersion()
  {
    Version serverVersion = new Version("1.0.0.1"); //Temp change this to server call.

    bool CheckForlatestVersion = MatchingVersion(serverVersion);
    if (CheckForlatestVersion)
    {
      //Do nothing and continue the process.
    }
    else
    {
      //Notify the user and disable any controls.
      NotifyUser();
    }
  }
  Version GetCurrentBaseVersion()
  {
    string versionInfo = Application.version;
    return new Version(versionInfo);
  }

  void NotifyUser()
  {
    //Insert code for user notification
  }
}
