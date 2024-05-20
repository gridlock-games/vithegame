using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;


namespace Vi.PlatformAPI
{
  public class PlatformAchievement : MonoBehaviour
  {
    [SerializeField] private List<AchivementScriptableObject> achievementsList;
    
    public void GrantAchievement(string ID)
    {
      AchivementScriptableObject achivement = achievementsList.Find(x => x.achievementID == ID);

      //Steam Code
      if (SteamManager.Initialized)
      {
        //Call in the achievement as completed
        Steamworks.SteamUserStats.GetAchievement(achivement.steamAchievementID, out bool isCompleted);
        if (!isCompleted ) {
          SteamUserStats.SetAchievement(achivement.steamAchievementID);
          SteamUserStats.StoreStats();
        }
      }

      
    }

    public void UpdateStats()
    {

    }

  }
}