using Proyecto26;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using UnityEngine;

namespace Vi.UI.FBAuthentication
{
  public static partial class FacebookAuth
  {
    private const string AuthorizationEndpoint = "https://www.facebook.com/v20.0/dialog/oauth";
    private const string AccessScope = "public_profile,email,openid";

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

    //Handles deeplink listener request - MJM
    public static void deeplinkListener(string content)
    {
      Debug.Log("Start verification");
      NameValueCollection dlquery = HttpUtility.ParseQueryString(content);
      HandleAuthResponse(dlquery);
    }

    private static void Auth()
    {
      _state = $"st={Guid.NewGuid().ToString()}$plt=Facebook";
      _codeVerifier = Guid.NewGuid().ToString();

      var codeChallenge = Utils.CreateCodeChallenge(_codeVerifier);
      //var authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(AccessScope)}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&client_id={_clientId}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256";
      var authorizationRequest = $"{AuthorizationEndpoint}?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&{AccessScope}&state={_state}&scope={AccessScope}";
      //Debug.Log("authorizationRequest=" + authorizationRequest);
      Application.OpenURL(authorizationRequest);
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

      Debug.Log(context);
      HandleAuthResponse(context.Request.QueryString);
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
      Debug.Log(state + " " + code);

      if (state == null || code == null) return;

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
        Uri = $"https://graph.facebook.com/v20.0/oauth/access_token",
        Params = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"redirect_uri", _redirectUri + "/"},
                {"client_secret", _clientSecret},
                {"code", code}
            }
      }).Then(
      response =>
      {
        //getting long code
        Debug.Log(response.Text);
        FacebookIdTokenResponse data = JsonUtility.FromJson<FacebookIdTokenResponse>(response.Text);
        RestClient.Request(new RequestHelper
        {
          Method = "POST",
          Uri = $"https://graph.facebook.com/v20.0/oauth/access_token",
          Params = new Dictionary<string, string>
            {
                {"grant_type", "fb_exchange_token"},
                {"client_id", _clientId},
                {"client_secret", _clientSecret},
                {"fb_exchange_token", data.access_token}
            }
        }).Then(
            response =>
            {
              Debug.Log(response.Text);
              FacebookIdTokenResponse longCodeData = JsonUtility.FromJson<FacebookIdTokenResponse>(response.Text);
              Debug.Log(longCodeData);
              _callback(true, null, longCodeData);
            });
      }).Catch(Debug.LogError);

      // getting long_code
    }

    private static string webpageHTML()
    {
      string htmlStuff = "<!--Vi authentication page MJM--><!--DO NOT ADD THE AUTHENTICATION JAVASCRIPT ON THIS PAGE - USED FOR THE LOCALHOST BASED AUTHENTICATION-->\r\n<!DOCTYPE html>\r\n<html>\r\n\r\n<head>\r\n    <title>Vi Content Server</title>\r\n    <link rel=stylesheet href=https://unpkg.com/plain-css@latest/dist/plain.min.css>\r\n    <style>\r\n        body {\r\n            background-image: url(https://authentication.vi-assets.com/vibackground.png);\r\n            background-size: cover;\r\n\r\n        }\r\n\r\n        .centerizedA {\r\n            height: auto;\r\n            width: auto;\r\n            position: relative;\r\n\r\n        }\r\n\r\n        .centerizedB {\r\n            padding: 20px;\r\n            border: 2px solid #6d6d6d;\r\n            background-color: #ffffff;\r\n            background-image: url(https://authentication.vi-assets.com/LoginWindow.png);\r\n            background-size: 200%;\r\n            background-position: center;\r\n            text-align: center;\r\n            border-radius: 10px;\r\n            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);\r\n            max-width: 100%;\r\n            width: 100%;\r\n            max-width: 600px;\r\n            display: flex;\r\n            flex-direction: column;\r\n            justify-content: center;\r\n            align-items: center;\r\n            color: white;\r\n\r\n        }\r\n    </style>\r\n</head>\r\n\r\n<body>\r\n    <div class=centerizedB> <img src=https://authentication.vi-assets.com/vilogo.png alt=Vi Game Logo\r\n            style=width:184px;height:184px; class=floating_element />\r\n        <h1 id=titleText class=display:inline;>Login Successful</h1>\r\n        <p id=subtitleText>\r\n            login details have been send to the game, You may close this tab</p>\r\n    </div>\r\n</body>\r\n\r\n</html>";

      return htmlStuff;
    }
  }
}