using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using System.Linq;

namespace Vi.UI
{
    public class LobbyUI : NetworkBehaviour
    {
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

        private readonly float size = 200;
        private readonly int height = 2;

        private NetworkVariable<float> characterLockTimer = new NetworkVariable<float>(60);
        private NetworkVariable<float> startGameTimer = new NetworkVariable<float>(5);

        private void Awake()
        {
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

        public override void OnNetworkSpawn()
        {
            characterLockTimer.OnValueChanged += OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged += OnStartGameTimerChange;
        }

        public override void OnNetworkDespawn()
        {
            characterLockTimer.OnValueChanged -= OnCharacterLockTimerChange;
            startGameTimer.OnValueChanged -= OnStartGameTimerChange;
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

        private void Start()
        {
            UpdateCharacterPreview(0, 0);
        }

        private void Update()
        {
            List<ulong> entireClientList = new List<ulong>();
            foreach (var playerData in PlayerDataManager.Singleton.GetPlayerDataList())
            {
                if (playerData.id >= 0) { entireClientList.Add((ulong)playerData.id); }
            }
            bool startingGame = lockedCharacters.SequenceEqual(entireClientList);

            if (IsServer)
            {
                if (PlayerDataManager.Singleton.GetPlayerDataList().Count > 0)
                {
                    if (startingGame) { startGameTimer.Value = Mathf.Clamp(startGameTimer.Value - Time.deltaTime, 0, Mathf.Infinity); }
                    else { characterLockTimer.Value = Mathf.Clamp(characterLockTimer.Value - Time.deltaTime, 0, Mathf.Infinity); }
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
            //if (previewObject) { Destroy(previewObject); }

            //CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            //previewObject = Instantiate(playerModelOption.playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            //previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            //characterNameText.text = playerModelOption.name;
            //characterRoleText.text = playerModelOption.role;
        }

        public void LockCharacter()
        {
            if (IsServer)
            {
                //LockCharacterClientRpc(NetworkManager.LocalClientId);
            }
            else
            {
                LockCharacterServerRpc(NetworkManager.LocalClientId);
            }
            //LockCharacterLocal();
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