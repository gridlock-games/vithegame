using System;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;

public class AuthenticationController : MonoBehaviour
{
    private const string clientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
    private const string secretId = "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";
    private const string firebaseURL = "https://vithegame-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [SerializeField] private GameObject signInPage;
    
    private void Start()
    {
        //check if data already cached if not activate sign in page
        if (PlayerPrefs.HasKey("email"))
        {
            RestClient.Get($"{firebaseURL}.json", GetCacheResponse);
            return;
        }
        signInPage.SetActive(true);
    }

    // Callback on getting data and save as user model
    private void GetCacheResponse(RequestException error, ResponseHelper helper)
    {
        var data = AuthHelper.GetUserData(helper.Text, PlayerPrefs.GetString("email"), secretId);
        if (data == null)
        {
            return;
        }
        PostUserdata(data);
    }


    public void SignIn()
    {
        GoogleAuth.Auth(clientId, secretId, (success, error, info) =>
        {
            if (success)
            {
                //Check if data already exist in firebase. data equal to null post new data.
                RestClient.Get($"{firebaseURL}.json", (exception, helper) =>
                {
                    var data = AuthHelper.GetUserData(helper.Text, info.email, secretId);
                    if (data == null)
                    {
                        data = new UserModel
                        {
                            account_name = info.name,
                            email = info.email,
                            display_picture = info.picture,
                            date_created = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                        };
                        PostUserdata(data);
                        return;
                    }

                    PostUserdata(data);
                });
                return;
            }
            //if Google login fail
            Debug.LogError(error);
        });
    }

    private void PostUserdata(UserModel data)
    {
        var _encrypt = AuthHelper.Encrypt(data.email, secretId);
        var _encryptdata = Convert.ToBase64String(_encrypt);
        if (_encryptdata.Contains('/'))
        {
            _encryptdata = _encryptdata.Replace('/', '-');
        }
        data.last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        RestClient.Put($"{firebaseURL}{_encryptdata}/data.json", data, (exception, helper) =>
        {
            DataManager.Instance.LoginSuccess(data);
        });
    }
}
