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

    }
}