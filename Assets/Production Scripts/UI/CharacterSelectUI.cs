using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using System.Linq;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private InputField usernameInputField;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private GameObject characterSelectParent;
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private GameObject serverListParent;
        [SerializeField] private Vector3 previewCharacterPosition = new Vector3(0.6f, 0, -7);
        [SerializeField] private Vector3 previewCharacterRotation = new Vector3(0, 180, 0);
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button refreshServersButton;

        private void Awake()
        {
            CloseServerBrowser();
            createCharacterButton.interactable = usernameInputField.text.Length > 0;
        }

        private void Start()
        {
            UpdateCharacterPreview(0, 0);
            StartCoroutine(WebRequestManager.GetRequest());
        }

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private void Update()
        {
            if (!WebRequestManager.IsRefreshingServers)
            {
                foreach (WebRequestManager.Server server in WebRequestManager.Servers)
                {
                    if (!serverListElementList.Find(item => item.Server._id == server._id))
                    {
                        ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                        serverListElementInstance.Initialize(this, server);
                        serverListElementList.Add(serverListElementInstance);
                    }
                }
            }

            serverListElementList = serverListElementList.OrderBy(item => item.pingTime).ToList();
            for (int i = 0; i < serverListElementList.Count; i++)
            {
                serverListElementList[i].gameObject.SetActive(serverListElementList[i].pingTime >= 0);
                serverListElementList[i].transform.SetSiblingIndex(i);
            }
        }

        public void OnUsernameChange()
        {
            createCharacterButton.interactable = usernameInputField.text.Length > 0;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(usernameInputField.text + "|0|0");
        }

        public void OpenServerBrowser()
        {
            characterSelectParent.SetActive(false);
            serverListParent.SetActive(true);
            RefreshServerBrowser();
        }

        public void CloseServerBrowser()
        {
            characterSelectParent.SetActive(true);
            serverListParent.SetActive(false);
        }

        public void RefreshServerBrowser()
        {
            StartCoroutine(WebRequestManager.GetRequest());
            foreach (ServerListElement serverListElement in serverListElementList)
            {
                Destroy(serverListElement.gameObject);
            }
            serverListElementList.Clear();
        }

        public void StartClient()
        {
            connectButton.interactable = false;
            closeServersMenuButton.interactable = false;
            refreshServersButton.interactable = false;
            NetworkManager.Singleton.StartClient();
        }

        private GameObject previewObject;
        public void UpdateCharacterPreview(int characterIndex, int skinIndex)
        {
            if (previewObject) { Destroy(previewObject); }

            CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            previewObject = Instantiate(playerModelOption.playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
        }

        public void ChangeSkin()
        {
            PlayerDataManager.ParsedConnectionData parsedConnectionData = PlayerDataManager.ParseConnectionData(NetworkManager.Singleton.NetworkConfig.ConnectionData);

            parsedConnectionData.skinIndex += 1;
            if (parsedConnectionData.skinIndex > PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[parsedConnectionData.characterIndex].skinOptions.Length - 1) { parsedConnectionData.skinIndex = 0; }

            PlayerDataManager.SetConnectionData(parsedConnectionData);

            UpdateCharacterPreview(parsedConnectionData.characterIndex, parsedConnectionData.skinIndex);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(previewCharacterPosition, 0.5f);
        }
    }
}