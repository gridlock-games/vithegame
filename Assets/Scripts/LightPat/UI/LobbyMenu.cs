using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;
        public GameObject startButton;
        public GameObject readyButton;
        public Button changeTeamButton;
        public Button startGameButton;
        public GameObject WaitingToStartText;
        public TMP_Dropdown gameModeDropdown;
        public TMP_Dropdown playerModelDropdown;
        public TextMeshProUGUI errorDisplay;
        [Header("Loadout dropdowns")]
        public TMP_Dropdown primaryWeaponDropdown;
        public TMP_Dropdown secondaryWeaponDropdown;
        public TMP_Dropdown tertiaryWeaponDropdown;

        GameObject playerModel;
        Vector3 cameraPositionOffset;

        public void LeaveLobby()
        {
            Destroy(NetworkManager.Singleton.gameObject);
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("StartMenu");
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
                ClientManager.Singleton.ChangeSceneServerRpc(NetworkManager.Singleton.LocalClientId, "Arena_ver2", true);
            }
            else if (gameModeDropdown.options[gameModeDropdown.value].text == "Deathmatch")
            {
                ClientManager.Singleton.ChangeSceneServerRpc(NetworkManager.Singleton.LocalClientId, "Arena_ver2", true);
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
            GameMode chosenGameMode;
            System.Enum.TryParse(gameModeDropdown.options[gameModeDropdown.value].text, out chosenGameMode);
            ClientManager.Singleton.UpdateGameModeServerRpc(chosenGameMode);
        }

        public void ChangeTeam()
        {
            bool nextTeam = false;
            bool reached = false;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                if (nextTeam)
                {
                    reached = true;
                    ClientManager.Singleton.ChangeTeamServerRpc(localClientId, team);
                    break;
                }
                if (team == ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team)
                    nextTeam = true;
            }

            if (!reached)
                ClientManager.Singleton.ChangeTeamServerRpc(localClientId, Team.Red);
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

            if (NetworkManager.Singleton.IsClient)
                StartCoroutine(WaitForClientConnection());
        }

        private IEnumerator WaitForClientConnection()
        {
            yield return new WaitUntil(() => ClientManager.Singleton.GetClientDataDictionary().ContainsKey(NetworkManager.Singleton.LocalClientId));
            UpdatePlayerDisplay();
            UpdateGameModeValue();
            primaryWeaponDropdown.value = 0;
            secondaryWeaponDropdown.value = 1;
            tertiaryWeaponDropdown.value = 2;
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
            bool enableTeams = (GameMode)System.Enum.Parse(typeof(GameMode), gameModeDropdown.options[gameModeDropdown.value].text) != GameMode.Duel;
            changeTeamButton.interactable = enableTeams;

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
                if (ClientManager.Singleton.GetClient(valuePair.Key).team == Team.Red) { teamColor = Color.red; }
                else if (ClientManager.Singleton.GetClient(valuePair.Key).team == Team.Blue) { teamColor = Color.blue; }
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

            bool canStartGame = false;
            if (gameModeDropdown.options[gameModeDropdown.value].text == "Duel")
            {
                ulong[] clientIdArray = ClientManager.Singleton.GetClientDataDictionary().Keys.ToArray();
                if (clientIdArray.Length < 2)
                {
                    errorDisplay.SetText("Duel requires there to be at least 2 players in the lobby");
                }
                else if (clientIdArray.Length > 2)
                {
                    errorDisplay.SetText("Duel requires there to be only 2 players in the lobby");
                }
                else
                {
                    errorDisplay.SetText("");

                    canStartGame = true;

                    if (NetworkManager.Singleton.IsServer)
                    {
                        int counter = 0;
                        foreach (ulong clientId in clientIdArray)
                        {
                            if (counter == 0)
                                ClientManager.Singleton.ChangeTeamOnServer(clientId, Team.Red);
                            else if (counter == 1)
                                ClientManager.Singleton.ChangeTeamOnServer(clientId, Team.Blue);
                            counter++;
                        }
                    }
                }
            }
            startGameButton.interactable = canStartGame;

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