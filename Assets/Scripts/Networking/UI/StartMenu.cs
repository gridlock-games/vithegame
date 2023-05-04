using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightPat.Core;
using Unity.Netcode;
using TMPro;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Rendering;

namespace LightPat.UI
{
    public class StartMenu : Menu
    {
        public GameObject settingsMenu;
        public TMP_InputField playerNameInput;
        public TMP_InputField clientIPAddressInput;
        public TMP_Dropdown serverIPAddressDropdown;

        public void OpenSettingsMenu()
        {
            GameObject _settings = Instantiate(settingsMenu);
            _settings.GetComponent<DisplaySettingsMenu>().SetLastMenu(gameObject);

            gameObject.SetActive(false);
        }

        public void StartClient()
        {
            string targetIP = clientIPAddressInput.text;
            if (targetIP == "Pat's House" | targetIP == "")
            {
                targetIP = "65.25.100.229"; // Pat's IP
            }

            if (playerNameInput.text == "")
            {
                Debug.LogError("Please select a player name before starting");
                return;
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(playerNameInput.text);

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Started Client, looking for address: " + clientIPAddressInput.text);
            }
        }

        public void StartServer()
        {
            string targetIP = "NO IP";
            string IPType = serverIPAddressDropdown.options[serverIPAddressDropdown.value].text;
            if (IPType == "Localhost")
            {
                targetIP = "127.0.0.1";
            }
            else if (IPType == "Local IP")
            {
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        targetIP = ip.ToString();
                        break;
                    }
                }
            }
            else if (IPType == "Public IP")
            {
                targetIP = IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim()).ToString();
            }

            if (targetIP == "NO IP")
            {
                Debug.LogError("Invalid IP Address " + IPType);
                return;
            }

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("Started Server at " + targetIP + ". Make sure you opened port 7777 for UDP traffic!");
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
        }

        private void StartServer(string targetIP)
        {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("Started Server at " + targetIP + ". Make sure you opened port 7777 for UDP traffic!");
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
        }

        public void StartHost()
        {
            if (playerNameInput.text == "")
            {
                Debug.LogError("Please select a player name before starting");
                return;
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(playerNameInput.text);

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = "127.0.0.1";
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Hosting local game");
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
        }

        private void Update()
        {
            // If we are a headless build
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                StartServer(IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim()).ToString());
            }
        }
    }
}
