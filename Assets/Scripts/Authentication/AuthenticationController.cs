using System;
using Assets.SimpleGoogleSignIn;
using Proyecto26;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthenticationController : MonoBehaviour
{
    
    [SerializeField] private GameObject btn_SignIn;
    [SerializeField] private GameObject btn_Signedin;
    [SerializeField] private GameObject btn_SignOut;
    [SerializeField] private GameObject btn_StartGame;
    private DataManager datamanager;
    private void Start()
    {
        datamanager = DataManager.Instance;
        //check if data already cached if not activate sign in page
        if (PlayerPrefs.HasKey("email"))
        {
            RestClient.Get($"{datamanager.firebaseURL}.json", GetCacheResponse);
            btn_Signedin.SetActive(true);
            btn_SignOut.SetActive(true);
            btn_StartGame.SetActive(true);
            return;
        } 
        
        
        btn_SignIn.SetActive(true);
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

                
                btn_Signedin.SetActive(true);
                btn_SignOut.SetActive(true);
                btn_StartGame.SetActive(true);
                return;
            }
            //if Google login fail
            Debug.LogError(error);
        });
    }

    public void StartGame() {
         if (PlayerPrefs.HasKey("email"))
        {
            SceneManager.LoadScene("CharacterSelect");
        } 
    }

}
