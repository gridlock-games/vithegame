using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace Vi.Core
{
    public class UserManager : MonoBehaviour
    {
        public void SetOfflineVariables()
        {
            IsLoggedIn = true;
            CurrentlyLoggedInUserId = "";

            if (IsLoggedIn)
            {
                WebRequestManager.Singleton.CharacterManager.RefreshCharacters();
            }
        }

        public IEnumerator CreateAccount(string username, string email, string password)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            CreateAccountPayload payload = new CreateAccountPayload(username, email, password, true);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/create", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/create", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CreateAccount() " + postRequest.error);
            }
            else
            {
                CreateAccountResultPayload badResultPayload = JsonConvert.DeserializeObject<CreateAccountResultPayload>(postRequest.downloadHandler.text);

                if (badResultPayload.mes == null)
                {
                    CreateAccountSuccessPayload goodResultPayload = JsonConvert.DeserializeObject<CreateAccountSuccessPayload>(postRequest.downloadHandler.text);
                }
                else
                {
                    LogInErrorText = badResultPayload.mes;
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    LogInErrorText = "Server Offline";
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    LogInErrorText = "Protocol Error";
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    LogInErrorText = "Data Processing Error";
                    break;
            }

            postRequest.Dispose();
            IsLoggingIn = false;
        }

        private struct CreateAccountPayload
        {
            public string username;
            public string email;
            public string password;
            public bool isPlayer;

            public CreateAccountPayload(string username, string email, string password, bool isPlayer)
            {
                this.username = username;
                this.email = email;
                this.password = password;
                this.isPlayer = isPlayer;
            }
        }

        private struct CreateAccountResultPayload
        {
            public string mes;
        }

        private struct CreateAccountSuccessPayload
        {
            public string username;
            public bool isPlayer;
            public List<string> dateCreated;
            public string id;
        }

        public bool IsLoggedIn { get; private set; }
        public bool IsLoggingIn { get; private set; }
        public string LogInErrorText { get; private set; }
        public string CurrentlyLoggedInUserId { get; private set; } = "";

        public void ResetLogInErrorText() { LogInErrorText = ""; }

        public IEnumerator Login(string username, string password)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            LoginPayload payload = new LoginPayload(username, password);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/login", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/login", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login() " + postRequest.error);
                IsLoggedIn = false;
                CurrentlyLoggedInUserId = "";
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                CurrentlyLoggedInUserId = loginResultPayload.userId;

                if (!IsLoggedIn)
                {
                    LogInErrorText = "Invalid Username or Password";
                    if (postRequest.downloadHandler.text.Contains("isVerified"))
                    {
                        LogInErrorText = "Verify Your Email";
                    }
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    LogInErrorText = "Server Offline";
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    LogInErrorText = "Protocol Error";
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    LogInErrorText = "Data Processing Error";
                    break;
            }

            postRequest.Dispose();
            IsLoggingIn = false;

            if (IsLoggedIn)
            {
                WebRequestManager.Singleton.CharacterManager.RefreshCharacters();
            }
        }

        public IEnumerator LoginWithFirebaseUserId(string email, string firebaseUserId)
        {
            IsLoggingIn = true;
            LogInErrorText = "";
            LoginWithFirebaseUserIdPayload payload = new LoginWithFirebaseUserIdPayload(email, firebaseUserId);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/firebaseAuth", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/firebaseAuth", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.Login() " + postRequest.error);

                IsLoggedIn = false;
                CurrentlyLoggedInUserId = "";
            }
            else
            {
                LoginResultPayload loginResultPayload = JsonConvert.DeserializeObject<LoginResultPayload>(postRequest.downloadHandler.text);
                IsLoggedIn = loginResultPayload.login;
                CurrentlyLoggedInUserId = loginResultPayload.userId;

                if (!IsLoggedIn)
                {
                    LogInErrorText = "Login Failed. This is probably a bug on our end.";
                }
            }

            switch (postRequest.result)
            {
                case UnityWebRequest.Result.InProgress:
                    LogInErrorText = "Request in Progress";
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    LogInErrorText = "Server Offline";
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    LogInErrorText = "Protocol Error";
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    LogInErrorText = "Data Processing Error";
                    break;
            }

            postRequest.Dispose();
            IsLoggingIn = false;

            if (IsLoggedIn)
            {
                WebRequestManager.Singleton.CharacterManager.RefreshCharacters();
            }
        }

        public void Logout()
        {
            IsLoggedIn = false;
            CurrentlyLoggedInUserId = "";
            LogInErrorText = default;

            WebRequestManager.Singleton.CharacterManager.StopCharacterRefresh();
        }

        public struct LoginPayload
        {
            public string username;
            public string password;

            public LoginPayload(string username, string password)
            {
                this.username = username;
                this.password = password;
            }
        }

        private struct LoginResultPayload
        {
            public string userId;
            public bool login;
            public bool isPlayer;

            public LoginResultPayload(string userId, bool login, bool isPlayer)
            {
                this.userId = userId;
                this.login = login;
                this.isPlayer = isPlayer;
            }
        }

        private struct LoginWithFirebaseUserIdPayload
        {
            public string email;
            public string firebaseUserId;

            public LoginWithFirebaseUserIdPayload(string email, string firebaseUserId)
            {
                this.email = email;
                this.firebaseUserId = firebaseUserId;
            }
        }

        public struct CreateUserPayload
        {
            public string username;
            public string email;
            public string password;
            public bool isPlayer;

            public CreateUserPayload(string username, string email, string password)
            {
                this.username = username;
                this.email = email;
                this.password = password;
                isPlayer = true;
            }
        }
    }
}