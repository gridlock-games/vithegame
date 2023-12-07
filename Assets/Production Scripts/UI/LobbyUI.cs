using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using System.Linq;
using TMPro;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [SerializeField] private GameObject roomSettingsParent;
        [SerializeField] private GameObject lobbyUIParent;
        [Header("Lobby UI Assignments")]
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private Transform characterSelectGridParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterRoleText;
        [SerializeField] private Vector3 previewCharacterPosition;
        [SerializeField] private Vector3 previewCharacterRotation;
        [SerializeField] private Button lockCharacterButton;
        [SerializeField] private Text characterLockTimeText;
        [SerializeField] private AccountCard playerAccountCardPrefab;
        [SerializeField] private Transform upperLeftTeamParent;
        [SerializeField] private Transform upperRightTeamParent;
        [SerializeField] private Transform lowerLeftTeamParent;
        [SerializeField] private Transform lowerRightTeamParent;
        [SerializeField] private Text gameModeText;
        [SerializeField] private Text mapText;
        [Header("Room Settings Assignments")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown mapDropdown;

        private readonly float size = 200;
        private readonly int height = 2;

        private NetworkVariable<float> characterLockTimer = new NetworkVariable<float>(60);
        private NetworkVariable<float> startGameTimer = new NetworkVariable<float>(5);

        private void Awake()
        {
            CloseRoomSettings();

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

            //mapDropdown.ClearOptions();
            //List<TMP_Dropdown.OptionData> mapOptions = new List<TMP_Dropdown.OptionData>();
            //NetSceneManager.Singleton.GetScenePayloadsOfType();
            //List<PlayerDataManager.map> mapList = new List<PlayerDataManager.map>();
            //foreach (PlayerDataManager.map map in System.Enum.GetValues(typeof(PlayerDataManager.map)))
            //{
            //    if (map == PlayerDataManager.map.None) { continue; }
            //    mapList.Add(map);
            //    mapOptions.Add(new TMP_Dropdown.OptionData(FromCamelCase(map.ToString())));
            //}
            //mapDropdown.AddOptions(mapOptions);
            //if (!mapList.Contains(PlayerDataManager.Singleton.Getmap())) { mapDropdown.value = 0; }

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

            if (IsClient) { StartCoroutine(WaitForPlayerDataToUpdatePreview()); }
        }

        public override void OnNetworkDespawn()
        {
            characterLockTimer.OnValueChanged -= OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged -= OnStartGameTimerChange;
        }

        private IEnumerator WaitForPlayerDataToUpdatePreview()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId));
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(NetworkManager.LocalClientId);
            UpdateCharacterPreview(playerData.characterIndex, playerData.skinIndex);
        }

        private void OnCharacterLockTimerChange(float prev, float current)
        {
            if (prev > 0 & current <= 0)
            {
                LockCharacter();
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CloseRoomSettings(); }

            List<ulong> entireClientList = new List<ulong>();
            var playerDataList = PlayerDataManager.Singleton.GetPlayerDataList();
            foreach (var playerData in playerDataList)
            {
                if (playerData.id >= 0) { entireClientList.Add((ulong)playerData.id); }
            }
            lockedCharacters.Sort();
            entireClientList.Sort();
            bool startingGame = lockedCharacters.SequenceEqual(entireClientList);

            if (IsServer)
            {
                if (playerDataList.Count > 0 & playerDataList.Count % 2 == 0)
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
            characterLockTimeText.text = startingGame ? startGameTimer.Value.ToString("F0") : characterLockTimer.Value.ToString("F0");

            Dictionary<PlayerDataManager.Team, Transform> teamParentDict = new Dictionary<PlayerDataManager.Team, Transform>();
            PlayerDataManager.Team[] possibleTeams = PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams;
            for (int i = 0; i < possibleTeams.Length; i++)
            {
                if (i == 0)
                    teamParentDict.Add(possibleTeams[i], upperLeftTeamParent);
                else if (i == 1)
                    teamParentDict.Add(possibleTeams[i], upperRightTeamParent);
                else if (i == 2)
                    teamParentDict.Add(possibleTeams[i], lowerLeftTeamParent);
                else if (i == 3)
                    teamParentDict.Add(possibleTeams[i], lowerRightTeamParent);
                else
                    Debug.LogError("Not sure where to parent team " + possibleTeams[i]);
            }

            foreach (Transform parent in teamParentDict.Values)
            {
                foreach (Transform child in parent)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataList())
            {
                AccountCard accountCard = Instantiate(playerAccountCardPrefab.gameObject, teamParentDict[playerData.team]).GetComponent<AccountCard>();
                accountCard.Initialize(playerData.id);
            }
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

        public void ChangeGameMode()
        {
            PlayerDataManager.Singleton.SetGameMode(System.Enum.Parse<PlayerDataManager.GameMode>(gameModeDropdown.options[gameModeDropdown.value].text.Replace(" ", "")));
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
                        lockedCharacters.Add((ulong)playerData.id);
                        LockCharacterClientRpc((ulong)playerData.id);
                    }
                }
            }
        }

        private void LockCharacterLocal()
        {
            lockCharacterButton.interactable = false;
            foreach (Transform child in characterSelectGridParent)
            {
                if (child.TryGetComponent(out CharacterSelectElement characterSelectElement))
                {
                    characterSelectElement.SetButtonInteractability(false);
                }
            }
        }

        private List<ulong> lockedCharacters = new List<ulong>();

        [ServerRpc(RequireOwnership = false)]
        private void LockCharacterServerRpc(ulong clientId)
        {
            lockedCharacters.Add(clientId);
            LockCharacterClientRpc(clientId);
        }

        [ClientRpc]
        private void LockCharacterClientRpc(ulong clientId)
        {
            if (!IsServer) { lockedCharacters.Add(clientId); }
            if (clientId == NetworkManager.LocalClientId) { LockCharacterLocal(); }
        }
    }
}