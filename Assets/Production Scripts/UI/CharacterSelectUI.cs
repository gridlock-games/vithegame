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
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private Transform characterSelectGridParent;
        [SerializeField] private GameObject characterSelectParent;
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private GameObject serverListParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterRoleText;
        [SerializeField] private Vector3 previewCharacterPosition = new Vector3(0.6f, 0, -7);
        [SerializeField] private Vector3 previewCharacterRotation = new Vector3(0, 180, 0);
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button refreshServersButton;

        private readonly float size = 200;
        private readonly int height = 2;

        private void Awake()
        {
            CloseServerBrowser();
            CharacterReference.PlayerModelOption[] playerModelOptions = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            Quaternion rotation = Quaternion.Euler(0, 0, -45);
            int characterIndex = 0;
            for (int x = 0; x < playerModelOptions.Length; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (characterIndex >= playerModelOptions.Length) { return; }

                    Vector3 pos = new Vector3(x * size - size, y * size, 0);
                    GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectGridParent);
                    g.transform.localPosition = rotation * pos;
                    g.GetComponent<CharacterSelectElement>().Initialize(this, playerModelOptions[characterIndex].characterImage, characterIndex, 0);
                    characterIndex++;
                }
            }
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
            characterNameText.text = playerModelOption.name;
            characterRoleText.text = playerModelOption.role;
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