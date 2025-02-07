using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Vi.Utility;
using System.Linq;
using Unity.Netcode;

namespace Vi.Core
{
    public class ServerManager : MonoBehaviour
    {
        public string PublicIP { get; private set; }
        public IEnumerator GetPublicIP()
        {
            UnityWebRequest getRequest = UnityWebRequest.Get("http://icanhazip.com");
            yield return getRequest.SendWebRequest();

            servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetPublicIP() " + getRequest.error);
                getRequest.Dispose();
                IsRefreshingServers = false;
                yield break;
            }
            PublicIP = getRequest.downloadHandler.text.Replace("\\r\\n", "").Replace("\\n", "").Trim();
        }

        public bool IsRefreshingServers { get; private set; }
        public Server[] LobbyServers { get; private set; } = new Server[0];
        public Server[] HubServers { get; private set; } = new Server[0];

        private List<Server> servers = new List<Server>();
        public void RefreshServers() { StartCoroutine(ServerGetRequest()); }
        private IEnumerator ServerGetRequest()
        {
            if (IsRefreshingServers) { yield break; }
            IsRefreshingServers = true;

            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels");
            yield return getRequest.SendWebRequest();

            servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.ServerGetRequest() " + getRequest.error + WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels");
                getRequest.Dispose();
                IsRefreshingServers = false;
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
                        servers.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                    }
                }
            }
            catch
            {
                servers = new List<Server>() { new Server("1", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777", ""), new Server("2", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776", "") };
            }

            if (NetworkManager.Singleton.IsServer)
            {
                HubServers = servers.FindAll(item => item.type == 0).ToArray();
                LobbyServers = servers.FindAll(item => item.type == 1).ToArray();
            }
            else // Not the server
            {
                if (FasterPlayerPrefs.Singleton.GetBool("AllowLocalhostServers"))
                {
                    if (servers.Count(item => item.ip == "127.0.0.1") > 0)
                    {
                        servers.RemoveAll(item => item.ip != "127.0.0.1");
                    }
                }
                else
                {
                    servers.RemoveAll(item => item.ip == "127.0.0.1");
                }

                if (FasterPlayerPrefs.Singleton.GetBool("AllowLANServers"))
                {
                    if (servers.Count(item => item.ip.StartsWith("192.168.")) > 0)
                    {
                        servers.RemoveAll(item => !item.ip.StartsWith("192.168."));
                    }
                }
                else
                {
                    servers.RemoveAll(item => item.ip.StartsWith("192.168."));
                }

                HubServers = servers.FindAll(item => item.type == 0).ToArray();
                LobbyServers = servers.FindAll(item => item.type == 1).ToArray();

                if (HubServers.Length == 0)
                {
                    HubServers = LobbyServers.ToArray();
                }
            }

            getRequest.Dispose();
            IsRefreshingServers = false;
        }

        public IEnumerator UpdateServerProgress(int progress)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }
            if (!thisServerCreated) { yield break; }

            ServerProgressPayload payload = new ServerProgressPayload(thisServer._id, progress);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateServerProgress()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator UpdateServerPopulation(int population, string label, string hostCharId)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server put request from a server!"); yield break; }
            if (!thisServerCreated) { yield break; }

            ServerPopulationPayload payload = new ServerPopulationPayload(thisServer._id, population, thisServer.type == 0 ? "Hub" : label == "" ? "Lobby" : label, hostCharId);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateServerPopulation()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        private Server thisServer;
        private bool thisServerCreated;
        public IEnumerator ServerPostRequest(ServerPostPayload payload)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Should only call server post request from a server!"); yield break; }

            yield return ServerGetRequest();

            foreach (Server server in servers)
            {
                if (payload.ip == server.ip & payload.port == server.port)
                {
                    thisServer = server;
                    thisServerCreated = true;
                    Debug.LogWarning("Server already exists in API!");
                    yield break;
                }
            }

            WWWForm form = new WWWForm();
            form.AddField("type", payload.type);
            form.AddField("population", payload.population);
            form.AddField("progress", payload.progress);
            form.AddField("ip", payload.ip);
            form.AddField("label", payload.label);
            form.AddField("port", payload.port);
            form.AddField("hostCharId", payload.hostCharId);

            UnityWebRequest postRequest = UnityWebRequest.Post(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels", form);
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.ServerPostRequest()" + postRequest.error);
                yield break;
            }

            thisServer = JsonConvert.DeserializeObject<Server>(postRequest.downloadHandler.text);

            postRequest.Dispose();

            yield return ServerGetRequest();

            thisServerCreated = true;
        }

        public bool IsDeletingServer { get; private set; }
        public void DeleteServer(string serverId) { StartCoroutine(DeleteServerCoroutine(serverId)); }
        private IEnumerator DeleteServerCoroutine(string serverId)
        {
            IsDeletingServer = true;
            ServerDeletePayload payload = new ServerDeletePayload(serverId);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest deleteRequest = UnityWebRequest.Delete(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels");
            deleteRequest.method = UnityWebRequest.kHttpVerbDELETE;
            deleteRequest.SetRequestHeader("Content-Type", "application/json");
            deleteRequest.uploadHandler = new UploadHandlerRaw(jsonData);
            yield return deleteRequest.SendWebRequest();

            if (deleteRequest.result != UnityWebRequest.Result.Success)
            {
                deleteRequest = UnityWebRequest.Delete(WebRequestManager.Singleton.GetAPIURL(false) + "servers/duels");
                deleteRequest.method = UnityWebRequest.kHttpVerbDELETE;
                deleteRequest.SetRequestHeader("Content-Type", "application/json");
                deleteRequest.uploadHandler = new UploadHandlerRaw(jsonData);
                yield return deleteRequest.SendWebRequest();
            }

            if (deleteRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Delete request error in LobbyManagerNPC.DeleteLobby() " + deleteRequest.error);
            }
            deleteRequest.Dispose();
            IsDeletingServer = false;
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
            public string hostCharId;

            public Server(string _id, int type, int population, int progress, string ip, string label, string __v, string port, string hostCharId)
            {
                this._id = _id;
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.__v = __v;
                this.port = port;
                this.hostCharId = hostCharId;
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
            public string hostCharId;

            public ServerPostPayload(int type, int population, int progress, string ip, string label, string port, string hostCharId)
            {
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.port = port;
                this.hostCharId = hostCharId;
            }
        }

        private struct ServerProgressPayload
        {
            public string serverId;
            public int progress;

            public ServerProgressPayload(string serverId, int progress)
            {
                this.serverId = serverId;
                this.progress = progress;
            }
        }

        private struct ServerPopulationPayload
        {
            public string serverId;
            public int population;
            public string label;
            public string hostCharId;

            public ServerPopulationPayload(string serverId, int population, string label, string hostCharId)
            {
                this.serverId = serverId;
                this.population = population;
                this.label = label;
                this.hostCharId = hostCharId;
            }
        }

        private struct ServerDeletePayload
        {
            public string serverId;

            public ServerDeletePayload(string serverId)
            {
                this.serverId = serverId;
            }
        }

        private void Start()
        {

        }

        private IEnumerator Initialize()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (!FasterPlayerPrefs.IsAutomatedClient)
            {
                yield return GetPublicIP();
                WebRequestManager.Singleton.SetAPIURL("http://" + PublicIP + ":80");
            }
#endif
            yield return null;
        }

        private void Update()
        {
            if (thisServerCreated)
            {
                if (!IsRefreshingServers)
                {
                    RefreshServers();

                    if (thisServer.type == 0) // Hub
                    {
                        if (!System.Array.Exists(HubServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            FasterPlayerPrefs.QuitGame();
                        }
                    }
                    else if (thisServer.type == 1) // Lobby
                    {
                        if (!System.Array.Exists(LobbyServers, item => item._id == thisServer._id))
                        {
                            Debug.Log(thisServer._id + " This server doesn't exist in the API, quitting now");
                            FasterPlayerPrefs.QuitGame();
                        }
                    }
                    else
                    {
                        Debug.LogError("Not sure how to handle server type: " + thisServer.type);
                    }
                }
            }
        }
    }
}