using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
  public static partial class FacebookAuth
  {
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string AccessScope = "openid email profile";

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
  }
}