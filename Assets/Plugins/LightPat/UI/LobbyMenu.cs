using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Networking;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;
        public GameObject startButton;
        public GameObject readyButton;
        public TMP_Dropdown changeTeamDropdown;
        public Button startGameButton;
        public GameObject WaitingToStartText;
        public TMP_Dropdown gameModeDropdown;
        public TMP_Dropdown playerModelDropdown;
        public TextMeshProUGUI errorDisplay;
        [Header("Loadout dropdowns")]
        public TMP_Dropdown primaryWeaponDropdown;
        public TMP_Dropdown secondaryWeaponDropdown;
        public TMP_Dropdown tertiaryWeaponDropdown;

        private GameObject playerModel;
        private Vector3 cameraPositionOffset;

        private bool leaveLobbyInProgress;
        public void LeaveLobby()
        {
            if (leaveLobbyInProgress) { return; }
            leaveLobbyInProgress = true;
            StartCoroutine(ConnectToHub());
        }

        private IEnumerator ConnectToHub()
        {
            // Get list of servers in the API
            UnityWebRequest getRequest = UnityWebRequest.Get(ClientManager.serverEndPointURL);

            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Get Request Error in LobbyMenu.ConnectToHub() " + getRequest.error);
            }

            string json = getRequest.downloadHandler.text;
            ClientManager.Server playerHubServer = new();

            bool playerHubServerFound = false;
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

                if (server.type == 1)
                {
                    playerHubServer = server;
                    playerHubServerFound = true;
                    break;
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

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = playerHubServer.ip;

            Debug.Log("Starting client: " + NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address + " " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            // Change the scene locally, then connect to the target IP
            NetworkManager.Singleton.StartClient();
        }

        public void ToggleReady()
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.ToggleReadyServerRpc(localClientId);
        }

        bool loadingGame;
        public void StartGame()
        {
            if (loadingGame) { return; }
            loadingGame = true;
            Debug.Log("Loading game");
            if (gameModeDropdown.options[gameModeDropdown.value].text == "Duel")
            {
                ClientManager.Singleton.ChangeScene("Duel", true, "OutdoorCastleArena");
            }
            else if (gameModeDropdown.options[gameModeDropdown.value].text == "Deathmatch")
            {
                ClientManager.Singleton.ChangeScene("Duel", true);
            }
        }

        public void UpdateWeaponLoadout()
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.ChangeSpawnWeaponsServerRpc(localClientId, new int[] { primaryWeaponDropdown.value, secondaryWeaponDropdown.value, tertiaryWeaponDropdown.value });
        }

        public void UpdatePlayerDisplay()
        {
            if (playerModel)
                Destroy(playerModel);
            playerModel = Instantiate(ClientManager.Singleton.playerPrefabOptions[playerModelDropdown.value]);
            playerModel.GetComponent<GameCreator.Melee.CharacterMelee>().enabled = false;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.ChangePlayerPrefabOptionServerRpc(localClientId, playerModelDropdown.value);
        }

        public void UpdateGameModeValue()
        {
            System.Enum.TryParse(gameModeDropdown.options[gameModeDropdown.value].text, out GameMode chosenGameMode);
            ClientManager.Singleton.UpdateGameModeServerRpc(chosenGameMode);
        }

        public void ChangeTeam()
        {
            Team team = System.Enum.Parse<Team>(changeTeamDropdown.options[changeTeamDropdown.value].text);
            ClientManager.Singleton.ChangeTeamServerRpc(NetworkManager.Singleton.LocalClientId, team);
        }

        private void ResetTeams()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            foreach (ulong clientId in ClientManager.Singleton.GetClientDataDictionary().Keys)
            {
                ClientManager.Singleton.ChangeTeamOnServer(clientId, System.Enum.Parse<Team>(changeTeamDropdown.options[changeTeamDropdown.value].text));
            }
        }

        private void Start()
        {
            List<TMP_Dropdown.OptionData> playerModelOptions = new List<TMP_Dropdown.OptionData>();
            foreach (GameObject playerPrefab in ClientManager.Singleton.playerPrefabOptions)
            {
                playerModelOptions.Add(new TMP_Dropdown.OptionData(playerPrefab.name));
            }
            playerModelDropdown.ClearOptions();
            playerModelDropdown.AddOptions(playerModelOptions);

            List<TMP_Dropdown.OptionData> gameModes = new List<TMP_Dropdown.OptionData>();
            foreach (GameMode gameMode in System.Enum.GetValues(typeof(GameMode)).Cast<GameMode>())
            {
                gameModes.Add(new TMP_Dropdown.OptionData(gameMode.ToString()));
            }
            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(gameModes);

            List<TMP_Dropdown.OptionData> weapons = new List<TMP_Dropdown.OptionData>();
            //foreach (Weapon weapon in ClientManager.Singleton.weaponPrefabOptions)
            //{
            //    weapons.Add(new TMP_Dropdown.OptionData(weapon.weaponName));
            //}
            primaryWeaponDropdown.ClearOptions();
            primaryWeaponDropdown.AddOptions(weapons);
            secondaryWeaponDropdown.ClearOptions();
            secondaryWeaponDropdown.AddOptions(weapons);
            tertiaryWeaponDropdown.ClearOptions();
            tertiaryWeaponDropdown.AddOptions(weapons);

            cameraPositionOffset = Camera.main.transform.localPosition;

            // Can't check if we are the client here, because the network manager may not be started yet if we are client
            if (!NetworkManager.Singleton.IsServer)
                StartCoroutine(WaitForClientConnection());
        }

        private bool clientIsConnected = false;
        private IEnumerator WaitForClientConnection()
        {
            yield return new WaitUntil(() => ClientManager.Singleton.GetClientDataDictionary().ContainsKey(NetworkManager.Singleton.LocalClientId));
            playerModelDropdown.value = ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).playerPrefabOptionIndex;
            UpdatePlayerDisplay();
            UpdateGameModeValue();
            ChangeTeam();
            primaryWeaponDropdown.value = 0;
            secondaryWeaponDropdown.value = 1;
            tertiaryWeaponDropdown.value = 2;
            clientIsConnected = true;
        }

        private void Update()
        {
            // Only let lobby leader change the game mode
            if (NetworkManager.Singleton.LocalClientId == ClientManager.Singleton.lobbyLeaderId.Value)
                gameModeDropdown.interactable = true;
            else
                gameModeDropdown.interactable = false;

            // Set game mode dropdown
            if (gameModeDropdown.options[gameModeDropdown.value].text != ClientManager.Singleton.gameMode.Value.ToString())
            {
                for (int i = 0; i < gameModeDropdown.options.Count; i++)
                {
                    if (ClientManager.Singleton.gameMode.Value.ToString() == gameModeDropdown.options[i].text)
                    {
                        gameModeDropdown.SetValueWithoutNotify(i);
                        break;
                    }
                }
            }

            // If our game mode is not set to duel, enable teams
            //bool enableTeams = (GameMode)System.Enum.Parse(typeof(GameMode), gameModeDropdown.options[gameModeDropdown.value].text) != GameMode.Duel;
            GameMode currentGameMode = System.Enum.Parse<GameMode>(gameModeDropdown.options[gameModeDropdown.value].text);

            List<TMP_Dropdown.OptionData> teamOptions = new List<TMP_Dropdown.OptionData>();
            if (currentGameMode == GameMode.Duel)
            {
                foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
                {
                    if (team == Team.Spectator | team == Team.Competitor)
                    {
                        teamOptions.Add(new TMP_Dropdown.OptionData(team.ToString()));
                    }
                }
            }
            else if (currentGameMode == GameMode.TeamDeathmatch)
            {
                foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
                {
                    if (team == Team.Spectator | team == Team.Red | team == Team.Blue)
                    {
                        teamOptions.Add(new TMP_Dropdown.OptionData(team.ToString()));
                    }
                }
            }

            // Update team options
            if (changeTeamDropdown.options.Count != teamOptions.Count)
            {
                changeTeamDropdown.ClearOptions();
                changeTeamDropdown.AddOptions(teamOptions);
                ChangeTeam();
            }
            else // If list lengths are the same
            {
                for (int i = 0; i < changeTeamDropdown.options.Count; i++)
                {
                    if (changeTeamDropdown.options[i].text != teamOptions[i].text | changeTeamDropdown.options[i].image != teamOptions[i].image)
                    {
                        changeTeamDropdown.ClearOptions();
                        changeTeamDropdown.AddOptions(teamOptions);
                        ChangeTeam();
                        break;
                    }
                }
            }

            bool enableTeams = true;

            bool canStartGame = false;
            if (currentGameMode == GameMode.Duel)
            {
                int competitorCount = 0;
                foreach (ClientData clientData in ClientManager.Singleton.GetClientDataDictionary().Values)
                {
                    if (clientData.team == Team.Competitor) { competitorCount++; }
                }

                if (competitorCount < 2)
                {
                    errorDisplay.SetText("Duel requires there to be at least 2 competitors");
                }
                else if (competitorCount > 2)
                {
                    errorDisplay.SetText("There are too many competitors. Please change your team to spectator if you are not dueling");
                }
                else
                {
                    errorDisplay.SetText("");
                    canStartGame = true;
                }
            }
            else if (currentGameMode == GameMode.TeamDeathmatch)
            {

            }

            //startGameButton.interactable = canStartGame;

            changeTeamDropdown.interactable = enableTeams;

            // Put main camera in right spot to view player model
            if (playerModel)
                Camera.main.transform.position = playerModel.transform.position + cameraPositionOffset;

            // Player names logic
            foreach (Transform child in playerNamesParent)
            {
                Destroy(child.gameObject);
            }

            bool everyoneIsReady = true;
            if (ClientManager.Singleton.GetClientDataDictionary().Count == 0)
                everyoneIsReady = false;
            foreach (KeyValuePair<ulong, ClientData> valuePair in ClientManager.Singleton.GetClientDataDictionary())
            {
                GameObject nameIcon = Instantiate(playerNamePrefab, playerNamesParent);
                nameIcon.GetComponentInChildren<TextMeshProUGUI>().SetText(valuePair.Value.clientName);

                // Enable switch teams button if we are the local client for that nameIcon
                if (valuePair.Key == NetworkManager.Singleton.LocalClientId)
                    nameIcon.GetComponentInChildren<Button>().interactable = true;
                else
                    nameIcon.GetComponentInChildren<Button>().interactable = false;

                // Set the color of the team button
                Color teamColor = Color.black;
                Team clientTeam = ClientManager.Singleton.GetClient(valuePair.Key).team;
                if (clientTeam == Team.Red) { teamColor = Color.red; }
                else if (clientTeam == Team.Blue) { teamColor = Color.blue; }
                else if (clientTeam == Team.Spectator) { teamColor = Color.clear; }
                nameIcon.GetComponentInChildren<Button>(true).GetComponent<Image>().color = teamColor;
                nameIcon.GetComponentInChildren<Button>(true).gameObject.SetActive(enableTeams);

                // Change color of ready icon
                if (valuePair.Value.ready)
                {
                    Color newColor = new Color(0, 255, 0, 255);
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = newColor;
                    if (valuePair.Key == NetworkManager.Singleton.LocalClientId) // If this is the local player
                        readyButton.GetComponent<Image>().color = newColor;
                }
                else
                {
                    Color newColor = new Color(255, 0, 0, 255);
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = newColor;
                    if (valuePair.Key == NetworkManager.Singleton.LocalClientId) // If this is the local player
                        readyButton.GetComponent<Image>().color = newColor;
                }
                
                // Only make crown icon visible on the lobby leader
                if (valuePair.Key == ClientManager.Singleton.lobbyLeaderId.Value)
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 255);
                else
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 0);

                // Enable start button if everyone is ready
                if (!valuePair.Value.ready)
                    everyoneIsReady = false;

                // Set positions of all name icons
                for (int i = 0; i < playerNamesParent.childCount; i++)
                {
                    playerNamesParent.GetChild(i).localPosition = new Vector3(iconSpacing.x, -(i + 1) * iconSpacing.y, 0);
                }
            }

            if (everyoneIsReady)
            {
                if (NetworkManager.Singleton.LocalClientId == ClientManager.Singleton.lobbyLeaderId.Value)
                {
                    startButton.SetActive(true);
                }
                else
                {
                    // set waiting to start text to true
                    WaitingToStartText.SetActive(true);
                }
            }
            else
            {
                startButton.SetActive(false);
                WaitingToStartText.SetActive(false);
            }
        }
    }
}