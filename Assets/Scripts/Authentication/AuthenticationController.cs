using System;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;

public class AuthenticationController : MonoBehaviour
{
    
    [SerializeField] private GameObject signInPage;
    private DataManager datamanager;
    private void Start()
    {
        datamanager = DataManager.Instance;
        //check if data already cached if not activate sign in page
        if (PlayerPrefs.HasKey("email"))
        {
            RestClient.Get($"{datamanager.firebaseURL}.json", GetCacheResponse);
            return;
        }
        signInPage.SetActive(true);
    }

    // Callback on getting data and save as user model
    private void GetCacheResponse(RequestException error, ResponseHelper helper)
    {
        var data = AuthHelper.GetUserData(helper.Text, PlayerPrefs.GetString("email"), datamanager.secretId);
        if (data == null)
        {
            return;
        }
        data.last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        datamanager.PostUserdata(data);
    }


    public void SignIn()
    {
        GoogleAuth.Auth(datamanager.clientId, datamanager.secretId, (success, error, info) =>
        {
            if (success)
            {
                //Check if data already exist in firebase. data equal to null post new data.
                RestClient.Get($"{datamanager.firebaseURL}.json", (exception, helper) =>
                {
                    var data = AuthHelper.GetUserData(helper.Text, info.email, datamanager.secretId);
                    if (data == null)
                    {
                        data = new UserModel
                        {
                            account_name = info.name,
                            email = info.email,
                            display_picture = info.picture,
                            date_created = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                            last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                        };
                        datamanager.PostUserdata(data);
                        return;
                    }
                    data.last_login = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                    datamanager.PostUserdata(data);
                });
                return;
            }
            //if Google login fail
            Debug.LogError(error);
        });
    }


}
