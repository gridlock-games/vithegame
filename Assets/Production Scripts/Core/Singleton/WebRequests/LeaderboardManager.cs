using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace Vi.Core
{
    public class LeaderboardManager : MonoBehaviour
    {
        public IEnumerator SendHordeModeLeaderboardResult(string charId, string playerName, PlayerDataManager.GameMode gameMode, float clearTime, int wave, float damageDealt)
        {
            HordeModeLeaderboardResultPayload payload = new HordeModeLeaderboardResultPayload(charId, playerName, gameMode, clearTime, wave, damageDealt);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.SendHordeModeLeaderboardResult()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct HordeModeRecord
        {
            public string playerName;
            public string gameMode;
            public int wave;
            public float clearTime;
            public float damageDealt;
        }

        private struct HordeModeLeaderboardResultPayload
        {
            public string charId;
            public HordeModeRecord record;
            public string boardType;

            public HordeModeLeaderboardResultPayload(string charId, string playerName, PlayerDataManager.GameMode gameMode, float clearTime, int wave, float damageDealt)
            {
                this.charId = charId;
                record = new HordeModeRecord()
                {
                    playerName = playerName,
                    gameMode = PlayerDataManager.GetGameModeString(gameMode),
                    wave = wave,
                    clearTime = clearTime,
                    damageDealt = damageDealt
                };
                boardType = "horde";
            }
        }

        public IEnumerator SendKillsLeaderboardResult(string charId, string playerName, PlayerDataManager.GameMode gameMode, int kills, int deaths, int assists)
        {
            KillsLeaderboardResultPayload payload = new KillsLeaderboardResultPayload(charId, playerName, gameMode, kills, deaths, assists);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/postLeaderBoard", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.SendKillsLeaderboardResult()" + postRequest.error);
            }
            postRequest.Dispose();
        }

        public struct KillsRecord
        {
            public string playerName;
            public string gameMode;
            public int kills;
            public int deaths;
            public int assists;
            public float KDA;
        }

        private struct KillsLeaderboardResultPayload
        {
            public string charId;
            public KillsRecord record;
            public string boardType;

            public KillsLeaderboardResultPayload(string charId, string playerName, PlayerDataManager.GameMode gameMode, int kills, int deaths, int assists)
            {
                this.charId = charId;
                record = new KillsRecord()
                {
                    playerName = playerName,
                    gameMode = PlayerDataManager.GetGameModeString(gameMode),
                    kills = kills,
                    deaths = deaths,
                    assists = assists,
                    KDA = deaths == 0 ? kills + assists : (kills + assists) / (float)deaths
                };
                boardType = "kills";
            }
        }

        public List<KillsLeaderboardEntry> killsLeaderboardEntries { get; private set; } = new List<KillsLeaderboardEntry>();
        public List<HordeLeaderboardEntry> hordeLeaderboardEntries { get; private set; } = new List<HordeLeaderboardEntry>();

        public IEnumerator GetLeaderboard()
        {
            // Kills leaderboard
            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/getLeaderBoardSummary/kills");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetLeaderboard()");
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;

            killsLeaderboardEntries = JsonConvert.DeserializeObject<List<KillsLeaderboardEntry>>(json);

            getRequest.Dispose();

            // Horde mode leaderboard
            getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false)+ "characters/getLeaderboard/horde");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetLeaderboard()");
                getRequest.Dispose();
                yield break;
            }
            json = getRequest.downloadHandler.text;

            hordeLeaderboardEntries = JsonConvert.DeserializeObject<List<HordeLeaderboardEntry>>(json);

            getRequest.Dispose();
        }

        public struct KillsLeaderboardEntry
        {
            public string boardType;
            public string charId;
            public KillsRecord record;
        }

        public struct HordeLeaderboardEntry
        {
            public string boardType;
            public string charId;
            public HordeModeRecord record;
            public string dateCreated;
        }
    }
}