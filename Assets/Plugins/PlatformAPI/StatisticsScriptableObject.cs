using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace jomarcentermjm.PlatformAPI
{
  [CreateAssetMenu(fileName = "Statistics", menuName = "PlatformAPI/Statistics", order = 2)]
  public class StatisticsScriptableObject : ScriptableObject
  {
    public string StatisticsID;
    public string StatisticsName;
    public string StatisticsDescription;

    [Header("steam")]
    public string steamStatisticsID;

    [Header("Google/PlayStore/Playgame")]
    public string playgameStatisticsID;
  }
}