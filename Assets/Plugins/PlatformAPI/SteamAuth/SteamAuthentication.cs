using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using Codice.CM.WorkspaceServer.DataStore;
using Firebase.Auth;
using Mono.Cecil.Cil;
using System.Collections.Specialized;
using System.Web;
using Proyecto26;
using System.Text;
using Google.MiniJSON;



#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS

using Steamworks;

#endif


namespace jomarcentermjm.PlatformAPI
{
  [Serializable]
  public class SteamUserAccountData
  {
    public string firebaseToken;
    public uint steamID;
    public string userEmail;
  }
  public static partial class SteamAuthentication
  {
    private const string AuthorizationProcessingEndpoint = "steamauth.vi-assets.com/";
    // Start is called before the first frame update
    //private const string APIURL = "http://steamauth.vi-assets.com/";
    private const string APIURL = "http://localhost:1337/";
    private static Action<bool, string, FirebaseUser, SteamUserAccountData, string> _callback;

    public static void Auth(Action<bool, string, FirebaseUser, SteamUserAccountData, string> callback)
    {
      _callback = callback;
      if (!SteamAPI.Init())
      {
        Debug.LogError("Failed to initialize Steam API.");
        return;
      }

      //Get a session Ticket
      byte[] sessionTicket = new byte[1024];
      uint ticketSize;
      HAuthTicket authTicketHandle;
      SteamNetworkingIdentity identity;
      identity = new SteamNetworkingIdentity();

      identity.SetSteamID(SteamUser.GetSteamID());

      authTicketHandle = Steamworks.SteamUser.GetAuthSessionTicket(sessionTicket, 1024, out ticketSize, ref identity);
      if (authTicketHandle != HAuthTicket.Invalid)
      {
        string sessionTicketDataString = BitConverter.ToString(sessionTicket, 0, (int)ticketSize).Replace("-", "");
        Debug.Log(sessionTicketDataString);
        SteamSendSessionTicketToServer(sessionTicketDataString);
      }

      void SteamSendSessionTicketToServer(string sessionTicket)
      {

        string convertingData = $"{{\r\n    \"sessionTicket\" : \"{sessionTicket}\"\r\n}}";
        Debug.Log(sessionTicket);
        byte[] convertedST = Encoding.ASCII.GetBytes(convertingData);
        RestClient.Request(new RequestHelper
        {
          Method = "POST",
          Uri = $"{APIURL}steamticketverify",
          ContentType = "application/json",
          BodyRaw = convertedST
        }).Then(
response =>
{
  //getting long code
  Debug.Log(response.Text);
  SteamUserAccountData steamuseraccountdata = JsonUtility.FromJson<SteamUserAccountData>(response.Text);
  SignInWithFirebaseToken(steamuseraccountdata.firebaseToken, steamuseraccountdata);

}).Catch(Debug.LogError);

      }

      void SignInWithFirebaseToken(string customToken, SteamUserAccountData steamuseraccountdata)
      {
        FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        auth.SignInWithCustomTokenAsync(customToken).ContinueWith(task => {
          if (task.IsFaulted)
          {
            Debug.LogError("Firebase sign-in failed: " + task.Exception);
          }
          else if (task.IsCompleted)
          {
            FirebaseUser newUser = task.Result.User;
            Debug.Log(newUser.ToString());
            _callback(true, null , newUser, steamuseraccountdata, SteamFriends.GetPersonaName());
            
            //Send to MainMenu UI
            //Firebase.Auth.FirebaseUser newUser = task.Result;
            //Debug.Log("User signed in successfully: " + newUser.UserId);
          }
        });
      }

    }




    private static string webpageHTML()
    {
      string htmlStuff = "<!--Vi authentication page MJM-->\r\n<!--DO NOT ADD THE AUTHENTICATION JAVASCRIPT ON THIS PAGE - USED FOR THE LOCALHOST BASED AUTHENTICATION-->\r\n\r\n<!DOCTYPE html>\r\n<html>\r\n\r\n<head>\r\n    <title>Vi Content Server</title>\r\n    <link rel=\"stylesheet\" href=\"https://unpkg.com/plain-css@latest/dist/plain.min.css\">\r\n\r\n    <style>\r\n        body {\r\n            background-image: url(\"https://authentication.vi-assets.com/vibackground.png\");\r\n            background-size: cover;\r\n        }\r\n\r\n        .centerizedA {\r\n            height: auto;\r\n            width: auto;\r\n            position: relative;\r\n        }\r\n\r\n        .centerizedB {\r\n            background-image: url(\"https://authentication.vi-assets.com/LoginWindow.png\");\r\n            background-position: center;\r\n            border: 2px solid gray;\r\n            border-radius: 5px;\r\n            text-align: center;\r\n            padding-right: 15px;\r\n            padding-left: 15px;\r\n            margin-right: auto;\r\n            margin-left: auto;\r\n            position: relative;\r\n            background-color: white;\r\n            padding-bottom: 15px;\r\n            padding-top: 15px;\r\n            width: 50%;\r\n            color: white;\r\n\r\n            top: 50%;\r\n            transform: translate(0, -50%);\r\n        }\r\n    </style>\r\n\r\n</head>\r\n\r\n<body>\r\n    <div class=\"centerizedB\">\r\n        <img src=\"https://authentication.vi-assets.com/vilogo.png\" alt=\"Vi Game Logo\" style=\"width:184px;height:184px;\" class=\"floating_element\" />\r\n        <h1 id=\"titleText\" class=\" display:inline;\">Authentication Credentials transferred!</h1>\r\n        <p id=\"subtitleText\">your authentication credentials have been send to the game, You may close this tab</p>\r\n    </div>\r\n</body>\r\n\r\n\r\n</html>";

      return htmlStuff;
    }


  }

}
