using System;
using System.Collections;
using System.Collections.Generic;
using Proyecto26;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    public string clientId = "775793118365-5tfdruavpvn7u572dv460i8omc2hmgjt.apps.googleusercontent.com";
    public string secretId = "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";
    public string firebaseURL = "https://vithegame-default-rtdb.asia-southeast1.firebasedatabase.app/";
    
    
    [SerializeField] private UserSO UserData;

    public UserModel Data {
        get => UserData.Data;
        set => UserData.Data = value;
    }
    public enum SessionType
    {
        LOGIN,
        LOGOUT
    }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoginSuccess(UserModel _data)
    {
        GameSession(SessionType.LOGIN);
        Debug.Log("Login Success");
    }
    public void GameSession(SessionType type)
    {
        if (type == SessionType.LOGIN)
        {
            PlayerPrefs.SetString("email", Data.email);
            return;
        }

        if (!PlayerPrefs.HasKey("email")) return;
        PlayerPrefs.DeleteKey("email");
    }
    
    public void PostUserdata(UserModel data, bool isLogin = true)
    {
        var _encrypt = AuthHelper.Encrypt(data.email, secretId);
        var _encryptdata = Convert.ToBase64String(_encrypt);
        if (_encryptdata.Contains('/'))
        {
            _encryptdata = _encryptdata.Replace('/', '-');
        }
        RestClient.Put($"{firebaseURL}{_encryptdata}/data.json", data, (exception, helper) =>
        {
            Data = data;
            if (!isLogin) return;
            LoginSuccess(data);
        });
    }
}
