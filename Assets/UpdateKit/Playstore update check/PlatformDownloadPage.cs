using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformDownloadPage : MonoBehaviour
{
  public void SentPeopleToStore()
  {
#if UNITY_ANDROID
    //Sent to Android Play store
    Application.OpenURL("market://details?id=" + Application.productName);
#endif
#if UNITY_IOS
    Application.OpenURL("itms-apps://itunes.apple.com/app/" + Application.productName);
#endif
  }
}