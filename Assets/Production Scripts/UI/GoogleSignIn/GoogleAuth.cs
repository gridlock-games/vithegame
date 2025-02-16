﻿using Proyecto26;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;

namespace Vi.UI.SimpleGoogleSignIn
{
  /// <summary>
  /// https://developers.google.com/identity/protocols/oauth2/native-app
  /// </summary>
  public static partial class GoogleAuth
  {
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string AccessScope = "openid email profile";

    private static string _clientId;
    private static string _clientSecret;
    private static string _redirectUri;
    private static string _state;
    private static string _codeVerifier;

    private static string _redirectUriLocalhost;

    private static Action<bool, string, GoogleIdTokenResponse> _callback;

    private static System.Net.HttpListener httpListener1 = null;
    private static System.IO.Stream output1 = null;

    public static void Auth(string clientId, string clientSecret, Action<bool, string, GoogleIdTokenResponse> callback)
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

    //Not needed for mobile version
    private static void Listen()
    {
      var httpListener = new System.Net.HttpListener();

      httpListener.Prefixes.Add(_redirectUriLocalhost);
      httpListener.Start();

      httpListener1 = httpListener;

      var context = System.Threading.SynchronizationContext.Current;
      var asyncResult = httpListener.BeginGetContext(result => context.Send(HandleHttpListenerCallback, result), httpListener);

      // Block the thread when background mode is not supported to serve HTTP response while the application is not in focus.
      if (!Application.runInBackground) asyncResult.AsyncWaitHandle.WaitOne();
    }

    private static void HandleHttpListenerCallback(object state)
    {
      var result = (IAsyncResult)state;
      var httpListener = (System.Net.HttpListener)result.AsyncState;
      var context = httpListener.EndGetContext(result);

      // Send an HTTP response to the browser to notify the user to close the browser.
      var response = context.Response;

      var buffer = System.Text.Encoding.UTF8.GetBytes(webpageHTML());

      response.ContentLength64 = buffer.Length;

      var output = response.OutputStream;
      output1 = output;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      httpListener.Close();
      HandleAuthResponse(context.Request.QueryString);
    }

    //Implemented as a exploit prevention
    public static void ShutdownListner()
    {
      httpListener1.Close();
      output1.Close();
    }

    //Handles deeplink listener request - MJM
    public static void deeplinkListener(string content)
    {
      Debug.Log("Start verification");
      NameValueCollection dlquery = HttpUtility.ParseQueryString(content);
      HandleAuthResponse(dlquery);
    }

    private static void Auth()
    {
      _state = $"st={Guid.NewGuid().ToString()}$pl=Google";
      _codeVerifier = Guid.NewGuid().ToString();

      var codeChallenge = Utils.CreateCodeChallenge(_codeVerifier);
      var authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(AccessScope)}&prompt=consent&redirect_uri={Uri.EscapeDataString(_redirectUri)}&client_id={_clientId}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256&access_type=offline";

      //Debug.Log("authorizationRequest=" + authorizationRequest);
      Application.OpenURL(authorizationRequest);
    }

    private static void HandleAuthResponse(NameValueCollection parameters)
    {
      Debug.Log("Handle AUTH REESPONSE");
      var error = parameters.Get("error");
      Debug.Log(error);
      if (error != null)
      {
        Debug.Log("Handle AUTH REESPONSE - ERROR");
        _callback?.Invoke(false, error, null);
        return;
      }

      var state = parameters.Get("state");
      var code = parameters.Get("code");
      var scope = parameters.Get("scope");
      Debug.Log(state + " " + code + " " + scope);

      if (state == null || code == null || scope == null) return;

      if (state == _state)
      {
        PerformCodeExchange(code, _codeVerifier);
      }
      else
      {
        Debug.Log("Unexpected response.");
      }
    }

    private static void PerformCodeExchange(string code, string codeVerifier)
    {
      RestClient.Request(new RequestHelper
      {
        Method = "POST",
        Uri = "https://oauth2.googleapis.com/token",
        Params = new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", _clientId },
                {"client_secret", _clientSecret},
                {"code_verifier", codeVerifier},
                {"grant_type","authorization_code"},
                {"redirect_uri", _redirectUri}
            }
      }).Then(
      response =>
      {
        Debug.Log($"{response.Text}");
        GoogleIdTokenResponse data = JsonUtility.FromJson<GoogleIdTokenResponse>(response.Text);

        _callback(true, null, data);
      }).Catch(Debug.LogError);
    }

    /// <summary>
    /// Response object to exchanging the Google Auth Code with the Id Token
    /// </summary>
    [Serializable]
    public class GoogleIdTokenResponse
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
      //For implementation
      string htmlFileLocation = Application.streamingAssetsPath + "/AuthenticationLocalhost.html";

      //PC File read

      string htmlStuff = "<!--Vi authentication page MJM--><!--DO NOT ADD THE AUTHENTICATION JAVASCRIPT ON THIS PAGE - USED FOR THE LOCALHOST BASED AUTHENTICATION-->\r\n<!DOCTYPE html>\r\n<html>\r\n\r\n<head>\r\n    <title>Vi Content Server</title>\r\n    <link rel=\"stylesheet\" href=\"https://unpkg.com/plain-css@latest/dist/plain.min.css\">\r\n    <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">\r\n    <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>\r\n    <link href=\"https://fonts.googleapis.com/css2?family=Jost:ital,wght@0,100..900;1,100..900&display=swap\"\r\n        rel=\"stylesheet\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <meta charset=\"UTF-8\">\r\n    <style>\r\n        * {\r\n            margin: 0;\r\n            padding: 0;\r\n            box-sizing: border-box;\r\n        }\r\n\r\n        .container {\r\n            display: flex;\r\n            flex-direction: column;\r\n            align-items: center;\r\n            justify-content: center;\r\n        }\r\n\r\n        body {\r\n            height: 100%;\r\n            display: flex;\r\n            justify-content: center;\r\n            align-items: center;\r\n            background-color: #252525;\r\n            background-size: cover;\r\n            font-family: \"Jost\", sans-serif;\r\n            background-image: url(https://authentication.vi-assets.com/vibackground.png);\r\n\r\n        }\r\n\r\n        .centerizedA {\r\n            height: auto;\r\n            width: auto;\r\n            position: relative;\r\n\r\n        }\r\n\r\n        .centerizedB {\r\n            padding: 20px;\r\n            border: 2px solid #6d6d6d;\r\n            background-color: #ffffff;\r\n            background-image: url(https://authentication.vi-assets.com/LoginWindow.png);\r\n            background-size: 200%;\r\n            background-position: center;\r\n            text-align: center;\r\n            border-radius: 10px;\r\n            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);\r\n            max-width: 100%;\r\n            width: 100%;\r\n            max-width: 600px;\r\n            display: flex;\r\n            flex-direction: column;\r\n            justify-content: center;\r\n            align-items: center;\r\n            color: white;\r\n\r\n        }\r\n    </style>\r\n</head>\r\n\r\n<body>\r\n    <div class=\"container\">\r\n    <div class=centerizedB> <img src=https://authentication.vi-assets.com/vilogo.png alt=Vi Game Logo\r\n            style=width:184px;height:184px; class=floating_element />\r\n        <h1 id=titleText class=display:inline;>Login Successful</h1>\r\n        <p id=subtitleText>\r\n            You may close this tab and return to Vi</p>\r\n    </div>\r\n</div>\r\n</body>\r\n\r\n</html>";

      return htmlStuff;
    }
  }
}