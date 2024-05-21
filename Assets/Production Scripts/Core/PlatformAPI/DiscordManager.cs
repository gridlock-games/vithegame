using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
namespace jomarcentermjm.PlatformAPI
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
        Details = "Logging in to Vi",
        Assets = {
          LargeImage = "vi_game_logo",
          SmallImage = "vi_game_logo"
        }
      };
      activityManager.UpdateActivity(activity, (res) =>
      {
        if (res == Discord.Result.Ok)
        {
          Debug.Log("Successful activity change");
        }
      });
    }

    public void ChangeActivityMessage(string state, string details, string largeimg, string largetext, string smallimage, string smalltext)
    {
      var activityManager = discord.GetActivityManager();
      var activity = new Discord.Activity
      {
        State = "Login Menu",
        Details = "Logging in to Vi",
        Assets = {
        LargeImage = largeimg,
        SmallImage = smallimage,
        SmallText = smalltext,
        LargeText = largetext
        }
      };
      activityManager.UpdateActivity(activity, (res) =>
      {
        if (res == Discord.Result.Ok)
        {
          Debug.Log("Successful activity change");
        }
      });
    }

    public void clearActivity()
    {
      var activityManager = discord.GetActivityManager();
      activityManager.ClearActivity((result) =>
      {
        if (result == Discord.Result.Ok)
        {
          Debug.Log("End");
        }
        else
        {
          Debug.Log("Failed");
        }
      });
    }
    // Update is called once per frame
    void LateUpdate()
    {
      discord.RunCallbacks();
    }
  }
}