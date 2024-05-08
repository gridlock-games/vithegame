using UnityEngine;
using Vi.UI.SimpleGoogleSignIn;

namespace Vi.UI
{
  public class DeepLinkProcessing : MonoBehaviour
  {
    public static DeepLinkProcessing Instance { get; private set; }
    public string deeplinkURL;

    private loginSiteSource lss;

    public enum loginSiteSource
    {
      inactive,
      google,
      apple,
      facebook,
      elonMistake,
      steam,
      epic,
      origin
    }

    public void SetLoginSource(loginSiteSource source)
    {
      lss = source;
    }

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
      deepLinkTypeProcessing();
    }

    private void deepLinkTypeProcessing()
    {
      string deeplinkID = deeplinkURL.Split('?')[0];
      //Remove "vigamegridlock://"
      deeplinkID = deeplinkID.Replace("vigamegridlock://", "");
      deeplinkID = deeplinkID.Replace("/", "");
      Debug.Log(deeplinkID);

      //Add a check if user is login or not
      switch (deeplinkID)
      {
        case "authentication":
          Debug.Log("Access Login System Deeplink");
          loginVerification();
          return;

        case "profile":
          Debug.Log("Access Profile Deeplink");
          return;

        case "gift":
          Debug.Log("Access gift code system");
          return;

        default:
          return;
      }
    }

    private void loginVerification()
    {
      Debug.Log("LOGIN VERIFYABLE");
      string loginData = deeplinkURL.Split('?')[1];
      Debug.Log(loginData);
      string fullLoginData = "?" + loginData;

      //Temp Setup
      //lss = loginSiteSource.google;

      switch (lss)
      {
        case loginSiteSource.inactive:
          //Do Nothing
          break;

        case loginSiteSource.google:
          //TODO: Add additional verification like source domain checking ie. "www.googleapis.com"
          GoogleAuth.deeplinkListener(fullLoginData);
          break;

        case loginSiteSource.apple:
          break;

        case loginSiteSource.facebook:
          break;

        case loginSiteSource.elonMistake:
          break;

        case loginSiteSource.steam:
          break;

        case loginSiteSource.epic:
          break;

        case loginSiteSource.origin:
          break;

        default:
          break;
      }
      //Sentitive item - Erase Deeplink content after use
      lss = loginSiteSource.inactive;
      deeplinkURL = "[none]";
    }

    //private void demotest()
    //{
    //  // Pass the data to whatever it is needed
    //  DemoDeepLinkPassthru ddlp = this.GetComponent<DemoDeepLinkPassthru>();
    //  if (ddlp != null)
    //  {
    //    Debug.Log("Detected File: " + deeplinkURL);
    //    ddlp.updateOutputText(deeplinkURL);
    //  }
    //}
  }
}
