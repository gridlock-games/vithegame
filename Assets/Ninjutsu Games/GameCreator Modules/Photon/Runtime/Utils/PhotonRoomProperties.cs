using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace NJG.PUN
{
    public static class PhotonRoomProperties
    {
        public const string TIME_START = "StartTime";
        public const string TIME_DURATION = "DurationTime";

        /// <summary>
        /// Returns room Start Time.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static double GetElapsedTime(this Room room)
        {
            return room.HasProperty(TIME_START) ? PhotonNetwork.Time - room.GetStartTime() : 0;
        }

        /// <summary>
        /// Returns room Start Time.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static double GetRemainingTime(this Room room)
        {
            float secsPerRound = room.HasProperty(TIME_DURATION) ? room.GetDurationTime() : 0;
            return room.HasProperty(TIME_DURATION) ? secsPerRound - (room.GetElapsedTime() % secsPerRound) : room.GetElapsedTime();
        }

        /// <summary>
        /// Returns room Start Time.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static double GetStartTime(this Room room)
        {
            return room.HasProperty(TIME_START) ? room.GetProperty<double>(TIME_START) : -1;
        }

        /// <summary>
        /// Returns room Duration Time.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static int GetDurationTime(this Room room)
        {
            return room.HasProperty(TIME_START) ? room.GetProperty<int>(TIME_DURATION) : -1;
        }

        /// <summary>
        /// Defines the Start Time Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="time"></param>
        public static void SetStartTime(this Room room, double time)
        {
            room.SetProperty<double>(TIME_START, time, false);
        }

        /// <summary>
        /// Defines the duration time of this Room (in seconds).
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="time"></param>
        public static void SetDurationTime(this Room room, int timeInSeconds)
        {
            room.SetProperty<int>(TIME_DURATION, timeInSeconds, false);
        }

        /// <summary>
        /// Returns a Room property of type Bool.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static bool GetBool(this Room room, string propertyName, bool fallback = false)
        {
            return room.HasProperty(propertyName) ? room.GetProperty<bool>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Room property of type string.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static string GetString(this Room room, string propertyName, string fallback = "")
        {
            return room.HasProperty(propertyName) ? room.GetProperty<string>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Room property of type float.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static float GetFloat(this Room room, string propertyName, float fallback = 0)
        {
            return room.HasProperty(propertyName) ? room.GetProperty<float>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Room property of type double.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static double GetDouble(this Room room, string propertyName, double fallback = 0)
        {
            return room.HasProperty(propertyName) ? room.GetProperty<double>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Room property of type int.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static int GetInt(this Room room, string propertyName, int fallback = 0)
        {
            return room.HasProperty(propertyName) ? room.GetProperty<int>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Room property.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static T GetProperty<T>(this Room room, string propertyName)
        {
            object prop;
            //Debug.Log("Player GetProperty " + room.ToStringFull());
            if (room.CustomProperties.TryGetValue(propertyName, out prop))
            {
                return (T)Convert.ChangeType(prop, typeof(T));
            }

            return (T)Convert.ChangeType(prop, typeof(T));
        }

        /// <summary>
        /// Returns a Room property.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static object GetProperty(this Room room, string propertyName)
        {
            object prop;
            if (room.CustomProperties.TryGetValue(propertyName, out prop))
            {
                return prop;
            }

            return prop;
        }

        /// <summary>
        /// Returns true if the give property is found
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasProperty(this Room room, string propertyName)
        {
            return room == null ? false : (room.CustomProperties == null ? false : room.CustomProperties.ContainsKey(propertyName));
        }

        /// <summary>
        /// Incremements an int property from the Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void AddInt(this Room room, string propertyName, int propertyValue, bool webFoward)
        {
            if (!room.HasProperty(propertyName))
            {
                room.SetProperty(propertyName, propertyValue, webFoward);
            }
            else
            {
                room.SetProperty(propertyName, propertyValue + room.GetProperty<int>(propertyName), webFoward);
            }
        }

        /// <summary>
        /// Incremements an int property from the Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void AddFloat(this Room room, string propertyName, float propertyValue, bool webFoward)
        {
            if (!room.HasProperty(propertyName))
            {
                room.SetProperty(propertyName, propertyValue, webFoward);
            }
            else
            {
                room.SetProperty(propertyName, propertyValue + room.GetProperty<float>(propertyName), webFoward);
            }
        }

        /// <summary>
        /// Clear all properties from this Room.
        /// </summary>
        /// <param name="player"></param>
        public static void ClearAllProperties(this Room room)
        {
            ExitGames.Client.Photon.Hashtable t = room.CustomProperties;
            List<object> keys = new List<object>(t.Keys);
            for (int i = 0, imax = keys.Count; i < imax; i++)
            {
                t[keys[i]] = null;
            }
            room.SetCustomProperties(t);
        }

        /// <summary>
        /// Defines a string property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetString(this Room room, string propertyName, string propertyValue, bool webFoward)
        {
            room.SetProperty<string>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a int property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetInt(this Room room, string propertyName, int propertyValue, bool webFoward)
        {
            room.SetProperty<int>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a float property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetFloat(this Room room, string propertyName, float propertyValue, bool webFoward)
        {
            room.SetProperty<float>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a double property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetDouble(this Room room, string propertyName, double propertyValue, bool webFoward)
        {
            room.SetProperty<double>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a bool property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetBool(this Room room, string propertyName, bool propertyValue, bool webFoward)
        {
            room.SetProperty<bool>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a property on this Room.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetProperty<T>(this Room room, string propertyName, T propertyValue, bool webFoward)
        {
            Hashtable startTimeProp = new Hashtable();  // only use ExitGames.Client.Photon.Hashtable for Photon
            startTimeProp[propertyName] = propertyValue;
            PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);
            //room.SetCustomProperties(new Hashtable() { { propertyName, propertyValue } }, null, webFoward);
        }
    }

}