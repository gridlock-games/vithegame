using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
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
        Data = _data;
        GameSession(SessionType.LOGIN);
        //TODO: Load Scene
        SceneManager.LoadScene("Prototype");
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
}
