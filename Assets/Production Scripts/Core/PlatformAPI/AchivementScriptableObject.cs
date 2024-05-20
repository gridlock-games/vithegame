using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformAPI
{
  [CreateAssetMenu(fileName = "Achievements", menuName = "PlatformAPI/Achievement", order = 1)]
  public class AchivementScriptableObject : MonoBehaviour
  {
    public string achievementID;
    public string achievementName;
    public string achievementDescription;

    [Header("steam")]
    public string steamAchievementID;

  }
}