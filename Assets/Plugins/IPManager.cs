using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;


namespace UnityEngine
{

    public class IPManager : MonoBehaviour
    {
        public string ServerAPIURL = "38.60.245.223/servers/duels";
        // public string ServerAPIURL = "192.168.0.106/servers/duels";
        public string VMServerHost = GetIP(ADDRESSFAM.IPv4);

        public IEnumerator CheckAPI()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(ServerAPIURL))
            {
                yield return www.SendWebRequest();

                string FailOverServerAPIURL = "http://" + GetIP(ADDRESSFAM.IPv4) + ":3000/servers/duels";
                string FailOverVMServerHost = GetIP(ADDRESSFAM.IPv4);

                // Please do not remove
                //string FailOverServerAPIURL = "http://" + "192.168.100.78" + ":3000/servers/duels";
                //string FailOverVMServerHost = "192.168.100.78";

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("API request error: " + www.error);
                    Debug.LogWarning("API FailOverServerAPIURL: " + FailOverServerAPIURL);
                    this.ServerAPIURL = FailOverServerAPIURL;
                    this.VMServerHost = FailOverVMServerHost;
                }
                else
                {
                    if (www.responseCode == 200)
                    {
                        Debug.Log("API request successful (Status 200).");
                        this.ServerAPIURL = "38.60.245.223/servers/duels";
                        this.VMServerHost = GetIP(ADDRESSFAM.IPv4);
                        // this.VMServerHost = IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim()).ToString();
                        // Do nothing
                    }
                    else
                    {
                        Debug.LogWarning("API request returned a non-200 status code: " + www.responseCode);
                        Debug.LogWarning("API: " + FailOverServerAPIURL);
                        this.ServerAPIURL = FailOverServerAPIURL;
                        this.VMServerHost = FailOverVMServerHost;
                    }
                }
            }
        }
        

        private string GetSubNet(string IPAddress) {
           return IPAddress.Substring(0, IPAddress.LastIndexOf('.') + 1);
        }

        public static string GetIP(ADDRESSFAM Addfam)
        {
            if (Application.isEditor)
            {
                return "127.0.0.1";
            }

            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "";

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {

                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (Addfam == ADDRESSFAM.IPv4)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (Addfam == ADDRESSFAM.IPv6)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return output;
        }
    }

    public enum ADDRESSFAM
    {
        IPv4, IPv6
    }
}