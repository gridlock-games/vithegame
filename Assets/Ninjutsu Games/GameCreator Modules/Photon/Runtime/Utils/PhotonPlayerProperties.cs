//----------------------------------------------
//   Online RPG - Author: Hjupter Cerrud
// Copyright © 2013 - 2015 Ninjutsu Games LTD.
//----------------------------------------------


using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NJG.PUN
{
    public static class PlayerProperties
    {
        const string D = "Deaths";
        const string K = "Kills";
        const string A = "Assists";
        public const string PING = "Ping";

        /// <summary>
        /// Predefined Kills property.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int Kills(this Player player)
        {
            return player.GetInt(K);
        }

        /// <summary>
        /// Predefined Deaths property.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int Deaths(this Player player)
        {
            return player.GetInt(D);
        }

        /// <summary>
        /// Predefined Assists property.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int Assists(this Player player)
        {
            return player.GetInt(A);
        }

        /// <summary>
        /// Returns Players ping.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetPing(this Player player)
        {
            return player.GetInt(PING);
        }

        /// <summary>
        /// Updates Player ping.
        /// </summary>
        /// <param name="player"></param>
        public static void SetPing(this Player player)
        {
            SetInt(player, PING, PhotonNetwork.GetPing(), false);
        }

        /*public static object GetProperty(this Player player, string name)
        {
            object prop;
            //Debug.Log("Player GetProperty " + player.ToStringFull());
            if (player.customProperties.TryGetValue(name, out prop))
            {
                return prop;
            }

            return null;
        }*/

        /// <summary>
        /// Returns a Player property of type Bool.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static bool GetBool(this Player player, string propertyName, bool fallback = false)
        {
            return player.HasProperty(propertyName) ? player.GetProperty<bool>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Player property of type string.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static string GetString(this Player player, string propertyName, string fallback = "")
        {
            return player.HasProperty(propertyName) ? player.GetProperty<string>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Player property of type float.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static float GetFloat(this Player player, string propertyName, float fallback = 0)
        {
            return player.HasProperty(propertyName) ? player.GetProperty<float>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Player property of type int.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static int GetInt(this Player player, string propertyName, int fallback = 0)
        {
            return player.HasProperty(propertyName) ? player.GetProperty<int>(propertyName) : fallback;
        }

        /// <summary>
        /// Returns a Player property.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static T GetProperty<T>(this Player player, string propertyName)
        {
            object prop;
            //Debug.Log("Player GetProperty " + player.ToStringFull());
            if (player.CustomProperties.TryGetValue(propertyName, out prop))
            {
                return (T)Convert.ChangeType(prop, typeof(T));
            }

            return (T)Convert.ChangeType(prop, typeof(T));
        }

        /// <summary>
        /// Returns a Player property.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static object GetProperty(this Player player, string propertyName)
        {
            object prop;
            if (player.CustomProperties.TryGetValue(propertyName, out prop))
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
        public static bool HasProperty(this Player player, string propertyName)
        {
            return player == null ? false : (player.CustomProperties == null ? false : player.CustomProperties.ContainsKey(propertyName));
        }

        /// <summary>
        /// Incremements an int property from the Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void AddInt(this Player player, string propertyName, int propertyValue, bool webFoward)
        {
            if (!player.HasProperty(propertyName))
            {
                player.SetProperty(propertyName, propertyValue, webFoward);
            }
            else
            {
                player.SetProperty(propertyName, propertyValue + player.GetProperty<int>(propertyName), webFoward);
            }
        }

        /// <summary>
        /// Incremements an int property from the Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void AddFloat(this Player player, string propertyName, float propertyValue, bool webFoward)
        {
            if (!player.HasProperty(propertyName))
            {
                player.SetProperty(propertyName, propertyValue, webFoward);
            }
            else
            {
                player.SetProperty(propertyName, propertyValue + player.GetProperty<float>(propertyName), webFoward);
            }
        }

        /// <summary>
        /// Clear all properties from this Player.
        /// </summary>
        /// <param name="player"></param>
        public static void ClearAllProperties(this Player player)
        {
            ExitGames.Client.Photon.Hashtable t = player.CustomProperties;
            List<object> keys = new List<object>(t.Keys);
            for (int i = 0, imax = keys.Count; i < imax; i++)
            {
                t[keys[i]] = null;
            }
            player.SetCustomProperties(t);
        }

        /// <summary>
        /// Defines a string property on this Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetString(this Player player, string propertyName, string propertyValue, bool webFoward)
        {
            player.SetProperty<string>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a int property on this Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetInt(this Player player, string propertyName, int propertyValue, bool webFoward)
        {
            player.SetProperty<int>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a float property on this Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetFloat(this Player player, string propertyName, float propertyValue, bool webFoward)
        {
            player.SetProperty<float>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a bool property on this Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetBool(this Player player, string propertyName, bool propertyValue, bool webFoward)
        {
            player.SetProperty<bool>(propertyName, propertyValue, webFoward);
        }

        /// <summary>
        /// Defines a property on this Player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetProperty<T>(this Player player, string propertyName, T propertyValue, bool webFoward)
        {
            /*if (!PhotonNetwork.connectedAndReady)
            {
                Debug.LogWarning("JoinTeam was called in state: " + PhotonNetwork.connectionStateDetailed + ". Not connectedAndReady.");
            }*/

            //T currentProp = (T)player.GetProperty(propertyName);
            //if (currentProp != propertyValue)
            //{

            if (webFoward)
            {
                var flags = new WebFlags(0);
                //flags.SendSync = false;
                //flags.SendState = true;
                flags.HttpForward = true;
                player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { propertyName, propertyValue } }, null, flags);
            }
            else
            {
                player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { propertyName, propertyValue } }, null, null);
            }
            
            //Debug.Log("Player SetProperty " + player.ToStringFull() + " / currentProp" + currentProp + " / propertyName" + propertyName + " / propertyValue" + propertyValue);
            //}
        }
    }

}