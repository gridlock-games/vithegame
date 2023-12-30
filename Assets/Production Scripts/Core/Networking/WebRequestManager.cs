using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Vi.Core
{
    public static class WebRequestManager
    {
        private const string serverAPIURL = "38.60.245.223/servers/duels";

        public static List<Server> Servers { get; private set; } = new List<Server>();

        public static bool IsRefreshingServers { get; private set; }
        public static IEnumerator GetRequest()
        {
            if (IsRefreshingServers) { yield break; }
            IsRefreshingServers = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(serverAPIURL);
            yield return getRequest.SendWebRequest();

            Servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetRequest() " + serverAPIURL);
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                if (json != "[]")
                {
                    foreach (string jsonSplit in json.Split("},"))
                    {
                        string finalJsonElement = jsonSplit;
                        if (finalJsonElement[0] == '[')
                            finalJsonElement = finalJsonElement.Remove(0, 1);
                        if (finalJsonElement[^1] == ']')
                            finalJsonElement = finalJsonElement.Remove(finalJsonElement.Length - 1, 1);
                        if (finalJsonElement[^1] != '}')
                            finalJsonElement += "}";
                        Servers.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                    }
                }
            }
            catch
            {
                Servers = new List<Server>() { new Server("", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777"), new Server("", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776") };
            }

            getRequest.Dispose();
            IsRefreshingServers = false;
        }

        public static IEnumerator PutRequest(ServerPutPayload payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(serverAPIURL, jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.PutRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public static IEnumerator PostRequest(ServerPostPayload payload)
        {
            WWWForm form = new WWWForm();
            form.AddField("type", payload.type);
            form.AddField("population", payload.population);
            form.AddField("progress", payload.progress);
            form.AddField("ip", payload.ip);
            form.AddField("label", payload.label);
            form.AddField("port", payload.port);

            UnityWebRequest postRequest = UnityWebRequest.Post(serverAPIURL, form);
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.PostRequest()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct Server
        {
            public string _id;
            public int type;
            public int population;
            public int progress;
            public string ip;
            public string label;
            public string __v;
            public string port;

            public Server(string _id, int type, int population, int progress, string ip, string label, string __v, string port)
            {
                this._id = _id;
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.__v = __v;
                this.port = port;
            }
        }

        public struct ServerPostPayload
        {
            public int type;
            public int population;
            public int progress;
            public string ip;
            public string label;
            public string port;

            public ServerPostPayload(int type, int population, int progress, string ip, string label, string port)
            {
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.port = port;
            }
        }

        public struct ServerPutPayload
        {
            public string serverId;
            public int population;
            public int progress;
            public string label;
            public string port;

            public ServerPutPayload(string serverId, int population, int progress, string label, string port)
            {
                this.serverId = serverId;
                this.population = population;
                this.progress = progress;
                this.label = label;
                this.port = port;
            }
        }

        public static List<Character> Characters { get; private set; } = new List<Character>() { new Character("Human_Male", "Char A", 10), new Character("Human_Male", "Char B", 1) };

        public struct Character
        {
            public string characterModelName;
            public string characterName;
            public int characterLevel;
            public string bodyColorName;
            public string headColorName;
            public string eyeColorName;
            public string beardName;
            public string browsName;
            public string hairName;

            public Character(string characterModelName, string characterName, int characterLevel)
            {
                this.characterModelName = characterModelName;
                this.characterName = characterName;
                this.characterLevel = characterLevel;
                bodyColorName = "";
                headColorName = "";
                eyeColorName = "";
                beardName = "";
                browsName = "";
                hairName = "";
            }

            public Character(string characterModelName, string characterName, int characterLevel, string bodyColorName, string headColorName, string eyeColorName, string beardName, string browsName, string hairName)
            {
                this.characterModelName = characterModelName;
                this.characterName = characterName;
                this.characterLevel = characterLevel;
                this.bodyColorName = bodyColorName;
                this.headColorName = headColorName;
                this.eyeColorName = eyeColorName;
                this.beardName = beardName;
                this.browsName = browsName;
                this.hairName = hairName;
            }
        }
    }
}