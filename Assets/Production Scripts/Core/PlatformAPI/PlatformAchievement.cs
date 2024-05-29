using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;



namespace jomarcentermjm.PlatformAPI
{
  public class PlatformAchievement : MonoBehaviour
  {
    [SerializeField] private List<AchivementScriptableObject> achievementsList;
    [SerializeField] private List<StatisticsScriptableObject> StatisticsList;

    public static PlatformAchievement instance;

    private void Awake()
    {
      if (instance == null)
        instance = this;
      else
        Destroy(gameObject);

      DontDestroyOnLoad(gameObject);
    }

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

    public void UpdateStats(string ID, int value)
    {
      //Steam
      if (SteamManager.Initialized)
      {
        SteamUserStats.SetStat(ID, value);
      }
    }

    public int GetStats(GamePlatform platform , string ID)
    {
      int output = 0;

      //Steam
      if (SteamManager.Initialized && platform == GamePlatform.Steam)
      {
        SteamUserStats.GetStat(ID, out output);
      }



      return output;
    }


  }
}