using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DeepLinkProcessing : MonoBehaviour
{
  public static DeepLinkProcessing Instance { get; private set; }
  public string deeplinkURL;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      Application.deepLinkActivated += onDeepLinkActivated;
      if (!string.IsNullOrEmpty(Application.absoluteURL))
      {
        // Cold start and Application.absoluteURL not null so process Deep Link.
        onDeepLinkActivated(Application.absoluteURL);
      }
      // Initialize DeepLink Manager global variable.
      else deeplinkURL = "[none]";
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void onDeepLinkActivated(string url)
  {
    deeplinkURL = url;
    demotest();
  }

  private void demotest()
  {
    // Pass the data to whatever it is needed
    DemoDeepLinkPassthru ddlp = this.GetComponent<DemoDeepLinkPassthru>();
    if (ddlp != null)
    {
      Debug.Log("Detected File: " + deeplinkURL);
      ddlp.updateOutputText(deeplinkURL);
    }

  }
}