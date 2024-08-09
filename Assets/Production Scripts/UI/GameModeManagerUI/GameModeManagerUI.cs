using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Vi.Utility;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.UI
{
    public class GameModeManagerUI : MonoBehaviour
    {
        [Header("Base UI")]
        [SerializeField] protected Image leftScoreTeamColorImage;
        [SerializeField] protected Image rightScoreTeamColorImage;
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;
        [SerializeField] private CanvasGroup[] canvasGroupsToAffectOpacity;

        [Header("MVP Presentation")]
        [SerializeField] private CanvasGroup MVPCanvasGroup;
        [SerializeField] private AccountCard MVPAccountCard;
        [SerializeField] private Camera MVPPresentationCamera;
        [SerializeField] private Text MVPKillsText;
        [SerializeField] private Text MVPDeathsText;
        [SerializeField] private Text MVPAssistsText;

        protected GameModeManager gameModeManager;
        protected void Start()
        {
            RefreshStatus();
            gameModeManager = GetComponentInParent<GameModeManager>();
            OnScoreListChanged();
            gameModeManager.SubscribeScoreListCallback(delegate { OnScoreListChanged(); });
            
            roundResultText.enabled = false;

            roundWinThresholdText.text = "Rounds To Win Game: " + gameModeManager.GetNumberOfRoundsWinsToWinGame().ToString();

            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();

            leftScoreTeamColorImage.enabled = false;
            rightScoreTeamColorImage.enabled = false;

            MVPPresentationCamera.enabled = false;
            StartCoroutine(SetCameraOrientation());
        }

        private IEnumerator SetCameraOrientation()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.HasPlayerSpawnPoints());
            MVPPresentationCamera.transform.position = PlayerDataManager.Singleton.GetPlayerSpawnPoints().previewCharacterPosition + PlayerSpawnPoints.cameraPreviewCharacterPositionOffset;
            MVPPresentationCamera.transform.rotation = Quaternion.Euler(PlayerSpawnPoints.cameraPreviewCharacterRotation);
        }

        private void OnDestroy()
        {
            gameModeManager.UnsubscribeScoreListCallback(delegate { OnScoreListChanged(); });
        }

        protected void OnScoreListChanged()
        {
            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();
        }

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroupsToAffectOpacity)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
        }

        protected void Update()
        {
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { OnScoreListChanged(); }

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!gameModeManager.IsSpawned) { return; }

            roundTimerText.text = gameModeManager.GetRoundTimerDisplayString();
            roundTimerText.color = gameModeManager.IsInOvertime() ? Color.red : Color.white;

            if (gameModeManager.IsWaitingForPlayers)
            {
                roundResultText.enabled = true;
                roundResultText.text = "WAITING FOR PLAYERS";
            }
            else if (gameModeManager.ShouldDisplaySpecialNextGameActionMessage())
            {
                roundResultText.enabled = false;
                roundResultText.text = string.Empty;
            }
            else
            {
                roundResultText.enabled = gameModeManager.ShouldDisplayNextGameAction();
                roundResultText.text = gameModeManager.GetRoundResultMessage();

                if (gameModeManager.ShouldDisplayNextGameActionTimer())
                {
                    roundResultText.text += gameModeManager.GetNextGameActionTimerDisplayString();
                }
                else
                {
                    roundResultText.text = roundResultText.text.Trim();
                }
            }

            gameEndText.text = gameModeManager.GetGameEndMessage();

            switch (gameModeManager.GetPostGameStatus())
            {
                case GameModeManager.PostGameStatus.None:
                    MVPCanvasGroup.alpha = 0;
                    break;
                case GameModeManager.PostGameStatus.MVP:
                    if (!MVPPreviewObject) { StartCoroutine(CreateMVPPreview()); }
                    MVPCanvasGroup.alpha = Mathf.MoveTowards(MVPCanvasGroup.alpha, 1, Time.deltaTime * opacityTransitionSpeed);
                    MVPAccountCard.Initialize(gameModeManager.GetMVPScore().id, true);
                    break;
                case GameModeManager.PostGameStatus.Scoreboard:
                    MVPCanvasGroup.alpha = Mathf.MoveTowards(MVPCanvasGroup.alpha, 0, Time.deltaTime * opacityTransitionSpeed);
                    if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.Singleton.LocalClientId))
                    {
                        PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.LocalPlayerData;
                        if (playerData.team == PlayerDataManager.Team.Spectator)
                        {
                            PlayerDataManager.Singleton.GetLocalSpectatorObject().Value.GetComponent<ActionMapHandler>().OpenScoreboard();
                        }
                        else
                        {
                            PlayerDataManager.Singleton.GetLocalPlayerObject().Value.GetComponent<ActionMapHandler>().OpenScoreboard();
                        }
                    }
                    break;
                default:
                    Debug.LogError("Unsure how to handle post game status " + gameModeManager.GetPostGameStatus());
                    break;
            }
        }

        private const float opacityTransitionSpeed = 2;

        private IEnumerator CreateMVPPreview()
        {
            MVPKillsText.text = "";
            MVPDeathsText.text = "";
            MVPAssistsText.text = "";

            yield return new WaitUntil(() => gameModeManager.GetMVPScore().isValid);

            MVPKillsText.text = gameModeManager.GetMVPScore().cumulativeKills.ToString();
            MVPDeathsText.text = gameModeManager.GetMVPScore().cumulativeDeaths.ToString();
            MVPAssistsText.text = gameModeManager.GetMVPScore().cumulativeAssists.ToString();

            WebRequestManager.Character character = PlayerDataManager.Singleton.GetPlayerData(gameModeManager.GetMVPScore().id).character;

            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            KeyValuePair<int, int> kvp = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptionIndices(character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            if (MVPPreviewObject) { Destroy(MVPPreviewObject); }
            // Instantiate the player model
            MVPPreviewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab,
                PlayerDataManager.Singleton.GetPlayerSpawnPoints().previewCharacterPosition + PlayerSpawnPoints.previewCharacterPositionOffset,
                Quaternion.Euler(PlayerSpawnPoints.previewCharacterRotation));

            AnimationHandler animationHandler = MVPPreviewObject.GetComponent<AnimationHandler>();
            animationHandler.ChangeCharacter(character);
            MVPPreviewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());

            MVPPresentationCamera.enabled = true;

            yield return new WaitUntil(() => animationHandler.Animator);

            animationHandler.Animator.CrossFadeInFixedTime("MVP", 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));
        }

        private GameObject MVPPreviewObject;
    }
}