using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class ServerListElement : MonoBehaviour
    {
        [SerializeField] private Text serverNameText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text pingText;

        public WebRequestManager.Server server { get; private set; }

        //CharacterSelectUI characterSelectUI;
        public void Initialize(CharacterSelectUI characterSelectUI, WebRequestManager.Server server)
        {
            //this.characterSelectUI = characterSelectUI;
            this.server = server;
            serverNameText.text = server.label;
            playerCountText.text = server.population.ToString();
            regionText.text = "NA";
            pingText.text = "";
        }

        private void OnEnable()
        {
            StartCoroutine(PingServer());
        }

        private IEnumerator PingServer()
        {
            Ping ping = new Ping(server.ip);
            yield return new WaitUntil(() => ping.isDone);
            pingText.text = ping.time.ToString();
        }
    }
}