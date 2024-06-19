using jomarcentermjm.PlatformAPI;
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
    PlayerDataManager.GameMode gameModeID;
    List<string> gameModeNameList = new List<string>() {
            "None",
            "Free For All",
            "Team Elimination",
            "Essence War",
            "Outpost Rush",
            "Team Deathmatch"
    };

    List<string> gameModeDiscordInfoIDList = new List<string>() {
            "None",
            "freeforall",
            "teamelim",
            "EssenceWar",
            "OutpostRush",
            "TeamDeathmatch"
    };

    string gameModeName;
    string mapName;
    string gameModeDiscordInfoID;
    // Start is called before the first frame update
    void Start()
    {
      gameModeManager = FindFirstObjectByType<GameModeManager>();
      gameModeID = PlayerDataManager.Singleton.GetGameMode();
      gameModeName = gameModeNameList[(int)gameModeID];
      gameModeDiscordInfoID = gameModeDiscordInfoIDList[(int)gameModeID];
      mapName = PlayerDataManager.Singleton.GetMapName();

      gameModeManager.SubscribeScoreListCallback(delegate { OnListChange(); });
      

    }

    private void OnDestroy()
    {
      gameModeManager.UnsubscribeScoreListCallback(delegate { OnListChange(); });
    }

    protected void OnListChange()
    {
      HandlePlatformAPI(gameModeManager.GetLeftScoreString(), gameModeManager.GetRightScoreString(), gameModeManager.GetRoundCount().ToString(), mapName, gameModeName);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void HandlePlatformAPI(string LScore, string RScore, string RoundNumber, string StageName = "Main Level", string GameModeName = "Mode Name")
    {

      //Rich presence
      if (PlatformRichPresence.instance != null)
      {
        //Change logic here that would handle scenario where the player is host.
        PlatformRichPresence.instance.UpdatePlatformStatus($"Round {RoundNumber} - {LScore} : {RScore}", $"{StageName} - {GameModeName} ", $"Round {RoundNumber} - {LScore} : {RScore}","#StatusGeneral", gameModeDiscordInfoID, gameModeName);
      }
    }
  }
}
