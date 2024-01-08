using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;

namespace Vi.Core
{
    public class WebRequestManager : MonoBehaviour
    {
        private static WebRequestManager _singleton;

        public static WebRequestManager Singleton
        {
            get
            {
                return _singleton;
            }
        }

        private void Awake()
        {
            _singleton = this;
        }

        private const string serverAPIURL = "38.60.245.223/servers/duels";

        public List<Server> Servers { get; private set; } = new List<Server>();

        public bool IsRefreshingServers { get; private set; }
        public void RefreshServers() { StartCoroutine(ServerGetRequest()); }
        private IEnumerator ServerGetRequest()
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

        public IEnumerator ServerPutRequest(ServerPutPayload payload)
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

        public IEnumerator ServerPostRequest(ServerPostPayload payload)
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
        private string currentlyLoggedInUserId = "652b4e237527296665a5059b";

        public List<Character> Characters { get; private set; } = new List<Character>();

        public bool IsRefreshingCharacters { get; private set; }
        public void RefreshCharacters() { StartCoroutine(CharacterGetRequest()); }
        private IEnumerator CharacterGetRequest()
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
                Characters = new List<Character>() { GetDefaultCharacter(), GetDefaultCharacter() };
            }

            getRequest.Dispose();
            IsRefreshingCharacters = false;
        }

        public bool IsGettingCharacterById { get; private set; }
        public Character CharacterById { get; private set; }
        public void GetCharacterById(string characterId) { StartCoroutine(CharacterByIdGetRequest(characterId)); }

        private IEnumerator CharacterByIdGetRequest(string characterId)
        {
            if (IsGettingCharacterById) { yield break; }
            IsGettingCharacterById = true;
            UnityWebRequest getRequest = UnityWebRequest.Get(characterAPIURL + "getCharacter/" + characterId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.CharacterByIdGetRequest() " + characterAPIURL + "getCharacter/" + characterId);
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;
            try
            {
                CharacterById = JsonConvert.DeserializeObject<Character>(json);
            }
            catch
            {
                CharacterById = GetDefaultCharacter();
            }

            getRequest.Dispose();
            IsGettingCharacterById = false;
        }

        public IEnumerator CharacterPutRequest(Character character)
        {
            CharacterPutPayload payload = new CharacterPutPayload(character._id.ToString(), character.slot, character.eyeColor.ToString(), character.hair.ToString(),
                character.bodyColor.ToString(), character.beard.ToString(), character.brows.ToString(), character.name.ToString(), character.model.ToString());

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(characterAPIURL + "updateCharacterCosmetic", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.ServerPutRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public IEnumerator CharacterPostRequest(Character character)
        {
            CharacterPostPayload payload = new CharacterPostPayload(character.userId.ToString(), character.slot, character.eyeColor.ToString(), character.hair.ToString(),
                character.bodyColor.ToString(), character.beard.ToString(), character.brows.ToString(), character.name.ToString(), character.model.ToString());

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(characterAPIURL + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public IEnumerator CharacterDeleteRequest(string characterId)
        {
            Debug.Log("TODO: character delete request");
            yield return new WaitForSeconds(3);
        }

        public Character GetDefaultCharacter() { return new Character("", "Human_Male", "", 0, 1); }

        public struct Character : INetworkSerializable
        {
            public FixedString32Bytes _id;
            public int slot;
            public FixedString32Bytes name;
            public FixedString32Bytes model;
            public int experience;
            public FixedString32Bytes bodyColor;
            public FixedString32Bytes eyeColor;
            public FixedString32Bytes beard;
            public FixedString32Bytes brows;
            public FixedString32Bytes hair;
            public FixedString128Bytes dateCreated;
            public CharacterAttributes attributes;
            public FixedString32Bytes userId;
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
                userId = Singleton.currentlyLoggedInUserId;
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
                userId = Singleton.currentlyLoggedInUserId;
                this.level = level;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _id);
                serializer.SerializeValue(ref slot);
                serializer.SerializeValue(ref name);
                serializer.SerializeValue(ref model);
                serializer.SerializeValue(ref experience);
                serializer.SerializeValue(ref bodyColor);
                serializer.SerializeValue(ref eyeColor);
                serializer.SerializeValue(ref beard);
                serializer.SerializeValue(ref brows);
                serializer.SerializeValue(ref hair);
                //serializer.SerializeValue(ref dateCreated);
                serializer.SerializeNetworkSerializable(ref attributes);
                serializer.SerializeValue(ref userId);
                serializer.SerializeValue(ref level);
            }
        }

        public struct CharacterAttributes : INetworkSerializable
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref strength);
                serializer.SerializeValue(ref vitality);
                serializer.SerializeValue(ref agility);
                serializer.SerializeValue(ref dexterity);
                serializer.SerializeValue(ref intelligence);
            }
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

        private struct CharacterPutPayload
        {
            public string id;
            public int slot;
            public string eyeColor;
            public string hair;
            public string bodyColor;
            public string beard;
            public string brows;
            public string name;
            public string model;

            public CharacterPutPayload(string id, int slot, string eyeColor, string hair, string bodyColor, string beard, string brows, string name, string model)
            {
                this.id = id;
                this.slot = slot;
                this.eyeColor = eyeColor;
                this.hair = hair;
                this.bodyColor = bodyColor;
                this.beard = beard;
                this.brows = brows;
                this.name = name;
                this.model = model;
            }
        }
    }
}