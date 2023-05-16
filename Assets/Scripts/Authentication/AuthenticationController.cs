using System;
using System.Linq;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;

public class AuthenticationController : MonoBehaviour
{
    private const string clientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
    private const string secretId = "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";
    private const string firebaseURL = "https://vithegame-default-rtdb.asia-southeast1.firebasedatabase.app/";

    public string email;

    public UserModel _user;
    
    
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestClient.Get($"{firebaseURL}.json", GetResponse);
        }
    }

    
    // Callback on getting data and save as user model
    private void GetResponse(RequestException error, ResponseHelper data)
    {
        _user = AuthHelper.GetUserData(data.Text, email, secretId);
    }


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
        });
    }

    private void PostUserdata(UserModel user)
    {
        var _encrypt = AuthHelper.Encrypt(user.email, secretId);
        var _encryptdata = Convert.ToBase64String(_encrypt);
        if (_encryptdata.Contains('/'))
        {
            _encryptdata = _encryptdata.Replace('/', '-');
        }
        
        RestClient.Put($"{firebaseURL}{_encryptdata}/data.json", user);
    }
}
