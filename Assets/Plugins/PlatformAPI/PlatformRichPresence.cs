using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
using Steamworks;
//using Discord;
#endif

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

        public static PlatformRichPresence instance;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void UpdatePlatformStatus(string title, string description = "", string linethree = "", string richpresenceKey = "#StatusGeneral", string mainImageID = null, string mainImageDesc = "", string subImageID = null, string subImageDesc = "")
        {
#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
            //Debug.Log("Updating Platform Status");
            //Steam

            if (gameObject.GetComponent<SteamManager>() != null)
            {
                //Debug.Log("Successful reporting on steam");
                string RecreatedValue = description + linethree;
                SteamFriends.SetRichPresence("steam_display", richpresenceKey);
                SteamFriends.SetRichPresence("status_message", RecreatedValue);
            }

            //Null check
            if (mainImageID == null) mainImageID = defaultMainImageID;
            if (subImageID == null) subImageID = defaultSubImageID;

            //Discord
            //var discordManager = gameObject.GetComponent<DiscordManager>();
            //if (discordManager != null)
            //{
            //    //Debug.Log("Successful reporting on discord");
            //    discordManager.ChangeActivityMessage(title, description, mainImageID, mainImageDesc, subImageID, subImageDesc);
            //}
#endif
        }

        public void ClearPlatformStatus()
        {

        }

    }
}