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

            public void SetActive(bool isActive)
            {
                teamTitleText.gameObject.SetActive(isActive);
                transformParent.gameObject.SetActive(isActive);
                addBotButton.gameObject.SetActive(isActive);
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

            if (IsClient) { StartCoroutine(WaitForPlayerDataToUpdatePreview()); }
        }

        public override void OnNetworkDespawn()
        {
            characterLockTimer.OnValueChanged -= OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged -= OnStartGameTimerChange;
            lockedClients.OnListChanged -= OnLockedClientListChange;
        }

        private IEnumerator WaitForPlayerDataToUpdatePreview()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId);
            UpdateCharacterPreview(playerData.characterIndex, playerData.skinIndex);
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
                    NetSceneManager.Singleton.LoadScene("Free For All");
                }
            }
        }

        private PlayerDataManager.GameMode lastGameMode;
        private Dictionary<PlayerDataManager.Team, Transform> teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CloseRoomSettings(); }

            // Timer logic
            List<PlayerDataManager.PlayerData> playerDataList = PlayerDataManager.Singleton.GetPlayerDataList();
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

            bool canCountDown = playerDataList.Count > 0 & playerDataList.Count % 2 == 0;
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
            characterLockTimeText.text = startingGame & canCountDown ? "Starting game in " + startGameTimer.Value.ToString("F0") : "Locking Characters in " + characterLockTimer.Value.ToString("F0");

            roomSettingsButton.gameObject.SetActive(PlayerDataManager.Singleton.IsLobbyLeader() & !(startingGame & canCountDown));
            if (!roomSettingsButton.gameObject.activeSelf) { CloseRoomSettings(); }

            foreach (Transform child in leftTeamParent.transformParent)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in rightTeamParent.transformParent)
            {
                Destroy(child.gameObject);
            }

            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataList())
            {
                if (teamParentDict.ContainsKey(playerData.team))
                {
                    AccountCard accountCard = Instantiate(playerAccountCardPrefab.gameObject, teamParentDict[playerData.team]).GetComponent<AccountCard>();
                    accountCard.Initialize(playerData.id);
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
                for (int i = 0; i < possibleTeams.Length; i++)
                {
                    if (i == 0)
                    {
                        leftTeamParent.teamTitleText.text = PlayerDataManager.GetTeamText(possibleTeams[i]);
                        teamParentDict.Add(possibleTeams[i], leftTeamParent.transformParent);
                        PlayerDataManager.Team teamValue = possibleTeams[i];
                        leftTeamParent.addBotButton.onClick.RemoveAllListeners();
                        leftTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
                    }
                    else if (i == 1)
                    {
                        rightTeamParent.teamTitleText.text = PlayerDataManager.GetTeamText(possibleTeams[i]);
                        teamParentDict.Add(possibleTeams[i], rightTeamParent.transformParent);
                        PlayerDataManager.Team teamValue = possibleTeams[i];
                        leftTeamParent.addBotButton.onClick.RemoveAllListeners();
                        leftTeamParent.addBotButton.onClick.AddListener(delegate { AddBot(teamValue); });
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
            }

            gameModeText.text = FromCamelCase(PlayerDataManager.Singleton.GetGameMode().ToString());
            mapText.text = PlayerDataManager.Singleton.GetMapName();

            lastGameMode = PlayerDataManager.Singleton.GetGameMode();
        }

        private GameObject previewObject;
        public void UpdateCharacterPreview(int characterIndex, int skinIndex)
        {
            if (previewObject) { Destroy(previewObject); }

            CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            previewObject = Instantiate(playerModelOption.playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
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
            int characterIndex = 0;
            int skinIndex = 0;
            PlayerDataManager.Singleton.AddBotData(characterIndex, skinIndex, team);
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
                foreach (var playerData in PlayerDataManager.Singleton.GetPlayerDataList())
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