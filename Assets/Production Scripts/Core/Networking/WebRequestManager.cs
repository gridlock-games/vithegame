using Newtonsoft.Json;
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
        public static IEnumerator ServerGetRequest()
        {
            if (IsRefreshingServers) { yield break; }
            IsRefreshingServers = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(serverAPIURL);
            yield return getRequest.SendWebRequest();

            Servers.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.ServerGetRequest() " + serverAPIURL);
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
                Servers = new List<Server>() { new Server("1", 0, 0, 0, "127.0.0.1", "Hub Localhost", "", "7777"), new Server("2", 1, 0, 0, "127.0.0.1", "Lobby Localhost", "", "7776") };
            }

            getRequest.Dispose();
            IsRefreshingServers = false;
        }

        public static IEnumerator ServerPutRequest(ServerPutPayload payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(serverAPIURL, jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.ServerPutRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public static IEnumerator ServerPostRequest(ServerPostPayload payload)
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
                Debug.LogError("Post request error in WebRequestManager.ServerPostRequest()" + postRequest.error);
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
        
        // TODO Change the string at the end to be the account ID of whoever we sign in under
        private const string characterAPIURL = "https://us-central1-vithegame.cloudfunctions.net/api/characters/";
        private static string currentlyLoggedInUserId = "652b4e237527296665a5059b";

        public static List<Character> Characters { get; private set; } = new List<Character>();

        public static bool IsRefreshingCharacters { get; private set; }
        public static IEnumerator CharacterGetRequest()
        {
            if (IsRefreshingCharacters) { yield break; }
            IsRefreshingCharacters = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(characterAPIURL + currentlyLoggedInUserId);
            yield return getRequest.SendWebRequest();

            Characters.Clear();
            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.CharacterGetRequest() " + characterAPIURL + currentlyLoggedInUserId);
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                Characters = JsonConvert.DeserializeObject<List<Character>>(json);
            }
            catch
            {
                Characters = new List<Character>() { DefaultCharacter, DefaultCharacter };
            }

            getRequest.Dispose();
            IsRefreshingCharacters = false;
        }

        public static IEnumerator CharacterPostRequest(Character character)
        {
            CharacterPostPayload payload = new CharacterPostPayload(character.userId, character.slot, character.eyeColor, character.hair,
                character.bodyColor, character.beard, character.brows, character.name, character.model);

            WWWForm form = new WWWForm();
            form.AddField("userId", payload.userId);
            form.AddField("character", JsonUtility.ToJson(payload.character));
            
            UnityWebRequest postRequest = UnityWebRequest.Post(characterAPIURL + "createCharacterCosmetic", form);
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
            }

            postRequest.Dispose();
        }

        public static Character DefaultCharacter { get; private set; } = new Character("", "Human_Male", "", 0, 1);

        public struct Character
        {
            public string _id;
            public int slot;
            public string name;
            public string model;
            public int experience;
            public string bodyColor;
            public string eyeColor;
            public string beard;
            public string brows;
            public string hair;
            public string dateCreated;
            public CharacterAttributes attributes;
            public string userId;
            public int level;

            public Character(string _id, string model, string name, int experience, int level)
            {
                slot = 0;
                this._id = _id;
                this.model = model;
                this.name = name;
                this.experience = experience;
                bodyColor = "";
                eyeColor = "";
                beard = "";
                brows = "";
                hair = "";
                dateCreated = "";
                attributes = new CharacterAttributes();
                userId = currentlyLoggedInUserId;
                this.level = level;
            }

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level)
            {
                slot = 0;
                this._id = _id;
                this.model = model;
                this.name = name;
                this.experience = experience;
                this.bodyColor = bodyColor;
                this.eyeColor = eyeColor;
                this.beard = beard;
                this.brows = brows;
                this.hair = hair;
                dateCreated = "";
                attributes = new CharacterAttributes();
                userId = currentlyLoggedInUserId;
                this.level = level;
            }
        }

        public struct CharacterAttributes
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;
        }

        private struct CharacterPostPayload
        {
            public string userId;
            public NestedCharacter character;

            public CharacterPostPayload(string userId, int slot, string eyeColor, string hair, string bodyColor, string beard, string brows, string name, string model)
            {
                this.userId = userId;
                character = new NestedCharacter()
                {
                    slot = slot,
                    eyeColor = eyeColor,
                    hair = hair,
                    bodyColor = bodyColor,
                    beard = beard,
                    brows = brows,
                    name = name,
                    model = model
                };
            }

            public struct NestedCharacter
            {
                public int slot;
                public string eyeColor;
                public string hair;
                public string bodyColor;
                public string beard;
                public string brows;
                public string name;
                public string model;
            }
        }
    }
}