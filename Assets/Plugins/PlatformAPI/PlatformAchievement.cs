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

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
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
#endif


    }

    public void UpdateStats(string ID, int value)
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
      //Steam
      if (SteamManager.Initialized)
      {
        SteamUserStats.SetStat(ID, value);
      }
#endif
    }

    public int GetStats(GamePlatform platform , string ID)
    {
      int output = 0;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
      //Steam
      if (SteamManager.Initialized && platform == GamePlatform.Steam)
      {
        SteamUserStats.GetStat(ID, out output);
      }
#endif


      return output;
    }


  }
}