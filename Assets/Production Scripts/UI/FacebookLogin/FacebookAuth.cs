using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Vi.UI.SimpleGoogleSignIn.GoogleAuth;

namespace Vi.UI
{
  public static partial class FacebookAuth
  {
    private const string AuthorizationEndpoint = "https://www.facebook.com/v20.0/dialog/oauth";
    private const string AccessScope = "openid email profile";

    private static string _clientId;
    private static string _clientSecret;
    private static string _redirectUri;
    private static string _state;
    private static string _codeVerifier;

    private static string _redirectUriLocalhost;

    private static Action<bool, string, FacebookIdTokenResponse> _callback;

    private static System.Net.HttpListener httpListener1 = null;
    private static System.IO.Stream output1 = null;

    public static void Auth(string clientId, string clientSecret, Action<bool, string, FacebookIdTokenResponse> callback)
    {
      _clientId = clientId;
      _clientSecret = clientSecret;
      _callback = callback;
      //_redirectUri = $"http://localhost:{Utils.GetRandomUnusedPort()}/";
      _redirectUri = $"https://authentication.vi-assets.com";

      //This can be ignored
      _redirectUriLocalhost = $"http://localhost:7750/";

      Auth();

      //Not needed for mobile version/used on windows version
      if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
      { Listen(); }

    }


    [Serializable]
    public class FacebookIdTokenResponse
    {
      public string access_token;
      public int expires_in;
      public string id_token;
      public string refresh_token;
      public string scope;
      public string token_type;
    }

    private static string webpageHTML()
    {
      string htmlStuff = "<!--Vi authentication page MJM-->\r\n<!--DO NOT ADD THE AUTHENTICATION JAVASCRIPT ON THIS PAGE - USED FOR THE LOCALHOST BASED AUTHENTICATION-->\r\n\r\n<!DOCTYPE html>\r\n<html>\r\n\r\n<head>\r\n    <title>Vi Content Server</title>\r\n    <link rel=\"stylesheet\" href=\"https://unpkg.com/plain-css@latest/dist/plain.min.css\">\r\n\r\n    <style>\r\n        body {\r\n            background-image: url(\"https://authentication.vi-assets.com/vibackground.png\");\r\n            background-size: cover;\r\n        }\r\n\r\n        .centerizedA {\r\n            height: auto;\r\n            width: auto;\r\n            position: relative;\r\n        }\r\n\r\n        .centerizedB {\r\n            background-image: url(\"https://authentication.vi-assets.com/LoginWindow.png\");\r\n            background-position: center;\r\n            border: 2px solid gray;\r\n            border-radius: 5px;\r\n            text-align: center;\r\n            padding-right: 15px;\r\n            padding-left: 15px;\r\n            margin-right: auto;\r\n            margin-left: auto;\r\n            position: relative;\r\n            background-color: white;\r\n            padding-bottom: 15px;\r\n            padding-top: 15px;\r\n            width: 50%;\r\n            color: white;\r\n\r\n            top: 50%;\r\n            transform: translate(0, -50%);\r\n        }\r\n    </style>\r\n\r\n</head>\r\n\r\n<body>\r\n    <div class=\"centerizedB\">\r\n        <img src=\"https://authentication.vi-assets.com/vilogo.png\" alt=\"Vi Game Logo\" style=\"width:184px;height:184px;\" class=\"floating_element\" />\r\n        <h1 id=\"titleText\" class=\" display:inline;\">Authentication Credentials transferred!</h1>\r\n        <p id=\"subtitleText\">your authentication credentials have been send to the game, You may close this tab</p>\r\n    </div>\r\n</body>\r\n\r\n\r\n</html>";

      return htmlStuff;
    }
  }
}