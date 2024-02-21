using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [SerializeField] private GameObject roomSettingsParent;
        [SerializeField] private GameObject lobbyUIParent;
        [Header("Lobby UI Assignments")]
        [SerializeField] private Vector3 previewCharacterPosition;
        [SerializeField] private Vector3 previewCharacterRotation;
        [SerializeField] private Button lockCharacterButton;
        [SerializeField] private Text characterLockTimeText;
        [SerializeField] private AccountCard playerAccountCardPrefab;
        [SerializeField] private AccountCardParent leftTeamParent;
        [SerializeField] private AccountCardParent rightTeamParent;
        [SerializeField] private Text gameModeText;
        [SerializeField] private Text mapText;
        [SerializeField] private Button roomSettingsButton;
        [SerializeField] private Image primaryWeaponIcon;
        [SerializeField] private Text primaryWeaponText;
        [SerializeField] private Image secondaryWeaponIcon;
        [SerializeField] private Text secondaryWeaponText;
        [SerializeField] private Button[] loadoutPresetButtons;
        [SerializeField] private Button spectateButton;
        [Header("Room Settings Assignments")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown mapDropdown;
        [SerializeField] private TMP_Dropdown teamDropdown;

        private NetworkVariable<float> characterLockTimer = new NetworkVariable<float>(60);
        private NetworkVariable<float> startGameTimer = new NetworkVariable<float>(5);

        [System.Serializable]
        private class AccountCardParent
        {
            public Text teamTitleText;
            public Transform transformParent;
            public Button addBotButton;
            public Button joinTeamButton;

            public void SetActive(bool isActive)
            {
                teamTitleText.gameObject.SetActive(isActive);
                transformParent.gameObject.SetActive(isActive);
                addBotButton.gameObject.SetActive(isActive);
                joinTeamButton.gameObject.SetActive(isActive);
            }
        }

        private void Awake()
        {
            lockedClients = new NetworkList<ulong>();

            CloseRoomSettings();

            // Game modes
            gameModeDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> gameModeOptions = new List<TMP_Dropdown.OptionData>();
            List<PlayerDataManager.GameMode> gameModeList = new List<PlayerDataManager.GameMode>();
            foreach (PlayerDataManager.GameMode gameMode in System.Enum.GetValues(typeof(PlayerDataManager.GameMode)))
            {
                if (gameMode == PlayerDataManager.GameMode.None) { continue; }
                gameModeList.Add(gameMode);
                gameModeOptions.Add(new TMP_Dropdown.OptionData(FromCamelCase(gameMode.ToString())));
            }
            gameModeDropdown.AddOptions(gameModeOptions);
            int gameModeIndex = gameModeList.IndexOf(PlayerDataManager.Singleton.GetGameMode());
            gameModeDropdown.SetValueWithoutNotify(gameModeIndex != -1 ? gameModeIndex : 0);
            ChangeGameMode();

            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            foreach (Button button in loadoutPresetButtons)
            {
                button.interactable = false;
            }
            spectateButton.interactable = false;
            lockCharacterButton.interactable = false;
            
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));
            
            foreach (Button button in loadoutPresetButtons)
            {
                button.interactable = true;
            }
            spectateButton.interactable = true;
            lockCharacterButton.interactable = true;
        }

        public static string FromCamelCase(string inputString)
        {
            string returnValue = inputString;

            //Strip leading "_" character
            returnValue = Regex.Replace(returnValue, "^_", "").Trim();
            //Add a space between each lower case character and upper case character
            returnValue = Regex.Replace(returnValue, "([a-z])([A-Z])", "$1 $2").Trim();
            //Add a space between 2 upper case characters when the second one is followed by a lower space character
            returnValue = Regex.Replace(returnValue, "([A-Z])([A-Z][a-z])", "$1 $2").Trim();

            return returnValue;
        }

        public override void OnNetworkSpawn()
        {
            characterLockTimer.OnValueChanged += OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged += OnStartGameTimerChange;
            lockedClients.OnListChanged += OnLockedClientListChange;

            if (IsClient)
            {
                StartCoroutine(UpdateCharacterPreview());
                StartCoroutine(InitializeLoadoutButtons());
            }
        }

        private IEnumerator InitializeLoadoutButtons()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));

            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutPresetButtons.Length; i++)
            {
                Button button = loadoutPresetButtons[i];
                int var = i;
                button.onClick.AddListener(delegate { ChooseLoadoutPreset(button, var); });
                if (PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).character.IsSlotActive(i)) { activeLoadoutSlot = i; }
            }
            loadoutPresetButtons[activeLoadoutSlot].onClick.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            characterLockTimer.OnValueChanged -= OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged -= OnStartGameTimerChange;
            lockedClients.OnListChanged -= OnLockedClientListChange;
        }

        private void OnCharacterLockTimerChange(float prev, float current)
        {
            if (IsServer)
            {
                if (prev > 0 & current <= 0) { LockCharacter(); }
            }
        }

        private void OnStartGameTimerChange(float prev, float current)
        {
            if (IsServer)
            {
                if (prev > 0 & current <= 0)
                {
                    switch (PlayerDataManager.Singleton.GetGameMode())
                    {
                        case PlayerDataManager.GameMode.FreeForAll:
                            NetSceneManager.Singleton.LoadScene("Free For All");
                            break;
                        case PlayerDataManager.GameMode.TeamElimination:
                            NetSceneManager.Singleton.LoadScene("Team Elimination");
                            break;
                        case PlayerDataManager.GameMode.EssenceWar:
                            NetSceneManager.Singleton.LoadScene("Essence War");
                            break;
                        case PlayerDataManager.GameMode.OutputRush:
                            NetSceneManager.Singleton.LoadScene("Outpost Rush");
                            break;
                        default:
                            Debug.LogError("Not sure what scene to load for game mode: " + PlayerDataManager.Singleton.GetGameMode());
                            break;
                    }

                    NetSceneManager.Singleton.LoadScene(PlayerDataManager.Singleton.GetMapName());
                }
            }
        }

        public void SwitchToSpectate()
        {
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId);
            if (playerData.team == PlayerDataManager.Team.Spectator)
            {
                playerData.team = PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[0];
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
            else
            {
                playerData.team = PlayerDataManager.Team.Spectator;
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
        }

        public void ReturnToCharacterSelect()
        {
            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(); }

            NetSceneManager.Singleton.LoadScene("Character Select");
        }

        private string lastPlayersString;
        private PlayerDataManager.GameMode lastGameMode;
        private Dictionary<PlayerDataManager.Team, Transform> teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CloseRoomSettings(); }
            
            // Timer logic
            List<PlayerDataManager.PlayerData> playerDataList = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators();
            bool startingGame = playerDataList.Count != 0;
            foreach (PlayerDataManager.PlayerData playerData in playerDataList)
            {
                if (playerData.id >= 0)
                {
                    if (!lockedClients.Contains((ulong)playerData.id))
                    {
                        startingGame = false;
                        break;
                    }
                }
            }

            //bool canCountDown = playerDataList.Count > 0 & playerDataList.Count % 2 == 0;
            bool canCountDown = playerDataList.Count >= 2;
            string cannotCountDownMessage = "";
            switch (PlayerDataManager.Singleton.GetGameMode())
            {
                case PlayerDataManager.GameMode.None:
                    Debug.LogError("Why the fuck is the game mode set to none");
                    canCountDown = false;
                    if (!canCountDown) { cannotCountDownMessage = "Not sure how to count down for game mode none"; }
                    break;
                case PlayerDataManager.GameMode.FreeForAll:
                    canCountDown = playerDataList.Count >= 2;

                    if (!canCountDown) { cannotCountDownMessage = "Need 2 or more players to play"; }
                    break;
                case PlayerDataManager.GameMode.TeamElimination:
                    List<PlayerDataManager.PlayerData> team1List = playerDataList.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[0]);
                    List<PlayerDataManager.PlayerData> team2List = playerDataList.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[1]);
                    canCountDown = team1List.Count > 0 & team2List.Count > 0 & team1List.Count == team2List.Count;

                    if (!(team1List.Count > 0 & team2List.Count > 0)) { cannotCountDownMessage = "Need 2 or more players to play"; }
                    else if (team1List.Count != team2List.Count) { cannotCountDownMessage = "Each team needs the same number of players"; }
                    break;
                case PlayerDataManager.GameMode.EssenceWar:
                    canCountDown = false;
                    if (!canCountDown) { cannotCountDownMessage = "Not sure how to count down for essence war"; }
                    break;
                case PlayerDataManager.GameMode.OutputRush:
                    canCountDown = false;
                    if (!canCountDown) { cannotCountDownMessage = "Not sure how to count down for outpost rush"; }
                    break;
                default:
                    Debug.Log("Not sure if we should count down for game mode: " + PlayerDataManager.Singleton.GetGameMode());
                    break;
            }

            if (IsServer)
            {
                if (canCountDown)
                {
                    if (startingGame) { startGameTimer.Value = Mathf.Clamp(startGameTimer.Value - Time.deltaTime, 0, Mathf.Infinity); }
                    else { characterLockTimer.Value = Mathf.Clamp(characterLockTimer.Value - Time.deltaTime, 0, Mathf.Infinity); }
                }
                else
                {
                    characterLockTimer.Value = 60;
                    startGameTimer.Value = 5;
                }
            }

            //characterLockTimeText.text = startingGame & canCountDown ? "Starting game in " + startGameTimer.Value.ToString("F0") : "Locking Characters in " + characterLockTimer.Value.ToString("F0");
            if (startingGame & canCountDown)
            {
                characterLockTimeText.text = "Starting game in " + startGameTimer.Value.ToString("F0");
            }
            else if (!canCountDown)
            {
                characterLockTimeText.text = cannotCountDownMessage;
            }
            else
            {
                characterLockTimeText.text = "Locking Characters in " + characterLockTimer.Value.ToString("F0");
            }

            roomSettingsButton.gameObject.SetActive(PlayerDataManager.Singleton.IsLobbyLeader() & !(startingGame & canCountDown));
            if (!roomSettingsButton.gameObject.activeSelf) { CloseRoomSettings(); }
            leftTeamParent.addBotButton.gameObject.SetActive(PlayerDataManager.Singleton.IsLobbyLeader() & !(startingGame & canCountDown) & leftTeamParent.teamTitleText.text != "");
            rightTeamParent.addBotButton.gameObject.SetActive(PlayerDataManager.Singleton.IsLobbyLeader() & !(startingGame & canCountDown) & rightTeamParent.teamTitleText.text != "");

            string playersString = "";
            foreach (PlayerDataManager.PlayerData data in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
            {
                playersString += data.id.ToString() + data.team.ToString() + data.character.name.ToString() + lockedClients.Contains((ulong)data.id).ToString();
            }

            if (lastPlayersString != playersString)
            {
                foreach (Transform child in leftTeamParent.transformParent)
                {
                    Destroy(child.gameObject);
                }

                foreach (Transform child in rightTeamParent.transformParent)
                {
                    Destroy(child.gameObject);
                }

                bool leftTeamJoinInteractable = false;
                bool rightTeamJoinInteractable = false;
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    if (teamParentDict.ContainsKey(playerData.team))
                    {
                        AccountCard accountCard = Instantiate(playerAccountCardPrefab.gameObject, teamParentDict[playerData.team]).GetComponent<AccountCard>();
                        accountCard.Initialize(playerData.id, lockedClients.Contains((ulong)playerData.id));

                        if (playerData.id == (int)NetworkManager.LocalClientId)
                        {
                            leftTeamJoinInteractable = teamParentDict[playerData.team] != leftTeamParent.transformParent;
                            rightTeamJoinInteractable = teamParentDict[playerData.team] != rightTeamParent.transformParent;
                        }
                    }
                }

                leftTeamParent.joinTeamButton.interactable = leftTeamJoinInteractable & !lockedClients.Contains(NetworkManager.LocalClientId);
                rightTeamParent.joinTeamButton.interactable = rightTeamJoinInteractable & !lockedClients.Contains(NetworkManager.LocalClientId);
            }
            lastPlayersString = playersString;

            if (IsClient)
            {
                if (PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team == PlayerDataManager.Team.Spectator)
                {
                    spectateButton.GetComponentInChildren<Text>().text = "STOP SPECTATE";
                }
                else
                {
                    spectateButton.GetComponentInChildren<Text>().text = "SPECTATE";
                }
            }

            if (PlayerDataManager.Singleton.GetGameMode() != lastGameMode)
            {
                // Player account card display logic
                teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
                PlayerDataManager.Team[] possibleTeams = PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams;
                // Put the local team into the first index
                if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
                {
                    PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team;
                    int teamIndex = System.Array.IndexOf(possibleTeams, localTeam);
                    if (teamIndex != -1)
                    {
                        possibleTeams[teamIndex] = possibleTeams[0];
                        possibleTeams[0] = localTeam;
                    }
                }

                leftTeamParent.teamTitleText.text = "";
                rightTeamParent.teamTitleText.text = "";
                leftTeamParent.addBotButton.onClick.RemoveAllListeners();
                rightTeamParent.addBotButton.onClick.RemoveAllListeners();
                
                for (int i = 0; i < possibleTeams.Length; i++)
                {
                    if (i == 0)
                    {
                        leftTeamParent.teamTitleText.text = PlayerDataManager.GetTeamText(possibleTeams[i]);
                        teamParentDict.Add(possibleTeams[i], leftTeamParent.transformParent);
                        PlayerDataManager.Team teamValue = possibleTeams[i];
                        leftTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
                    }
                    else if (i == 1)
                    {
                        rightTeamParent.teamTitleText.text = PlayerDataManager.GetTeamText(possibleTeams[i]);
                        teamParentDict.Add(possibleTeams[i], rightTeamParent.transformParent);
                        PlayerDataManager.Team teamValue = possibleTeams[i];
                        rightTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
                    }
                    else
                    {
                        Debug.LogError("Not sure where to parent team " + possibleTeams[i]);
                    }
                }

                leftTeamParent.SetActive(teamParentDict.ContainsValue(leftTeamParent.transformParent));
                rightTeamParent.SetActive(teamParentDict.ContainsValue(rightTeamParent.transformParent));

                // Maps
                mapDropdown.ClearOptions();
                List<TMP_Dropdown.OptionData> mapOptions = new List<TMP_Dropdown.OptionData>();
                List<string> mapList = new List<string>();
                foreach (string map in PlayerDataManager.Singleton.GetGameModeInfo().possibleMapSceneGroupNames)
                {
                    mapList.Add(map);
                    mapOptions.Add(new TMP_Dropdown.OptionData(map));
                }
                mapDropdown.AddOptions(mapOptions);
                int mapIndex = mapList.IndexOf(mapDropdown.options[mapDropdown.value].text);
                mapDropdown.SetValueWithoutNotify(mapIndex != -1 ? mapIndex : 0);
                ChangeMap();

                // Teams
                teamDropdown.ClearOptions();
                List<TMP_Dropdown.OptionData> teamOptions = new List<TMP_Dropdown.OptionData>();
                List<PlayerDataManager.Team> teamList = new List<PlayerDataManager.Team>();
                foreach (PlayerDataManager.Team team in PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams)
                {
                    teamList.Add(team);
                    teamOptions.Add(new TMP_Dropdown.OptionData(FromCamelCase(team.ToString())));
                }
                teamDropdown.AddOptions(teamOptions);
                if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
                {
                    int teamIndex = teamList.IndexOf(PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId).team);
                    teamDropdown.SetValueWithoutNotify(teamIndex != -1 ? teamIndex : 0);
                    ChangeTeam();
                }

                if (IsServer)
                {
                    List<int> botClientIds = new List<int>();
                    foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                    {
                        if (playerData.id < 0) { botClientIds.Add(playerData.id); }
                    }

                    foreach (int clientId in botClientIds)
                    {
                        PlayerDataManager.Singleton.KickPlayer(clientId);
                    }
                }
            }

            gameModeText.text = FromCamelCase(PlayerDataManager.Singleton.GetGameMode().ToString());
            mapText.text = PlayerDataManager.Singleton.GetMapName();

            lastGameMode = PlayerDataManager.Singleton.GetGameMode();
        }

        private GameObject previewObject;
        private IEnumerator UpdateCharacterPreview()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));

            WebRequestManager.Character character = PlayerDataManager.Singleton.GetPlayerData((int)NetworkManager.LocalClientId).character;

            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            KeyValuePair<int, int> kvp = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptionIndices(character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            CharacterReference.PlayerModelOption playerModelOption = playerModelOptionList[characterIndex];

            if (previewObject) { Destroy(previewObject); }
            // Instantiate the player model
            previewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));

            previewObject.GetComponent<AnimationHandler>().ChangeCharacter(character);
            StartCoroutine(previewObject.GetComponent<LoadoutManager>().ApplyDefaultEquipment(character.raceAndGender));
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (previewObject) { Destroy(previewObject); }
        }

        public void OpenRoomSettings()
        {
            roomSettingsParent.SetActive(true);
            lobbyUIParent.SetActive(false);
        }

        public void CloseRoomSettings()
        {
            roomSettingsParent.SetActive(false);
            lobbyUIParent.SetActive(true);
        }

        public void AddBot(PlayerDataManager.Team team)
        {
            PlayerDataManager.Singleton.AddBotData(team);
        }

        private void ChooseLoadoutPreset(Button button, int loadoutSlot)
        {
            foreach (Button b in loadoutPresetButtons)
            {
                b.interactable = button != b;
            }

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId);

            PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UseCharacterLoadout(playerData.character._id.ToString(), (loadoutSlot + 1).ToString()));

            playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlot);
            PlayerDataManager.Singleton.SetPlayerData(playerData);

            CharacterReference.WeaponOption primaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetLoadoutFromSlot(loadoutSlot).weapon1ItemId).itemId);
            CharacterReference.WeaponOption secondaryOption = System.Array.Find(weaponOptions, item => item.itemWebId == WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.id == playerData.character.GetLoadoutFromSlot(loadoutSlot).weapon2ItemId).itemId);

            primaryWeaponIcon.sprite = primaryOption.weaponIcon;
            primaryWeaponText.text = primaryOption.name;
            secondaryWeaponIcon.sprite = secondaryOption.weaponIcon;
            secondaryWeaponText.text = secondaryOption.name;

            if (previewObject)
            {
                previewObject.GetComponent<LoadoutManager>().ChangeWeaponBeforeSpawn(LoadoutManager.WeaponSlotType.Primary, primaryOption);
                previewObject.GetComponent<LoadoutManager>().ChangeWeaponBeforeSpawn(LoadoutManager.WeaponSlotType.Secondary, secondaryOption);
            }
        }

        public void ChangeGameMode()
        {
            PlayerDataManager.Singleton.SetGameMode(System.Enum.Parse<PlayerDataManager.GameMode>(gameModeDropdown.options[gameModeDropdown.value].text.Replace(" ", "")));
        }

        public void ChangeMap()
        {
            PlayerDataManager.Singleton.SetMap(mapDropdown.options[mapDropdown.value].text);
        }

        public void ChangeTeam()
        {
            if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId);
                playerData.team = System.Enum.Parse<PlayerDataManager.Team>(teamDropdown.options[teamDropdown.value].text.Replace(" ", ""));
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
        }

        public void LockCharacter()
        {
            if (IsClient)
            {
                LockCharacterServerRpc(NetworkManager.LocalClientId);
            }
            else
            {
                foreach (var playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    if (playerData.id >= 0)
                    {
                        lockedClients.Add((ulong)playerData.id);
                    }
                }
            }
        }

        private void LockCharacterLocal()
        {
            lockCharacterButton.interactable = false;
            spectateButton.interactable = false;
            foreach (Button button in loadoutPresetButtons)
            {
                button.interactable = false;
            }
        }

        private NetworkList<ulong> lockedClients;

        [ServerRpc(RequireOwnership = false)] private void LockCharacterServerRpc(ulong clientId) { lockedClients.Add(clientId); }

        private void OnLockedClientListChange(NetworkListEvent<ulong> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<ulong>.EventType.Add)
            {
                if (networkListEvent.Value == NetworkManager.LocalClientId) { LockCharacterLocal(); }
            }
        }
    }
}