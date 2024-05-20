using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.PlatformAPI
{
  [CreateAssetMenu(fileName = "Achievements", menuName = "PlatformAPI/Achievement", order = 1)]
  public class AchivementScriptableObject : ScriptableObject
  {
    public string achievementID;
    public string achievementName;
    public string achievementDescription;

    [Header("steam")]
    public string steamAchievementID;

    [Header("Google/PlayStore/Playgame")]
    public string playgameAchievementID;
  }
}