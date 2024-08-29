//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System;

//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS && !UNITY_ANDROID && !UNITY_IOS

//using Discord;

//#endif

//namespace jomarcentermjm.PlatformAPI
//{
//  public class DiscordManager : MonoBehaviour
//  {
//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
//    public Discord.Discord discord;
//    public ActivityManager activityManager;
//#endif

//    // Use this for initialization
//    private void Start()
//    {
//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
//      discord = new Discord.Discord(1181075969558183996, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
//      activityManager = discord.GetActivityManager();
//      var activity = new Discord.Activity
//      {
//        State = "Login Menu",
//        Details = "Logging in to Vi",
//        Assets = {
//          LargeImage = "vi_game_logo",
//          SmallImage = "vi_game_logo"
//        }
//      };
//      activityManager.UpdateActivity(activity, (res) =>
//      {
//        if (res == Discord.Result.Ok)
//        {
//          //Debug.Log("Successful activity change");
//        }
//      });
//#endif
//    }

//    public void ChangeActivityMessage(string state, string details, string largeimg, string largetext, string smallimage, string smalltext)
//    {
//      clearActivity();
//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
//      var activity = new Discord.Activity
//      {
//        State = state,
//        Details = details,
//        Assets = {
//        LargeImage = largeimg,
//        SmallImage = smallimage,
//        SmallText = smalltext,
//        LargeText = largetext
//        }
//      };
//      activityManager.UpdateActivity(activity, (res) =>
//      {
//        if (res == Discord.Result.Ok)
//        {
//          //Debug.Log("Successful activity change");
//        }
//      });
//#endif
//    }

//    public void clearActivity()
//    {
//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
//      activityManager = discord.GetActivityManager();
//      activityManager.ClearActivity((result) =>
//      {
//        if (result == Discord.Result.Ok)
//        {
//          //Debug.Log("End");
//        }
//        else
//        {
//          //Debug.Log("Failed");
//        }
//      });
//#endif
//    }

//#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS

//    // Update is called once per frame
//    private void LateUpdate()
//    {
//      var callbackResult = discord.RunCallbacks();
//      if (callbackResult != Result.Ok)
//      {
//        Debug.LogError($"Discord API have recieved an error and will be shutdown - error: {callbackResult}");
//        Destroy(this);
//      }

//    }

//#endif
//  }
//}