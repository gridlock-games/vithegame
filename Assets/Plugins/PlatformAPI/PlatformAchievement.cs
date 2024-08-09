using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_SERVER

using Steamworks;

#endif

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
      if (achivement != null)
      {
#if !UNITY_SERVER
        //Steam Code
        if (gameObject.GetComponent<SteamManager>() != null)
        {
          if (!SteamManager.Initialized)
          {
            //Call in the achievement as completed
            Steamworks.SteamUserStats.GetAchievement(achivement.steamAchievementID, out bool isCompleted);
            if (!isCompleted)
            {
              SteamUserStats.SetAchievement(achivement.steamAchievementID);
              SteamUserStats.StoreStats();
            }
          }
        }
#endif
      }
      else
      {
        Debug.LogError($"Achievement {ID} does not exist. Did you type it in correctly?");
      }
    }

    public void UpdateStats(string ID, int value)
    {
      StatisticsScriptableObject stats = StatisticsList.Find(x => x.StatisticsID == ID);
      if (stats != null)
      {
#if !UNITY_SERVER
        //Steam
        if (gameObject.GetComponent<SteamManager>() != null)
        {
          if (!SteamManager.Initialized)
          {
            SteamUserStats.SetStat(stats.steamStatisticsID, value);
            SteamUserStats.StoreStats();
          }
        }
#endif
      }
      else
      {
        Debug.LogError($"Stats {ID} does not exist. Did you type it in correctly?");
      }
    }

    public int GetStats(GamePlatform platform, string ID)
    {
      StatisticsScriptableObject stats = StatisticsList.Find(x => x.StatisticsID == ID);

      int output = 0;
      if (stats != null)
      {
#if !UNITY_SERVER
        //Steam
        if (platform == GamePlatform.Steam &&gameObject.GetComponent<SteamManager>() != null)
        {
          if (SteamManager.Initialized)
          SteamUserStats.GetStat(stats.steamStatisticsID, out output);
        }
#endif
      }
      else
      {
        Debug.LogError($"Stats {ID} does not exist. Did you type it in correctly?");
      }

      return output;
    }
  }
}