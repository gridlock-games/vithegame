using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using System.Text.RegularExpressions;
using Vi.Utility;
using System.Linq;
using Unity.Collections;
using Vi.Core.CombatAgents;

namespace Vi.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject roomSettingsParent;
        [SerializeField] private GameObject lobbyUIParent;
        [Header("Character Preview")]
        [SerializeField] private Camera characterPreviewCamera;
        [SerializeField] private RawImage characterPreviewImage;
        [Header("Lobby UI Assignments")]
        [SerializeField] private Button returnToHubButton;
        [SerializeField] private Vector3 previewCharacterPosition;
        [SerializeField] private Vector3 previewCharacterRotation;
        [SerializeField] private Button lockCharacterButton;
        [SerializeField] private Button startGameButton;
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
        [SerializeField] private GameObject loadoutButtonParent;
        [SerializeField] private Button[] loadoutPresetButtons;
        [SerializeField] private Button spectateButton;
        [SerializeField] private Text spectatorCountText;
        [Header("Room Settings Assignments")]
        [SerializeField] private GameModeOption gameModeOptionPrefab;
        [SerializeField] private Transform gameModeOptionParent;
        [SerializeField] private MapOption mapOptionPrefab;
        [SerializeField] private Transform mapOptionParent;
        [SerializeField] private Text gameModeSpecificSettingsTitleText;
        [SerializeField] private GameModeInfoUI gameModeInfoUI;
        [SerializeField] private CustomSettingsParent[] customSettingsParents;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] GameObject modeOptions;
        [SerializeField] GameObject mapOptions;
        [SerializeField] GameObject settingsOptions;
        [SerializeField] Image mapPreview;
        [SerializeField] private Text roomSettingsMapNameText;

        private GameObject pauseInstance;
        public void OpenSettingsMenu()
        {
            pauseInstance = Instantiate(pauseMenu.gameObject);
        }

        [System.Serializable]
        private struct CustomSettingsParent
        {
            public PlayerDataManager.GameMode gameMode;
            public Transform parent;
            public CustomSettingsInputField[] inputFields;

            [System.Serializable]
            public struct CustomSettingsInputField
            {
                public string key;
                public InputField inputField;
            }
        }

        private const float characterLockTime = 60;
        private const float startGameTime = 5;

        private NetworkVariable<float> characterLockTimer = new NetworkVariable<float>(characterLockTime);
        private NetworkVariable<float> startGameTimer = new NetworkVariable<float>(startGameTime);

        [System.Serializable]
        private class AccountCardParent
        {
            public Text teamTitleText;
            public Transform transformParent;
            public Button addBotButton;
            public Button joinTeamButton;
            public Button editTeamNameButton;
            public InputField teamNameOverrideInputField;
            public InputField teamPrefixOverrideInputField;

            [HideInInspector] public PlayerDataManager.Team team = PlayerDataManager.Team.Competitor;

            public void SetActive(bool isActive)
            {
                teamTitleText.gameObject.SetActive(isActive);
                transformParent.gameObject.SetActive(true);
                addBotButton.gameObject.SetActive(isActive);
                joinTeamButton.gameObject.SetActive(isActive);
                editTeamNameButton.gameObject.SetActive(isActive);
                teamNameOverrideInputField.gameObject.SetActive(false);
                teamPrefixOverrideInputField.gameObject.SetActive(false);
            }

            public void ToggleTeamNameEditMode()
            {
                if (teamNameOverrideInputField.gameObject.activeSelf)
                {
                    teamNameOverrideInputField.gameObject.SetActive(false);
                    teamPrefixOverrideInputField.gameObject.SetActive(false);
                }
                else
                {
                    teamNameOverrideInputField.gameObject.SetActive(true);
                    teamPrefixOverrideInputField.gameObject.SetActive(true);
                }
            }

            public void ApplyTeamNameOverride()
            {
                PlayerDataManager.Singleton.SetTeamNameOverride(team, teamNameOverrideInputField.text, teamPrefixOverrideInputField.text);
            }
        }

        private readonly static List<PlayerDataManager.GameMode> whitelistedGameModes = new List<PlayerDataManager.GameMode>()
        {
            PlayerDataManager.GameMode.FreeForAll,
            PlayerDataManager.GameMode.TeamElimination,
            PlayerDataManager.GameMode.HordeMode,
            PlayerDataManager.GameMode.EssenceWar
        };

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;

        public void showMapOptions() {
            modeOptions.SetActive(false);
            settingsOptions.SetActive(false);
            mapOptions.SetActive(true);
        }

        public void showModeOptions() {
            modeOptions.SetActive(true);
            settingsOptions.SetActive(false);
            mapOptions.SetActive(false);
        }

        public void showSettingsOptions() {
            modeOptions.SetActive(false);
            settingsOptions.SetActive(true);
            mapOptions.SetActive(false);
        }

        private void Awake()
        {
            lockedClients = new NetworkList<ulong>();

            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            returnToHubButton.onClick.AddListener(() => ReturnToHub());

            CloseRoomSettings();

            // Game modes
            bool first = true;
            foreach (PlayerDataManager.GameMode gameMode in System.Enum.GetValues(typeof(PlayerDataManager.GameMode)))
            {
                if (!whitelistedGameModes.Contains(gameMode)) { continue; }
                //if (gameMode == PlayerDataManager.GameMode.None) { continue; }

                if (!Application.isEditor & gameMode == PlayerDataManager.GameMode.EssenceWar) { continue; }

                GameModeOption option = Instantiate(gameModeOptionPrefab.gameObject, gameModeOptionParent).GetComponent<GameModeOption>();
                StartCoroutine(option.Initialize(gameMode, PlayerDataManager.Singleton.GetGameModeIcon(gameMode)));

                if (first & PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None) { PlayerDataManager.Singleton.SetGameMode(gameMode); first = false; }
            }

            foreach (CustomSettingsParent customSettingsParent in customSettingsParents)
            {
                foreach (CustomSettingsParent.CustomSettingsInputField customSettingsInputField in customSettingsParent.inputFields)
                {
                    customSettingsInputField.inputField.onValueChanged.AddListener(delegate { ValidateInputAsInt(customSettingsInputField.inputField); });
                }
            }

            //HandlePlatformAPI();
            SyncRoomSettingsFields();

            StartCoroutine(Init());

            leftTeamParent.editTeamNameButton.onClick.AddListener(() => leftTeamParent.ToggleTeamNameEditMode());
            rightTeamParent.editTeamNameButton.onClick.AddListener(() => rightTeamParent.ToggleTeamNameEditMode());

            leftTeamParent.teamNameOverrideInputField.onEndEdit.AddListener(delegate { leftTeamParent.ApplyTeamNameOverride(); });
            rightTeamParent.teamNameOverrideInputField.onEndEdit.AddListener(delegate { rightTeamParent.ApplyTeamNameOverride(); });

            leftTeamParent.teamPrefixOverrideInputField.onEndEdit.AddListener(delegate { leftTeamParent.ApplyTeamNameOverride(); });
            rightTeamParent.teamPrefixOverrideInputField.onEndEdit.AddListener(delegate { rightTeamParent.ApplyTeamNameOverride(); });

        }

        private void Start()
        {
            if (characterPreviewCamera)
            {
                characterPreviewCamera.transform.SetParent(null);
                characterPreviewCamera.transform.position = new Vector3(0, 2.033f, -9.592f);
                characterPreviewCamera.transform.rotation = Quaternion.Euler(18.07f, 0, 0);
                characterPreviewImage.texture = characterPreviewCamera.targetTexture;
            }

            StartCoroutine(AutomatedClientLogic());
        }

        private IEnumerator AutomatedClientLogic()
        {
            if (!FasterPlayerPrefs.IsAutomatedClient) { yield break; }

            yield return new WaitForSeconds(5);

            yield return new WaitUntil(() => lockCharacterButton.gameObject.activeSelf & lockCharacterButton.interactable);

            lockCharacterButton.onClick.Invoke();

            yield return new WaitForSeconds(1);
            yield return new WaitUntil(() => startGameButton.gameObject.activeSelf & startGameButton.interactable);

            startGameButton.onClick.Invoke();
        }

        private void SyncRoomSettingsFields()
        {
            foreach (CustomSettingsParent.CustomSettingsInputField customSettingsInputField in System.Array.Find(customSettingsParents, item => item.gameMode == PlayerDataManager.Singleton.GetGameMode()).inputFields)
            {
                foreach (string propertyString in PlayerDataManager.Singleton.GetGameModeSettings().Split("|"))
                {
                    string[] propertySplit = propertyString.Split(":");
                    string propertyName = "";
                    int value = 0;
                    for (int i = 0; i < propertySplit.Length; i++)
                    {
                        if (i == 0)
                        {
                            propertyName = propertySplit[i];
                        }
                        else if (i == 1)
                        {
                            value = int.Parse(propertySplit[i]);
                        }
                        else
                        {
                            Debug.LogError("Not sure how to parse game mode property string " + propertyString);
                        }
                    }

                    if (propertyName == customSettingsInputField.key) { customSettingsInputField.inputField.text = value.ToString(); }
                }
            }
        }

        private void RefreshMapOptions()
        {
            foreach (Transform child in mapOptionParent)
            {
                Destroy(child.gameObject);
            }

            foreach (string mapName in PlayerDataManager.Singleton.GetGameModeInfo().possibleMapSceneGroupNames)
            {
                MapOption option = Instantiate(mapOptionPrefab.gameObject, mapOptionParent).GetComponent<MapOption>();
                StartCoroutine(option.Initialize(mapName, NetSceneManager.Singleton.GetSceneGroupIcon(mapName, 0)));
            }
        }

        private void RefreshGameMode(bool calledFromInit)
        {
            // Player account card display logic
            teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
            PlayerDataManager.Team[] possibleTeams = PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams;
            // Put the local team into the first index
            if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
            {
                PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
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

            leftTeamParent.joinTeamButton.onClick.RemoveAllListeners();
            rightTeamParent.joinTeamButton.onClick.RemoveAllListeners();

            for (int i = 0; i < possibleTeams.Length; i++)
            {
                if (i == 0)
                {
                    leftTeamParent.teamTitleText.text = PlayerDataManager.Singleton.GetTeamText(possibleTeams[i]);
                    teamParentDict.Add(possibleTeams[i], leftTeamParent.transformParent);
                    PlayerDataManager.Team teamValue = possibleTeams[i];
                    leftTeamParent.team = teamValue;
                    leftTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
                    leftTeamParent.joinTeamButton.onClick.AddListener(delegate { ChangeTeam(teamValue); });
                }
                else if (i == 1)
                {
                    rightTeamParent.teamTitleText.text = PlayerDataManager.Singleton.GetTeamText(possibleTeams[i]);
                    teamParentDict.Add(possibleTeams[i], rightTeamParent.transformParent);
                    PlayerDataManager.Team teamValue = possibleTeams[i];
                    rightTeamParent.team = teamValue;
                    rightTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
                    rightTeamParent.joinTeamButton.onClick.AddListener(delegate { ChangeTeam(teamValue); });
                }
                else
                {
                    Debug.LogError("Not sure where to parent team " + possibleTeams[i]);
                }
            }

            leftTeamParent.SetActive(teamParentDict.ContainsValue(leftTeamParent.transformParent));
            rightTeamParent.SetActive(teamParentDict.ContainsValue(rightTeamParent.transformParent));

            leftTeamParent.joinTeamButton.gameObject.SetActive(teamParentDict.Count > 1);
            rightTeamParent.joinTeamButton.gameObject.SetActive(teamParentDict.Count > 1);

            if (PlayerDataManager.Singleton.IsLobbyLeader())
            {
                leftTeamParent.teamNameOverrideInputField.text = "";
                leftTeamParent.teamPrefixOverrideInputField.text = "";

                rightTeamParent.teamNameOverrideInputField.text = "";
                rightTeamParent.teamPrefixOverrideInputField.text = "";

                leftTeamParent.ApplyTeamNameOverride();
                rightTeamParent.ApplyTeamNameOverride();
            }
            else
            {
                leftTeamParent.teamNameOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamText(leftTeamParent.team));
                leftTeamParent.teamPrefixOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamPrefixRaw(leftTeamParent.team));

                rightTeamParent.teamNameOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamText(rightTeamParent.team));
                rightTeamParent.teamPrefixOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamPrefixRaw(rightTeamParent.team));
            }

            // Maps
            RefreshMapOptions();

            // Even out teams
            if (IsServer & !calledFromInit)
            {
                // Check if any team has more than the limit of players
                Dictionary<PlayerDataManager.Team, int> originalTeamCounts = new Dictionary<PlayerDataManager.Team, int>();
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                {
                    if (originalTeamCounts.ContainsKey(playerData.team))
                    {
                        originalTeamCounts[playerData.team]++;
                    }
                    else
                    {
                        originalTeamCounts.Add(playerData.team, 1);
                    }
                }

                // If there is a player data element that is on a team other than the possible teams
                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => !possibleTeams.Contains(item.team))
                    | originalTeamCounts.Any(item => item.Value > PlayerDataManager.Singleton.GetMaxPlayersForMap() / possibleTeams.Length))
                {
                    Dictionary<PlayerDataManager.Team, int> teamCounts = new Dictionary<PlayerDataManager.Team, int>();
                    foreach (PlayerDataManager.Team possibleTeam in possibleTeams)
                    {
                        teamCounts.Add(possibleTeam, PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Where(item => item.team == possibleTeam).ToArray().Length);
                    }

                    foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
                    {
                        PlayerDataManager.PlayerData newPlayerData = playerData;
                        // Get the team with the lowest player count
                        newPlayerData.team = teamCounts.Aggregate((l, r) => l.Value <= r.Value ? l : r).Key;

                        PlayerDataManager.Singleton.SetPlayerDataWithServerAuth(newPlayerData);

                        if (teamCounts.ContainsKey(playerData.team)) { teamCounts[playerData.team]--; }
                        teamCounts[newPlayerData.team]++;
                    }
                }
            }

            // Show game mode info UI
            if (roomSettingsParent.activeSelf)
            {
                if (PlayerDataManager.Singleton.IsLobbyLeader() & IsClient)
                {
                    string gameModeString = PlayerDataManager.Singleton.GetGameMode().ToString();
                    if (!FasterPlayerPrefs.Singleton.HasString(gameModeString))
                    {
                        FasterPlayerPrefs.Singleton.SetString(gameModeString, "");
                        gameModeInfoUI.gameObject.SetActive(true);
                        gameModeInfoUI.Initialize(PlayerDataManager.Singleton.GetGameMode(), true);
                    }
                }
            }
        }

        private IEnumerator Init()
        {
            primaryWeaponIcon.enabled = false;
            secondaryWeaponIcon.enabled = false;

            primaryWeaponText.enabled = false;
            secondaryWeaponText.enabled = false;

            foreach (Button button in loadoutPresetButtons)
            {
                button.interactable = false;
            }
            spectateButton.interactable = false;
            lockCharacterButton.interactable = false;
            
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));

            // Do not remove this, it handles a fringe case for refreshing the local player's team properly
            yield return null;

            primaryWeaponIcon.enabled = true;
            secondaryWeaponIcon.enabled = true;

            primaryWeaponText.enabled = true;
            secondaryWeaponText.enabled = true;

            // Initialze loadout presset buttons
            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutPresetButtons.Length; i++)
            {
                Button button = loadoutPresetButtons[i];
                int var = i;
                button.onClick.AddListener(delegate { ChooseLoadoutPreset(button, var); });
                if (PlayerDataManager.Singleton.LocalPlayerData.character.IsSlotActive(i)) { activeLoadoutSlot = i; }
            }
            loadoutPresetButtons[activeLoadoutSlot].onClick.Invoke();

            spectateButton.interactable = true;
            lockCharacterButton.interactable = true;

            lockCharacterButton.onClick.RemoveAllListeners();
            lockCharacterButton.onClick.AddListener(LockCharacter);
            lockCharacterButton.GetComponentInChildren<Text>().text = "LOCK";

            RefreshGameMode(true);

            if (IsSpawned)
            {
                CreateCharacterPreview();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            characterLockTimer.OnValueChanged += OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged += OnStartGameTimerChange;
            lockedClients.OnListChanged += OnLockedClientListChange;
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
                            NetSceneManager.Singleton.LoadScene("Free For All", PlayerDataManager.Singleton.GetMapName());
                            break;
                        case PlayerDataManager.GameMode.TeamElimination:
                            NetSceneManager.Singleton.LoadScene("Team Elimination", PlayerDataManager.Singleton.GetMapName());
                            break;
                        case PlayerDataManager.GameMode.EssenceWar:
                            NetSceneManager.Singleton.LoadScene("Essence War", PlayerDataManager.Singleton.GetMapName());
                            break;
                        case PlayerDataManager.GameMode.OutpostRush:
                            NetSceneManager.Singleton.LoadScene("Outpost Rush", PlayerDataManager.Singleton.GetMapName());
                            break;
                        case PlayerDataManager.GameMode.TeamDeathmatch:
                            NetSceneManager.Singleton.LoadScene("Team Deathmatch", PlayerDataManager.Singleton.GetMapName());
                            break;
                        case PlayerDataManager.GameMode.HordeMode:
                            NetSceneManager.Singleton.LoadScene("Horde Mode", PlayerDataManager.Singleton.GetMapName());
                            break;
                        default:
                            Debug.LogError("Not sure what scene to load for game mode: " + PlayerDataManager.Singleton.GetGameMode());
                            break;
                    }
                }
            }
        }

        public void SwitchToSpectate()
        {
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.LocalPlayerData;
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

        private void ReturnToHub()
        {
            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown); }

            NetSceneManager.Singleton.LoadScene("Character Select");
            PersistentLocalObjects.Singleton.StartCoroutine(ReturnToHubCoroutine());
        }

        private IEnumerator ReturnToHubCoroutine()
        {
            returnToHubButton.interactable = false;

            if (NetworkManager.Singleton.IsListening)
            {
                PlayerDataManager.Singleton.WasDisconnectedByClient = true;
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            if (WebRequestManager.Singleton.HubServers.Length > 0)
            {
                yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
                networkTransport.SetConnectionData(WebRequestManager.Singleton.HubServers[0].ip, ushort.Parse(WebRequestManager.Singleton.HubServers[0].port), FasterPlayerPrefs.serverListenAddress);
                NetworkManager.Singleton.StartClient();
            }
        }

        public void ValidateInputAsInt(InputField inputField)
        {
            inputField.text = Regex.Replace(inputField.text, @"[^0-9]", "");
        }

        private string lastDataString;
        private PlayerDataManager.GameMode lastGameMode;
        private Dictionary<PlayerDataManager.Team, Transform> teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
        private void Update()
        {
            if (IsClient)
            {
                loadoutButtonParent.SetActive(PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator);
            }
            else
            {
                loadoutButtonParent.SetActive(false);
            }

            characterPreviewImage.enabled = loadoutButtonParent.activeSelf;

            if (PlayerDataManager.Singleton.TeamNameOverridesUpdatedThisFrame)
            {
                leftTeamParent.teamNameOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamText(leftTeamParent.team));
                leftTeamParent.teamPrefixOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamPrefixRaw(leftTeamParent.team));

                rightTeamParent.teamNameOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamText(rightTeamParent.team));
                rightTeamParent.teamPrefixOverrideInputField.SetTextWithoutNotify(PlayerDataManager.Singleton.GetTeamPrefixRaw(rightTeamParent.team));

                leftTeamParent.teamTitleText.text = PlayerDataManager.Singleton.GetTeamText(leftTeamParent.team);
                rightTeamParent.teamTitleText.text = PlayerDataManager.Singleton.GetTeamText(rightTeamParent.team);
            }

            int inputFieldCount = 0;
            foreach (CustomSettingsParent customSettingsParent in customSettingsParents)
            {
                customSettingsParent.parent.gameObject.SetActive(customSettingsParent.gameMode == PlayerDataManager.Singleton.GetGameMode());

                if (customSettingsParent.parent.gameObject.activeSelf)
                {
                    inputFieldCount = customSettingsParent.inputFields.Length;
                }
            }

            gameModeSpecificSettingsTitleText.gameObject.SetActive(inputFieldCount > 0);
            gameModeSpecificSettingsTitleText.text = PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode());

            List<PlayerDataManager.PlayerData> playerDataListWithSpectators = PlayerDataManager.Singleton.GetPlayerDataListWithSpectators();
            List<PlayerDataManager.PlayerData> playerDataListWithoutSpectators = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators();
            spectatorCountText.text = "Spectator Count: " + playerDataListWithSpectators.FindAll(item => item.team == PlayerDataManager.Team.Spectator).Count.ToString();

            // Timer logic
            KeyValuePair<bool, PlayerDataManager.PlayerData> lobbyLeaderKvp = PlayerDataManager.Singleton.GetLobbyLeader();
            bool isLobbyLeader = PlayerDataManager.Singleton.IsLobbyLeader();

            bool canStartGame = characterLockTimer.Value <= 0;
            if (!canStartGame)
            {
                if (lobbyLeaderKvp.Key | isLobbyLeader) // If a lobby leader exists
                {
                    // Start game is true if all players have locked in, or if the lock timer is at 0.
                    canStartGame = playerDataListWithoutSpectators.TrueForAll(item => lockedClients.Contains((ulong)item.id) | item.id < 0);
                }
            }
            
            bool canCountDown = false;
            string cannotCountDownMessage = "";
            switch (PlayerDataManager.Singleton.GetGameMode())
            {
                case PlayerDataManager.GameMode.None:
                    Debug.LogError("Why the fuck is the game mode set to none");
                    canCountDown = false;
                    if (!canCountDown) { cannotCountDownMessage = "Not sure how to count down for game mode none"; }
                    break;
                case PlayerDataManager.GameMode.FreeForAll:
                    canCountDown = playerDataListWithoutSpectators.Count >= 2;

                    if (!canCountDown) { cannotCountDownMessage = "Need 2 or more players to play"; }
                    break;
                case PlayerDataManager.GameMode.TeamElimination:
                    List<PlayerDataManager.PlayerData> team1List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[0]);
                    List<PlayerDataManager.PlayerData> team2List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[1]);
                    canCountDown = team1List.Count >= 2 & team2List.Count >= 2 & team1List.Count == team2List.Count;

                    if (!(team1List.Count >= 2 & team2List.Count >= 2)) { cannotCountDownMessage = "Need 2 or more players on each team to play"; }
                    else if (team1List.Count != team2List.Count) { cannotCountDownMessage = "Each team needs the same number of players"; }
                    break;
                case PlayerDataManager.GameMode.EssenceWar:
                    team1List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[0]);
                    team2List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[1]);
                    canCountDown = (team1List.Count == 3 & team2List.Count == 3) | (team1List.Count == 5 & team2List.Count == 5);

                    if (!canCountDown) { cannotCountDownMessage = "Essence War is 3v3 or 5v5 only"; }
                    break;
                case PlayerDataManager.GameMode.OutpostRush:
                    canCountDown = false;
                    if (!canCountDown) { cannotCountDownMessage = "Not sure how to count down for outpost rush"; }
                    break;
                case PlayerDataManager.GameMode.TeamDeathmatch:
                    team1List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[0]);
                    team2List = playerDataListWithoutSpectators.FindAll(item => item.team == PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams[1]);
                    canCountDown = team1List.Count >= 2 & team2List.Count >= 2 & team1List.Count == team2List.Count;

                    if (!(team1List.Count >= 2 & team2List.Count >= 2)) { cannotCountDownMessage = "Need 2 or more players on each team to play"; }
                    else if (team1List.Count != team2List.Count) { cannotCountDownMessage = "Each team needs the same number of players"; }
                    break;
                case PlayerDataManager.GameMode.HordeMode:
                    canCountDown = playerDataListWithoutSpectators.Count >= 1;

                    if (!canCountDown) { cannotCountDownMessage = "Need 1 or more players to play"; }
                    break;
                default:
                    Debug.Log("Not sure if we should count down for game mode: " + PlayerDataManager.Singleton.GetGameMode());
                    break;
            }
            int maxPlayersForMap = PlayerDataManager.Singleton.GetMaxPlayersForMap();
            if (playerDataListWithoutSpectators.Count > maxPlayersForMap)
            {
                canCountDown = false;
                cannotCountDownMessage = "Cannot play a " + PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()) + " match on " + PlayerDataManager.Singleton.GetMapName() + " with more than " + maxPlayersForMap.ToString() + " players";
            }

            bool roomSettingsParsedProperly = true;
            if (isLobbyLeader)
            {
                string gameModeSettings = "";
                foreach (CustomSettingsParent.CustomSettingsInputField customSettingsInputField in System.Array.Find(customSettingsParents, item => item.gameMode == PlayerDataManager.Singleton.GetGameMode()).inputFields)
                {
                    if (int.TryParse(customSettingsInputField.inputField.text, out int result))
                    {
                        roomSettingsParsedProperly = result > 0 & roomSettingsParsedProperly;
                        gameModeSettings += customSettingsInputField.key + ":" + result.ToString() + "|";
                        if (!roomSettingsParsedProperly) { cannotCountDownMessage = StringUtility.FromCamelCase(customSettingsInputField.key) + " must be greater than 0. Please edit room settings"; break; }
                    }
                    else
                    {
                        cannotCountDownMessage = StringUtility.FromCamelCase(customSettingsInputField.key) + " must have an integer value. Please edit room settings";
                        roomSettingsParsedProperly = false & roomSettingsParsedProperly;
                        break;
                    }
                }

                if (roomSettingsParsedProperly)
                {
                    PlayerDataManager.Singleton.SetGameModeSettings(gameModeSettings);
                }
            }
            else // Parse room settings into input fields
            {
                SyncRoomSettingsFields();
            }

            canCountDown &= roomSettingsParsedProperly;

            if (IsServer)
            {
                if (canCountDown)
                {
                    if (canStartGame)
                    {
                        if (startGameCalled.Value) { startGameTimer.Value = Mathf.Clamp(startGameTimer.Value - Time.deltaTime, 0, Mathf.Infinity); }
                    }
                    else
                    {
                        characterLockTimer.Value = Mathf.Clamp(characterLockTimer.Value - Time.deltaTime, 0, Mathf.Infinity);
                        startGameTimer.Value = startGameTime;

                        startGameCalled.Value = false;
                    }
                }
                else
                {
                    characterLockTimer.Value = characterLockTime;
                    startGameTimer.Value = startGameTime;

                    startGameCalled.Value = false;
                }
            }

            startGameButton.interactable = !startGameCalled.Value & !startGameServerRpcInProgress;

            startGameButton.gameObject.SetActive(canCountDown & canStartGame & isLobbyLeader);
            lockCharacterButton.gameObject.SetActive(!(canCountDown & canStartGame));

            if (canStartGame & canCountDown)
            {
                characterLockTimeText.text = startGameCalled.Value ? "Starting game in " + startGameTimer.Value.ToString("F0") : "Waiting for lobby leader to start game";
            }
            else if (!canCountDown)
            {
                characterLockTimeText.text = cannotCountDownMessage;
            }
            else
            {
                characterLockTimeText.text = "Locking Characters in " + characterLockTimer.Value.ToString("F0");
            }

            roomSettingsButton.gameObject.SetActive(isLobbyLeader & Mathf.Approximately(startGameTimer.Value, startGameTime));
            if (!roomSettingsButton.gameObject.activeSelf) { CloseRoomSettings(); }
            
            leftTeamParent.addBotButton.gameObject.SetActive(isLobbyLeader & !(canStartGame & canCountDown) & leftTeamParent.teamTitleText.gameObject.activeSelf);
            rightTeamParent.addBotButton.gameObject.SetActive(isLobbyLeader & !(canStartGame & canCountDown) & rightTeamParent.teamTitleText.gameObject.activeSelf);

            leftTeamParent.addBotButton.interactable = playerDataListWithoutSpectators.Count < maxPlayersForMap;
            rightTeamParent.addBotButton.interactable = playerDataListWithoutSpectators.Count < maxPlayersForMap;

            leftTeamParent.editTeamNameButton.gameObject.SetActive(isLobbyLeader & leftTeamParent.teamTitleText.gameObject.activeSelf & teamParentDict.ContainsValue(leftTeamParent.transformParent) & teamParentDict.ContainsValue(rightTeamParent.transformParent));
            rightTeamParent.editTeamNameButton.gameObject.SetActive(isLobbyLeader & rightTeamParent.teamTitleText.gameObject.activeSelf & teamParentDict.ContainsValue(leftTeamParent.transformParent) & teamParentDict.ContainsValue(rightTeamParent.transformParent));
            
            string dataString = "";
            foreach (PlayerDataManager.PlayerData data in playerDataListWithSpectators)
            {
                dataString += data.id.ToString() + data.character._id.ToString();
            }

            if (IsServer)
            {
                if (dataString != lastDataString)
                {
                    characterLockTimer.Value = characterLockTime;
                    startGameTimer.Value = startGameTime;
                }
            }

            lastDataString = dataString;

            if (IsClient)
            {
                if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
                {
                    if (PlayerDataManager.Singleton.LocalPlayerData.team == PlayerDataManager.Team.Spectator)
                    {
                        spectateButton.GetComponentInChildren<Text>().text = "STOP SPECTATE";
                    }
                    else
                    {
                        spectateButton.GetComponentInChildren<Text>().text = "SPECTATE";
                    }
                }
            }

            if (PlayerDataManager.Singleton.GetGameMode() != lastGameMode)
            {
                RefreshGameMode(false);
                UpdateRichPresence();
            }

            gameModeText.text = PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode());

            if (mapText.text != PlayerDataManager.Singleton.GetMapName())
            {
                UpdateRichPresence();
            }
            mapText.text = PlayerDataManager.Singleton.GetMapName();
            
            roomSettingsMapNameText.text = mapText.text;

            lastGameMode = PlayerDataManager.Singleton.GetGameMode();

            backgroundImage.sprite = NetSceneManager.Singleton.GetSceneGroupIcon(PlayerDataManager.Singleton.GetMapName(), 0);
            mapPreview.sprite = backgroundImage.sprite;

            RefreshPlayerCards();
        }

        private GameObject previewObject;
        private void CreateCharacterPreview()
        {
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { Debug.LogError("Calling create character preview before the local client is in the data list!"); return; }

            WebRequestManager.Character character = PlayerDataManager.Singleton.LocalPlayerData.character;

            if (previewObject)
            {
                if (previewObject.TryGetComponent(out PooledObject pooledObject))
                {
                    ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    previewObject = null;
                }
                else
                {
                    Destroy(previewObject);
                }
            }
            // Instantiate the player model
            if (PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.TryGetComponent(out PooledObject pO))
            {
                previewObject = ObjectPoolingManager.SpawnObject(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.GetComponent<PooledObject>(), previewCharacterPosition, Quaternion.Euler(previewCharacterRotation)).gameObject;
            }
            else
            {
                previewObject = Instantiate(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            }

            previewObject.GetComponent<AnimationHandler>().ChangeCharacter(character);
            previewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());
        }

        private string lastPlayersString;
        private void RefreshPlayerCards()
        {
            string playersString = PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId).ToString();
            playersString += PlayerDataManager.Singleton.GetGameMode().ToString();
            foreach (PlayerDataManager.PlayerData data in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
            {
                playersString += data.id.ToString() + data.team.ToString() + data.character._id.ToString() + lockedClients.Contains((ulong)data.id).ToString();
            }

            if (lastPlayersString == playersString) { return; }
            lastPlayersString = playersString;

            UpdateRichPresence();

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
            Dictionary<PlayerDataManager.Team, int> accountCardCounter = new Dictionary<PlayerDataManager.Team, int>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                if (teamParentDict.ContainsKey(playerData.team))
                {
                    if (!accountCardCounter.ContainsKey(playerData.team)) { accountCardCounter.Add(playerData.team, 0); }

                    Transform accountCardParent = teamParentDict[playerData.team];
                    if (PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams.Length == 1) { accountCardParent = accountCardCounter[playerData.team] >= 5 ? rightTeamParent.transformParent : teamParentDict[playerData.team]; }

                    AccountCard accountCard = Instantiate(playerAccountCardPrefab.gameObject, accountCardParent).GetComponent<AccountCard>();
                    accountCard.Initialize(playerData.id, lockedClients.Contains((ulong)playerData.id));
                    if (teamParentDict.Count > 1)
                    {
                        accountCard.SetChangeTeamLogic(teamParentDict.FirstOrDefault(item => item.Key != playerData.team).Key, accountCardParent == rightTeamParent.transformParent);
                    }
                    
                    if (playerData.id == (int)NetworkManager.LocalClientId)
                    {
                        leftTeamJoinInteractable = teamParentDict[playerData.team] != leftTeamParent.transformParent;
                        rightTeamJoinInteractable = teamParentDict[playerData.team] != rightTeamParent.transformParent;
                    }

                    accountCardCounter[playerData.team] += 1;
                }
                else
                {
                    Debug.LogWarning("Can't find parent for team " + playerData.team);
                }
            }

            leftTeamParent.joinTeamButton.interactable = leftTeamJoinInteractable & !lockedClients.Contains(NetworkManager.LocalClientId);
            rightTeamParent.joinTeamButton.interactable = rightTeamJoinInteractable & !lockedClients.Contains(NetworkManager.LocalClientId);
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (characterPreviewCamera) { Destroy(characterPreviewCamera.gameObject); }

            if (previewObject)
            {
                if (previewObject.TryGetComponent(out PooledObject pooledObject))
                {
                    if (pooledObject.IsSpawned)
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    }
                    previewObject = null;
                }
                else
                {
                    Destroy(previewObject);
                }
            }

            if (pauseInstance)
            {
                if (pauseInstance.TryGetComponent(out PauseMenu pauseMenu))
                {
                    pauseMenu.DestroyAllMenus();
                }
                else
                {
                    Destroy(pauseInstance);
                }
            }
        }

        public void OpenRoomSettings()
        {
            roomSettingsParent.SetActive(true);
            lobbyUIParent.SetActive(false);

            // Show game mode info UI
            if (PlayerDataManager.Singleton.IsLobbyLeader() & IsClient)
            {
                string gameModeString = PlayerDataManager.Singleton.GetGameMode().ToString();
                if (!FasterPlayerPrefs.Singleton.HasString(gameModeString))
                {
                    FasterPlayerPrefs.Singleton.SetString(gameModeString, "");
                    gameModeInfoUI.gameObject.SetActive(true);
                    gameModeInfoUI.Initialize(PlayerDataManager.Singleton.GetGameMode(), true);
                }
            }
        }

        public void CloseRoomSettings()
        {
            roomSettingsParent.SetActive(false);
            lobbyUIParent.SetActive(true);
        }

        public void OpenGameModeInfoUI()
        {
            gameModeInfoUI.gameObject.SetActive(true);
            gameModeInfoUI.Initialize(PlayerDataManager.Singleton.GetGameMode(), false);
        }

        public void AddBot(PlayerDataManager.Team team)
        {
            PlayerDataManager.Singleton.AddBotData(team, false);
        }

        private void ChooseLoadoutPreset(Button button, int loadoutSlotIndex)
        {
            foreach (Button b in loadoutPresetButtons)
            {
                b.interactable = button != b;
            }

            Dictionary<string, CharacterReference.WeaponOption> weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptionsDictionary();
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.LocalPlayerData;

            FixedString64Bytes activeLoadoutSlot = playerData.character.GetActiveLoadout().loadoutSlot.ToString();
            if ((loadoutSlotIndex + 1).ToString() != activeLoadoutSlot)
            {
                playerData.character = playerData.character.ChangeActiveLoadoutFromSlot(loadoutSlotIndex);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }

            WebRequestManager.Loadout loadout = playerData.character.GetLoadoutFromSlot(loadoutSlotIndex);

            CharacterReference.WeaponOption primaryOption;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon1ItemId.ToString(), out WebRequestManager.InventoryItem weapon1InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon1InventoryItem.itemId._id, out primaryOption))
                {
                    Debug.LogWarning("Can't find primary weapon inventory item in character reference");
                }
            }
            else
            {
                Debug.LogWarning("Can't find primary weapon inventory item");
                primaryOption = null;
            }

            CharacterReference.WeaponOption secondaryOption;
            if (WebRequestManager.TryGetInventoryItem(playerData.character._id.ToString(), loadout.weapon2ItemId.ToString(), out WebRequestManager.InventoryItem weapon2InventoryItem))
            {
                if (!weaponOptions.TryGetValue(weapon2InventoryItem.itemId._id, out secondaryOption))
                {
                    Debug.LogWarning("Can't find primary weapon inventory item in character reference");
                }
            }
            else
            {
                Debug.LogWarning("Can't find primary weapon inventory item");
                secondaryOption = null;
            }

            if (primaryOption == null) { primaryOption = WebRequestManager.GetDefaultPrimaryWeapon(); }
            if (secondaryOption == null) { secondaryOption = WebRequestManager.GetDefaultSecondaryWeapon(); }

            if (primaryOption != null)
            {
                primaryWeaponIcon.sprite = primaryOption.weaponIcon;
                primaryWeaponText.text = primaryOption.name;
            }

            if (secondaryOption != null)
            {
                secondaryWeaponIcon.sprite = secondaryOption.weaponIcon;
                secondaryWeaponText.text = secondaryOption.name;
            }
            
            if (previewObject)
            {
                previewObject.GetComponent<LoadoutManager>().ApplyLoadout(playerData.character.raceAndGender,
                    loadout, playerData.character._id.ToString());
            }
        }

        public void ChangeGameMode(PlayerDataManager.GameMode gameMode)
        {
            PlayerDataManager.Singleton.SetGameMode(gameMode);
        }

        public void ChangeMap(string map)
        {
            PlayerDataManager.Singleton.SetMap(map);
        }

        public void ChangeTeam(PlayerDataManager.Team team)
        {
            if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId))
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.LocalPlayerData;
                playerData.team = team;
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
        }

        private NetworkVariable<bool> startGameCalled = new NetworkVariable<bool>();
        private bool startGameServerRpcInProgress;

        public void StartGame()
        {
            if (IsServer)
            {
                startGameCalled.Value = true;
            }
            else
            {
                startGameServerRpcInProgress = true;
                StartGameServerRpc(NetworkManager.LocalClientId);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void StartGameServerRpc(ulong clientId)
        {
            StartGame();
            EndStartGameClientRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void EndStartGameClientRpc(RpcParams rpcParams)
        {
            startGameServerRpcInProgress = false;
        }

        public void LockCharacter()
        {
            if (IsClient)
            {
                LockCharacterServerRpc(NetworkManager.LocalClientId);
            }
            else
            {
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
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
            spectateButton.interactable = false;
            foreach (Button button in loadoutPresetButtons)
            {
                button.interactable = false;
            }

            lockCharacterButton.onClick.RemoveAllListeners();
            lockCharacterButton.onClick.AddListener(UnlockCharacter);
            lockCharacterButton.GetComponentInChildren<Text>().text = "UNLOCK";
        }

        private NetworkList<ulong> lockedClients;

        [Rpc(SendTo.Server, RequireOwnership = false)] private void LockCharacterServerRpc(ulong clientId) { lockedClients.Add(clientId); }

        private void UnlockCharacter()
        {
            if (lockedClients.Contains(NetworkManager.LocalClientId))
            {
                UnlockCharacterServerRpc(NetworkManager.LocalClientId);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void UnlockCharacterServerRpc(ulong clientId)
        {
            if (lockedClients.Contains(clientId))
            {
                lockedClients.Remove(clientId);
            }
        }

        private void UnlockCharacterLocal()
        {
            spectateButton.interactable = true;
            int activeLoadoutSlot = 0;
            for (int i = 0; i < loadoutPresetButtons.Length; i++)
            {
                Button button = loadoutPresetButtons[i];
                int var = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate { ChooseLoadoutPreset(button, var); });
                if (PlayerDataManager.Singleton.LocalPlayerData.character.IsSlotActive(i)) { activeLoadoutSlot = i; }
            }
            loadoutPresetButtons[activeLoadoutSlot].onClick.Invoke();

            lockCharacterButton.onClick.RemoveAllListeners();
            lockCharacterButton.onClick.AddListener(LockCharacter);
            lockCharacterButton.GetComponentInChildren<Text>().text = "LOCK";
        }

        private void OnLockedClientListChange(NetworkListEvent<ulong> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<ulong>.EventType.Add)
            {
                if (networkListEvent.Value == NetworkManager.LocalClientId) { LockCharacterLocal(); }
            }
            else if (networkListEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
            {
                if (networkListEvent.Value == NetworkManager.LocalClientId) { UnlockCharacterLocal(); }
            }
        }

        private void UpdateRichPresence()
        {
            DiscordManager.UpdateActivity("In Lobby (" + PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count + " of " + PlayerDataManager.Singleton.GetMaxPlayersForMap() + " Players)",
                        PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()) + " | " + PlayerDataManager.Singleton.GetMapName());
        }
    }
}