using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
namespace Vi.PlatformAPI
{
  public class DiscordManager : MonoBehaviour
  {

    public Discord.Discord discord;

    // Use this for initialization
    void Start()
    {
      discord = new Discord.Discord(1181075969558183996, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
      var activityManager = discord.GetActivityManager();
      var activity = new Discord.Activity
      {
        State = "Login Menu",
        Details = "Logging in to Vi"
      };
      activityManager.UpdateActivity(activity, (res) =>
      {
        if (res == Discord.Result.Ok)
        {
          Debug.Log("Successful discord access");
        }
      });
    }

    // Update is called once per frame
    void Update()
    {
      discord.RunCallbacks();
    }
  }
}