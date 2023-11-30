using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Vi.UI
{
    public class ServerListElement : MonoBehaviour
    {
        [SerializeField] private Text serverNameText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text pingText;

        public WebRequestManager.Server Server { get; private set; }
        public float pingTime { get; private set; } = -1;

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
            networkTransport.ConnectionData.Address = Server.ip;
            networkTransport.ConnectionData.Port = ushort.Parse(Server.port);
        }

        public void Initialize(CharacterSelectUI characterSelectUI, WebRequestManager.Server server)
        {
            Server = server;
            serverNameText.text = server.label;
            playerCountText.text = server.population.ToString();
            regionText.text = "NA";
            pingText.text = "";
            characterSelectUI.StartCoroutine(PingServer());
        }

        private IEnumerator PingServer()
        {
            Ping ping = new Ping(Server.ip);
            yield return new WaitUntil(() => ping.isDone);
            if (!pingText) { yield break; }
            pingText.text = ping.time.ToString();
            pingTime = ping.time;
        }

        private void Update()
        {
            // If network manager connection info matches this button
            button.interactable = networkTransport.ConnectionData.Address != Server.ip & networkTransport.ConnectionData.Port != ushort.Parse(Server.port);
        }
    }
}