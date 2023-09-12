using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Unity.Netcode;

namespace LightPat.UI
{
    public class PauseMenu : Menu
    {
        public GameObject displaySettingsMenu;
        public GameObject controlSettingsMenu;
        public Slider volumeSlider;
        public Button returnButton;

        public void OpenDisplayMenu()
        {
            if (returnCalled) { return; }
            GameObject _settings = Instantiate(displaySettingsMenu);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void OpenControlMenu()
        {
            if (returnCalled) { return; }
            GameObject _settings = Instantiate(controlSettingsMenu);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
        }

        public void RestartLevel()
        {
            //Application.LoadLevel(Application.loadedLevel);
        }

        private bool returnCalled;
        public void ReturnToCharSelect()
        {
            if (returnCalled) { return; }
            returnCalled = true;

            StartCoroutine(GoToCharSelect());
        }

        private IEnumerator GoToCharSelect()
        {
            Debug.Log("Shutting down NetworkManager");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            Debug.Log("Shutdown complete");

            SceneManager.LoadScene("CharacterSelect");
        }

        public void ReturnToHub()
        {
            if (returnCalled) { return; }
            returnCalled = true;

            StartCoroutine(ConnectToHub());
        }

        private IEnumerator ConnectToHub()
        {
            // Get list of servers in the API
            UnityWebRequest getRequest = UnityWebRequest.Get(ClientManager.Singleton.iPManager.ServerAPIURL);

            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in LobbyMenu.ConnectToHub() " + getRequest.error);
            }

            string json = getRequest.downloadHandler.text;
            ClientManager.Server playerHubServer = new();

            bool playerHubServerFound = false;

            if (json != "[]")
            {
                foreach (string jsonSplit in json.Split("},"))
                {
                    string finalJsonElement = jsonSplit;
                    if (finalJsonElement[0] == '[')
                    {
                        finalJsonElement = finalJsonElement.Remove(0, 1);
                    }

                    if (finalJsonElement[^1] == ']')
                    {
                        finalJsonElement = finalJsonElement.Remove(finalJsonElement.Length - 1, 1);
                    }

                    if (finalJsonElement[^1] != '}')
                    {
                        finalJsonElement += "}";
                    }

                    ClientManager.Server server = JsonUtility.FromJson<ClientManager.Server>(finalJsonElement);

                    if (server.type == 1 & server.ip == NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address)
                    {
                        playerHubServer = server;
                        playerHubServerFound = true;
                        break;
                    }
                }
            }

            if (!playerHubServerFound)
            {
                Debug.LogError("Player Hub Server not found in API. Is there a server with the type set to 1?");
                yield break;
            }

            Debug.Log("Shutting down NetworkManager");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            Debug.Log("Shutdown complete");

            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            networkTransport.ConnectionData.Address = playerHubServer.ip;
            networkTransport.ConnectionData.Port = ushort.Parse(playerHubServer.port);

            Debug.Log("Starting client: " + NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address + " " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            // Change the scene locally, then connect to the target IP
            NetworkManager.Singleton.StartClient();
        }

        private void Start()
        {
            volumeSlider.value = AudioListener.volume;

            if (SceneManager.GetActiveScene().name == "Hub")
            {
                returnButton.GetComponentInChildren<Text>().text = "RETURN TO CHARACTER SELECT";
                returnButton.onClick.AddListener(ReturnToCharSelect);
            }
            else
            {
                returnButton.GetComponentInChildren<Text>().text = "RETURN TO PLAYER HUB";
                returnButton.onClick.AddListener(ReturnToHub);
            }
        }
    }
}