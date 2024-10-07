using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using System;

namespace Vi.Core
{
	public class DiscordManager : MonoBehaviour
    {
        public static void UpdateActivity(string state, string details)
        {
            if (!canRun) { return; }

            // Create a new activity
            // SmallText and LargeText are hover text elements for the images you pass in
            Activity activity = new Activity
            {
                State = state,
                Details = details,
                Assets = {
                    LargeImage = "app-logo",
                    LargeText = details,
                    //SmallImage = "app-logo",
                    //SmallText = details
                },
                Timestamps =
                {
                    Start = startTime
                },
                Instance = true
            };

            ActivityManager activityManager = discord.GetActivityManager();
            activityManager.UpdateActivity(activity, (res) =>
            {
                if (res != Result.Ok)
                {
                    Debug.LogError("Discord error while updating activity " + res);
                }
            });
        }

        private static void ClearActivity()
        {
            if (!canRun) { return; }

            ActivityManager activityManager = discord.GetActivityManager();
            // Clear the current activity
            activityManager.ClearActivity((res) =>
            {
                if (res != Result.Ok)
                {
                    Debug.LogError("Discord error while clearing activity " + res);
                }
            });
        }

        private static Discord.Discord discord;
        private static long startTime;

        private const long CLIENT_ID = 1292647294285643836;

		void Start()
		{
			discord = new Discord.Discord(CLIENT_ID, (ulong)CreateFlags.NoRequireDiscord);
            startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        }

        private static bool canRun = true;
        private void Update()
        {
            if (!canRun) { return; }

            try
            {
                discord.RunCallbacks();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Discord Error - " + e);
                canRun = false;
            }
        }

        private void OnApplicationQuit()
        {
            if (!canRun) { return; }

            ClearActivity();
            discord.RunCallbacks();
        }
    }
}