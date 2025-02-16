// using jomarcentermjm.PlatformAPI;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class RichPresenceGameMode : MonoBehaviour
    {
        protected GameModeManager gameModeManager;
        private PlayerDataManager.GameMode gameModeID;

        private List<string> gameModeNameList = new List<string>() {
            "None",
            "Free For All",
            "Team Elimination",
            "Essence War",
            "Outpost Rush",
            "Team Deathmatch"
    };

        private List<string> gameModeDiscordInfoIDList = new List<string>() {
            "None",
            "freeforall",
            "teamelim",
            "EssenceWar",
            "OutpostRush",
            "TeamDeathmatch"
    };

        private string gameModeName;
        private string mapName;
        private string gameModeDiscordInfoID;

        // Start is called before the first frame update
        private void Start()
        {
            gameModeManager = FindFirstObjectByType<GameModeManager>();
            gameModeID = PlayerDataManager.Singleton.GetGameMode();
            gameModeName = gameModeNameList[(int)gameModeID];
            gameModeDiscordInfoID = gameModeDiscordInfoIDList[(int)gameModeID];
            mapName = PlayerDataManager.Singleton.GetMapName();

            if (gameModeID != PlayerDataManager.GameMode.None)
            {
                gameModeManager.onScoreListChanged += OnListChange;
            }
        }

        private void OnDestroy()
        {
            gameModeManager.onScoreListChanged -= OnListChange;
        }

        protected void OnListChange()
        {
            HandlePlatformAPI(gameModeManager.GetLeftScoreString(), gameModeManager.GetRightScoreString(), gameModeManager.GetRoundCount().ToString(), mapName, gameModeName);
        }

        public void HandlePlatformAPI(string LScore, string RScore, string RoundNumber, string StageName = "Main Level", string GameModeName = "Mode Name")
        {
            //Rich presence
            // if (PlatformRichPresence.instance != null)
            // {
            //     //Change logic here that would handle scenario where the player is host.
            //     PlatformRichPresence.instance.UpdatePlatformStatus($"Round {RoundNumber} - {LScore} : {RScore}", $"{StageName} - {GameModeName} ", $"Round {RoundNumber} - {LScore} : {RScore}", "#StatusGeneral", gameModeDiscordInfoID, gameModeName);
            // }
        }

    }
}