using System;
using System.Collections.Specialized;
using UnityEngine;
using Proyecto26;
using System.Collections.Generic;

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
        private static Action<bool, string, GoogleIdTokenResponse> _callback;

        public static void Auth(string clientId, string clientSecret, Action<bool, string, GoogleIdTokenResponse> callback)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _callback = callback;
            _redirectUri = $"http://localhost:{Utils.GetRandomUnusedPort()}/";

            Auth();
            Listen();

            //if (Application.platform == RuntimePlatform.Android)
            //{
            //    clientSecret = "google.auth";

            //    _clientId = clientId;
            //    _callback = callback;
            //    _redirectUri = $"{clientSecret}:/oauth2callback";

            //    Auth();
            //}
            //else
            //{
            //    _clientId = clientId;
            //    _clientSecret = clientSecret;
            //    _callback = callback;
            //    _redirectUri = $"http://localhost:{Utils.GetRandomUnusedPort()}/";

            //    Auth();
            //    Listen();
            //}
        }

        private static void Listen()
        {
            var httpListener = new System.Net.HttpListener();

            httpListener.Prefixes.Add(_redirectUri);
            httpListener.Start();

            var context = System.Threading.SynchronizationContext.Current;
            var asyncResult = httpListener.BeginGetContext(result => context.Send(HandleHttpListenerCallback, result), httpListener);

            // Block the thread when background mode is not supported to serve HTTP response while the application is not in focus.
            if (!Application.runInBackground) asyncResult.AsyncWaitHandle.WaitOne();
        }

        private static void HandleHttpListenerCallback(object state)
        {
            var result = (IAsyncResult) state;
            var httpListener = (System.Net.HttpListener) result.AsyncState;
            var context = httpListener.EndGetContext(result);

            // Send an HTTP response to the browser to notify the user to close the browser.
            var response = context.Response;


            var buffer = System.Text.Encoding.UTF8.GetBytes($"Success! Please close the browser tab and return to {Application.productName}.");

            response.ContentLength64 = buffer.Length;

            var output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);
            output.Close();
            httpListener.Close();

            HandleAuthResponse(context.Request.QueryString);
        }

        private static void Auth()
        {
            _state = Guid.NewGuid().ToString();
            _codeVerifier = Guid.NewGuid().ToString();

            var codeChallenge = Utils.CreateCodeChallenge(_codeVerifier);
            var authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(AccessScope)}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&client_id={_clientId}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256";

            //Debug.Log("authorizationRequest=" + authorizationRequest);
            Application.OpenURL(authorizationRequest);
        }

        private static void HandleAuthResponse(NameValueCollection parameters)
        {
            var error = parameters.Get("error");

            if (error != null)
            {
                _callback?.Invoke(false, error, null);
                return;
            }

            var state = parameters.Get("state");
            var code = parameters.Get("code");
            var scope = parameters.Get("scope");

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
                {"client_id", _clientId},
                {"client_secret", _clientSecret},
                {"code", code},
                {"code_verifier", codeVerifier},
                {"grant_type","authorization_code"},
                {"redirect_uri", _redirectUri}
            }
            }).Then(
            response =>
            {
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
    }
}