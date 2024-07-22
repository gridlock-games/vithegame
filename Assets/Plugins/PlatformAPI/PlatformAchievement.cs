using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX

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
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        //Steam Code
        if (SteamManager.Initialized && gameObject.GetComponent<SteamManager>() != null)
        {
          //Call in the achievement as completed
          Steamworks.SteamUserStats.GetAchievement(achivement.steamAchievementID, out bool isCompleted);
          if (!isCompleted)
          {
            SteamUserStats.SetAchievement(achivement.steamAchievementID);
            SteamUserStats.StoreStats();
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
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        //Steam
        if (SteamManager.Initialized)
        {
          SteamUserStats.SetStat(stats.steamStatisticsID, value);
          SteamUserStats.StoreStats();
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
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        //Steam
        if (SteamManager.Initialized && platform == GamePlatform.Steam)
        {
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