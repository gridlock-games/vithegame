using System;
using System.Linq;
using System.Text;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;

public class AuthenticationController : MonoBehaviour
{
    private const string clientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
    private const string secretId = "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";
    private const string firebaseURL = "https://vithegame-default-rtdb.asia-southeast1.firebasedatabase.app/";
    
    public void SignIn()
    {
        GoogleAuth.Auth(clientId, secretId, (success, error, info) =>
        {
            if (success)
            {
                var user = new UserModel
                {
                    account_name = info.name,
                    email = info.email,
                    display_picture = info.picture,
                    last_login = DateTime.Now.ToString("MM/dd/yyyy")
                };
                PostUserdata(user);
                return;
            }
            Debug.Log(error);
        });
    }

    private void PostUserdata(UserModel user)
    {
        var _encrypt = AuthHelper.Encrypt(user.email, secretId);
        RestClient.Put($"{firebaseURL}{Convert.ToBase64String(_encrypt)}/data.json", user);
        
        string decryptedString = AuthHelper.Decrypt(Convert.FromBase64String(Convert.ToBase64String(_encrypt)), secretId);
        Debug.Log(decryptedString);
    }
}
