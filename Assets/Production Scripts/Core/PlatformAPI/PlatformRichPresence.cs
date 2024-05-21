using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace jomarcentermjm.PlatformAPI
{
  public class PlatformRichPresence : MonoBehaviour
  {
    [Header("rich presence image")]
    [Tooltip("(DISCORD) required to upload the image in advance on discord developer site")]
    [SerializeField] string defaultMainImageID = "vi_game_logo";
    [Tooltip("(DISCORD) required to upload the image in advance on discord developer site")]
    [SerializeField] string defaultSubImageID = "vi_game_logo";

    //Note mainImageID and subImageID is being used by discord only.
    public void UpdatePlatformStatus(string title, string description = "", string linethree = "", string richpresenceKey = "#StatusGeneral", string mainImageID = null, string subImageID = null)
    {
      //Steam
      if (SteamManager.Initialized)
      {
        string RecreatedValue = description + linethree;
        SteamFriends.SetRichPresence(richpresenceKey, RecreatedValue);
      }
      //Discord

    }

    public void ClearPlatformStatus()
    {

    }
  }
}