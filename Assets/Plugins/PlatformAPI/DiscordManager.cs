using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Discord;
#endif
namespace jomarcentermjm.PlatformAPI
{
    public class DiscordManager : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        public Discord.Discord discord;
#endif

        // Use this for initialization
        void Start()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
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
#endif
        }

        public void ChangeActivityMessage(string state, string details, string largeimg, string largetext, string smallimage, string smalltext)
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
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
#endif
        }

        public void clearActivity()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
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
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        // Update is called once per frame
        void LateUpdate()
        {
            discord.RunCallbacks();
        }
#endif
    }
}
