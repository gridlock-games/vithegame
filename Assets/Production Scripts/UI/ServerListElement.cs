using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Vi.Utility;

namespace Vi.UI
{
    public class ServerListElement : MonoBehaviour
    {
        [SerializeField] private Text serverNameText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text pingText;

        public ServerManager.Server Server { get; private set; }
        public float pingTime { get; private set; } = Mathf.Infinity;

        private Button button;
        private UnityTransport networkTransport;
        private void Awake()
        {
            button = GetComponent<Button>();
            gameObject.SetActive(false);
            networkTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        public void SetConnectionInfo()
        {
            networkTransport.SetConnectionData(Server.ip, ushort.Parse(Server.port), FasterPlayerPrefs.serverListenAddress);
        }

        public void Initialize(MonoBehaviour UIParent, ServerManager.Server server)
        {
            Server = server;
            serverNameText.text = server.label;
            playerCountText.text = server.population.ToString();
            regionText.text = "MNL";
            pingText.text = "N/A";
            UIParent.StartCoroutine(PingServer());
        }

        private IEnumerator PingServer()
        {
            Ping ping = new Ping(Server.ip);
            float startTime = Time.time;

            while (true)
            {
                if (ping.isDone)
                {
                    pingTime = ping.time;
                    pingText.text = pingTime.ToString();
                    break;
                }

                if (Time.time - startTime > 2)
                {
                    pingTime = 2;
                    pingText.text = "N/A";
                    break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            // If network manager connection info matches this button
            button.interactable = networkTransport.ConnectionData.Address != Server.ip | networkTransport.ConnectionData.Port != ushort.Parse(Server.port);
        }
    }
}