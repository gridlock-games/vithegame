using jomarcentermjm.PlatformAPI;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
  public class RichPresenceGameMode : MonoBehaviour
  {

    protected GameModeManager gameModeManager;
    // Start is called before the first frame update
    void Start()
    {
      gameModeManager = FindFirstObjectByType<GameModeManager>();
      gameModeManager.SubscribeScoreListCallback(delegate { OnListChange(); });


    }

    private void OnDestroy()
    {
      gameModeManager.UnsubscribeScoreListCallback(delegate { OnListChange(); });
    }

    protected void OnListChange()
    {
      HandlePlatformAPI(gameModeManager.GetLeftScoreString(), gameModeManager.GetRightScoreString(), gameModeManager.GetRoundCount().ToString());
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
        PlatformRichPresence.instance.UpdatePlatformStatus("in-game", $"{StageName} - {GameModeName}", $"Round {RoundNumber} - {LScore} : {RScore}");
      }
    }
  }
}
