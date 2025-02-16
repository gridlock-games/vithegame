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
using UnityEngine.Rendering.Universal;

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
        [SerializeField] private Button returnToHubButton;

        [Header("Experience")]
        [SerializeField] private AnimationCurve experienceAppearanceCurve;
        [SerializeField] private RectTransform expGainedParent;
        [SerializeField] private Image expGainedBar;
        [SerializeField] private RectTransform expMessageParent;
        [SerializeField] private Text expGainedMessage;
        [SerializeField] private Weapon.Vector3AnimationCurve expMessageCurve;
        [SerializeField] private Text gameResultText;

        [Header("Rewards")]
        [SerializeField] private RectTransform rewardsSectionParent;
        [SerializeField] private Text levelText;
        [SerializeField] private Text rewardsHeaderText;
        [SerializeField] private Text viEssenceEarnedText;
        [SerializeField] private AnimationCurve rewardsAppearanceCurve;
        [SerializeField] private UIParticleSystem sparkleEffect;
        [SerializeField] private Image viEssenceRewardsImage;

        [Header("MVP Presentation")]
        [SerializeField] private Canvas MVPCanvas;
        [SerializeField] private CanvasGroup MVPCanvasGroup;
        [SerializeField] private AccountCard MVPAccountCard;
        [SerializeField] private Camera MVPPresentationCamera;
        [SerializeField] private Image[] MVPHeaderImages;
        [SerializeField] private Text MVPHeaderText;
        [SerializeField] private RectTransform killsParent;
        [SerializeField] private RectTransform killsParticleLocation;
        [SerializeField] private Text MVPKillsText;
        [SerializeField] private RectTransform deathsParent;
        [SerializeField] private RectTransform deathsParticleLocation;
        [SerializeField] private Text MVPDeathsText;
        [SerializeField] private RectTransform assistsParent;
        [SerializeField] private RectTransform assistsParticleLocation;
        [SerializeField] private Text MVPAssistsText;
        [SerializeField] private Light previewLightPrefab;
        [SerializeField] private AnimationCurve scaleLerpTimeCurve;

        protected GameModeManager gameModeManager;
        protected virtual void Start()
        {
            RefreshStatus();
            gameModeManager = GetComponentInParent<GameModeManager>();
            OnScoreListChanged();
            gameModeManager.onScoreListChanged += OnScoreListChanged;
            
            roundResultText.enabled = false;

            roundWinThresholdText.text = "Rounds To Win Game: " + gameModeManager.GetNumberOfRoundsWinsToWinGame().ToString();

            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();

            leftScoreTeamColorImage.enabled = false;
            rightScoreTeamColorImage.enabled = false;

            MVPPresentationCamera.enabled = false;

            MVPCanvas.enabled = false;

            foreach (Image MVPHeaderImage in MVPHeaderImages)
            {
                MVPHeaderImage.color = StringUtility.SetColorAlpha(MVPHeaderImage.color, 0);
            }
            MVPHeaderText.color = StringUtility.SetColorAlpha(MVPHeaderText.color, 0);

            ResetMVPUIElements();

            returnToHubButton.onClick.AddListener(() => ReturnToHub());
        }

        private void ResetMVPUIElements()
        {
            MVPAccountCard.transform.localScale = Vector3.zero;

            killsParent.localScale = Vector3.zero;
            assistsParent.localScale = Vector3.zero;
            deathsParent.localScale = Vector3.zero;

            expGainedParent.localScale = Vector3.zero;
            expGainedBar.fillAmount = 0;
            expMessageParent.localScale = Vector3.zero;

            rewardsSectionParent.localScale = Vector3.zero;
        }

        private void OnDestroy()
        {
            gameModeManager.onScoreListChanged -= OnScoreListChanged;
            if (lightInstance) { Destroy(lightInstance); }
            if (MVPPreviewObject)
            {
                if (MVPPreviewObject.TryGetComponent(out PooledObject pooledObject))
                {
                    if (pooledObject.IsSpawned)
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    }
                    MVPPreviewObject = null;
                }
                else
                {
                    Destroy(MVPPreviewObject);
                }
            }
        }

        protected void OnScoreListChanged()
        {
            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();

            UpdateDiscordRichPresence();
        }

        protected virtual void UpdateDiscordRichPresence()
        {
            if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
            {
                string scoreString = null;
                if (!string.IsNullOrWhiteSpace(leftScoreText.text) & !string.IsNullOrWhiteSpace(rightScoreText.text))
                {
                    scoreString = leftScoreText.text + " | " + rightScoreText.text;
                }
                else if (!string.IsNullOrWhiteSpace(leftScoreText.text))
                {
                    scoreString = leftScoreText.text;
                }
                else if (!string.IsNullOrWhiteSpace(rightScoreText.text))
                {
                    scoreString = rightScoreText.text;
                }
                DiscordManager.UpdateActivity("In " + PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()), scoreString);
            }
        }

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroupsToAffectOpacity)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
        }

        protected ActionMapHandler actionMapHandler;
        private void FindLocalActionMapHandler()
        {
            if (actionMapHandler)
            {
                if (actionMapHandler.gameObject.activeInHierarchy)
                {
                    return;
                }
            }

            if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.Singleton.LocalClientId))
            {
                if (PlayerDataManager.Singleton.LocalPlayerData.team == PlayerDataManager.Team.Spectator)
                {
                    var spectator = PlayerDataManager.Singleton.GetLocalSpectatorObject().Value;
                    if (spectator)
                    {
                        actionMapHandler = spectator.GetComponent<ActionMapHandler>();
                    }
                }
                else
                {
                    var player = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
                    if (player)
                    {
                        actionMapHandler = player.GetComponent<ActionMapHandler>();
                    }
                }
            }
        }

        [SerializeField] private TransitionController transitionController;

        private const float textPingPongSpeed = 1.5f;

        protected virtual void Update()
        {
            FindLocalActionMapHandler();

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
            else if (gameModeManager.GetPostGameStatus() == GameModeManager.PostGameStatus.Rewards
                & PlayerDataManager.Singleton.LocalPlayerData.team == PlayerDataManager.Team.Spectator)
            {
                roundResultText.enabled = true;
                roundResultText.text = "Displaying Player Rewards";
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
                    MVPCanvas.enabled = false;
                    MVPCanvasGroup.alpha = 0;
                    break;
                case GameModeManager.PostGameStatus.Rewards:
                    if (PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        if (gameModeManager.GetPostGameStatus() != lastPostGameStatus & !transitionController.TransitionRunning)
                        {
                            StartCoroutine(transitionController.PlayTransition());
                        }

                        if (transitionController.TransitionPeakReached)
                        {
                            MVPCanvas.enabled = true;
                            MVPCanvasGroup.alpha = 1;
                        }

                        if (PlayerDataManager.Singleton.LocalPlayerData.id != (int)NetworkManager.ServerClientId)
                        {
                            GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetPlayerScore(PlayerDataManager.Singleton.LocalPlayerData.id);
                            if (playerScore.isValid)
                            {
                                if (!MVPPreviewObject & !MVPPreviewInProgress) { StartCoroutine(CreateMVPPreview(playerScore)); }
                                MVPCanvas.enabled = true;
                                MVPAccountCard.InitializeAsMVPScore(playerScore.id);
                            }
                        }

                        gameResultText.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(1.1f, 1, 1), Mathf.PingPong(Time.time * textPingPongSpeed, 1));
                        rewardsHeaderText.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(1.1f, 1, 1), Mathf.PingPong(Time.time * textPingPongSpeed, 1));

                        viEssenceEarnedText.text = gameModeManager.ViEssenceEarnedFromMatch.ToString();
                        if (gameModeManager.ViEssenceEarnedFromMatch > 0)
                        {
                            viEssenceEarnedText.text += "x";
                        }

                        if (!displayRewardsHasBeenRun & !transitionController.TransitionRunning)
                        {
                            displayRewardsCoroutine = StartCoroutine(DisplayRewards());
                        }
                    }
                    break;
                case GameModeManager.PostGameStatus.MVP:
                    Cursor.lockState = CursorLockMode.None;
                    if (displayRewardsCoroutine != null) { StopCoroutine(displayRewardsCoroutine); }

                    if (gameModeManager.GetPostGameStatus() != lastPostGameStatus)
                    {
                        if (!transitionController.TransitionRunning) { StartCoroutine(transitionController.PlayTransition()); }
                    }

                    if (transitionController.TransitionPeakReached)
                    {
                        RemoveCharPreview();
                        ResetMVPUIElements();

                        MVPCanvas.enabled = true;
                        MVPCanvasGroup.alpha = 1;

                        foreach (Image MVPHeaderImage in MVPHeaderImages)
                        {
                            MVPHeaderImage.color = StringUtility.SetColorAlpha(MVPHeaderImage.color, 1);
                        }
                        MVPHeaderText.color = StringUtility.SetColorAlpha(MVPHeaderText.color, 1);

                        if (!MVPPreviewObject & !MVPPreviewInProgress)
                        {
                            GameModeManager.PlayerScore MVPScore = gameModeManager.GetMVPScore();
                            if (MVPScore.isValid)
                            {
                                StartCoroutine(CreateMVPPreview(MVPScore));
                                MVPAccountCard.InitializeAsMVPScore(MVPScore.id);

                                if (!displayKDARunning) { StartCoroutine(DisplayKDA(true)); }
                            }
                        }
                    }
                    break;
                default:
                    Debug.LogWarning("Unsure how to handle post game status " + gameModeManager.GetPostGameStatus());
                    break;
            }

            if (gameModeManager.GetPostGameStatus() != lastPostGameStatus)
            {
                charPreviewRemovedThisStatus = false;
            }

            lastPostGameStatus = gameModeManager.GetPostGameStatus();
        }

        public void OpenScoreboard()
        {
            if (actionMapHandler)
            {
                actionMapHandler.OpenScoreboard();
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

            if (WebRequestManager.Singleton.ServerManager.HubServers.Length > 0)
            {
                yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData(WebRequestManager.Singleton.ServerManager.HubServers[0].ip, ushort.Parse(WebRequestManager.Singleton.ServerManager.HubServers[0].port), FasterPlayerPrefs.serverListenAddress);
                NetworkManager.Singleton.StartClient();
            }
        }

        private GameModeManager.PostGameStatus lastPostGameStatus = GameModeManager.PostGameStatus.None;

        private bool charPreviewRemovedThisStatus;
        private void RemoveCharPreview()
        {
            if (charPreviewRemovedThisStatus) { return; }
            charPreviewRemovedThisStatus = true;

            if (lightInstance) { Destroy(lightInstance); }
            if (MVPPreviewObject)
            {
                if (MVPPreviewObject.TryGetComponent(out PooledObject pooledObject))
                {
                    if (pooledObject.IsSpawned)
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    }
                    MVPPreviewObject = null;
                }
                else
                {
                    Destroy(MVPPreviewObject);
                }
            }
        }

        private void OnLevelUp()
        {

        }

        private bool displayRewardsHasBeenRun;
        private Coroutine displayRewardsCoroutine;
        private IEnumerator DisplayRewards()
        {
            displayRewardsHasBeenRun = true;
            gameResultText.text = "";

            yield return new WaitUntil(() => Mathf.Approximately(MVPCanvasGroup.alpha, 1));
            yield return new WaitUntil(() => !Mathf.Approximately(gameModeManager.ExpEarnedFromMatch, -1));
            yield return new WaitUntil(() => gameModeManager.GetGameWinnerIds().Count > 0);

            gameResultText.text = gameModeManager.GetGameWinnerIds().Contains(PlayerDataManager.Singleton.LocalPlayerData.id) ? "VICTORY!" : "DEFEAT!";

            WebRequestManager.Singleton.CharacterManager.TryGetCharacterStats(PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(), out CharacterManager.CharacterStats stats);
            levelText.text = "Lv " + stats.level.ToString();
            expGainedMessage.text = "+" + gameModeManager.ExpEarnedFromMatch.ToString("F0") + " XP";

            // Calculated based on exp to next level and current exp earned
            float minExpFillAmount = Mathf.InverseLerp(0, stats.expToNextLv, stats.currentExp);
            float maxExpFillAmount = Mathf.InverseLerp(0, stats.expToNextLv, stats.currentExp + gameModeManager.ExpEarnedFromMatch);
            expGainedBar.fillAmount = Mathf.LerpUnclamped(minExpFillAmount, maxExpFillAmount, 0);

            float t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * UIAnimationTimeMultiplier;
                t = Mathf.Clamp01(t);
                expGainedParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, experienceAppearanceCurve.EvaluateNormalizedTime(t));
                yield return null;
            }

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * UIAnimationTimeMultiplier;
                t = Mathf.Clamp01(t);
                expGainedBar.fillAmount = Mathf.LerpUnclamped(minExpFillAmount, maxExpFillAmount, t);
                expMessageParent.transform.localScale = expMessageCurve.EvaluateNormalized(t);
                yield return null;
            }

            if (stats.currentExp + gameModeManager.ExpEarnedFromMatch >= stats.expToNextLv)
            {
                levelText.text = "Lv " + (stats.level+1).ToString();
                OnLevelUp();
            }

            yield return new WaitForSeconds(transitionWaitTime);

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * rewardsTransitionOutSpeed;
                t = Mathf.Clamp01(t);
                expGainedParent.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, t);
                yield return null;
            }

            t = 0;
            bool psPlayed = false;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * UIAnimationTimeMultiplier;
                t = Mathf.Clamp01(t);

                if (!psPlayed)
                {
                    if (t >= 0.7f)
                    {
                        psPlayed = true;
                        if (gameModeManager.ViEssenceEarnedFromMatch > 0)
                        {
                            sparkleEffect.ps.Play();
                            sparkleEffect.transform.position = viEssenceRewardsImage.rectTransform.position;
                        }
                    }
                }
                
                rewardsSectionParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, rewardsAppearanceCurve.EvaluateNormalizedTime(t));
                yield return null;
            }

            yield return new WaitForSeconds(transitionWaitTime);

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * rewardsTransitionOutSpeed;
                t = Mathf.Clamp01(t);
                rewardsSectionParent.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, t);
                yield return null;
            }

            yield return DisplayKDA(false);
        }

        private const float transitionWaitTime = 2;

        private bool displayKDARunning;
        private IEnumerator DisplayKDA(bool showAccountCard)
        {
            displayKDARunning = true;

            yield return new WaitUntil(() => !MVPPreviewInProgress);

            float t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * scaleTransitionSpeed;
                t = Mathf.Clamp01(t);
                killsParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                yield return null;
            }

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * scaleTransitionSpeed;
                t = Mathf.Clamp01(t);
                deathsParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                yield return null;
            }

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * scaleTransitionSpeed;
                t = Mathf.Clamp01(t);
                assistsParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                yield return null;
            }

            if (showAccountCard)
            {
                t = 0;
                while (!Mathf.Approximately(t, 1))
                {
                    t += Time.deltaTime * scaleTransitionSpeed;
                    t = Mathf.Clamp01(t);
                    MVPAccountCard.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                    yield return null;
                }
            }
            
            displayKDARunning = false;
        }

        private const float UIAnimationTimeMultiplier = 1.85f;
        private const float rewardsTransitionOutSpeed = 3.6f;
        private const float scaleTransitionSpeed = 2.85f;

        private GameObject MVPPreviewObject;
        private GameObject lightInstance;
        private bool MVPPreviewInProgress;
        private IEnumerator CreateMVPPreview(GameModeManager.PlayerScore playerScoreToPreview)
        {
            MVPPreviewInProgress = true;

            MVPKillsText.text = "";
            MVPDeathsText.text = "";
            MVPAssistsText.text = "";

            MVPKillsText.text = playerScoreToPreview.cumulativeKills.ToString();
            MVPDeathsText.text = playerScoreToPreview.cumulativeDeaths.ToString();
            MVPAssistsText.text = playerScoreToPreview.cumulativeAssists.ToString();

            CharacterManager.Character character = PlayerDataManager.Singleton.GetPlayerData(playerScoreToPreview.id).character;
            CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(character.raceAndGender);

            RemoveCharPreview();

            // Instantiate the player model
            Vector3 basePos = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetCharacterPreviewPosition();
            if (PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.TryGetComponent(out PooledObject pooledPrefab))
            {
                MVPPreviewObject = ObjectPoolingManager.SpawnObject(pooledPrefab,
                    basePos,
                    Quaternion.Euler(SpawnPoints.previewCharacterRotation)).gameObject;
            }
            else
            {
                MVPPreviewObject = Instantiate(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab,
                    basePos,
                    Quaternion.Euler(SpawnPoints.previewCharacterRotation));
            }

            AnimationHandler animationHandler = MVPPreviewObject.GetComponent<AnimationHandler>();
            animationHandler.ChangeCharacter(character);
            MVPPreviewObject.GetComponent<LoadoutManager>().ApplyLoadout(character.raceAndGender, character.GetActiveLoadout(), character._id.ToString());

            MVPPresentationCamera.transform.position = basePos + SpawnPoints.cameraPreviewCharacterPositionOffset;
            MVPPresentationCamera.transform.rotation = Quaternion.Euler(SpawnPoints.cameraPreviewCharacterRotation);

            yield return new WaitUntil(() => animationHandler.Animator);

            lightInstance = Instantiate(previewLightPrefab.gameObject);
            lightInstance.transform.SetParent(MVPPreviewObject.transform, true);
            lightInstance.transform.localPosition = new Vector3(0, 3, 4);
            lightInstance.transform.localEulerAngles = new Vector3(30, 180, 0);
            
            if (MVPPresentationCamera.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
            }
            MVPPresentationCamera.enabled = true;

            yield return new WaitUntil(() => !transitionController.TransitionPeakReached);

            string stateName = "MVP";
            if (gameModeManager.GetPostGameStatus() != GameModeManager.PostGameStatus.MVP)
            {
                yield return new WaitUntil(() => gameModeManager.GetGameWinnerIds().Count > 0);
                stateName = gameModeManager.GetGameWinnerIds().Contains(PlayerDataManager.Singleton.LocalPlayerData.id) ? "Victory" : "Defeat";
            }
            animationHandler.Animator.CrossFadeInFixedTime(stateName, 0.25f, animationHandler.Animator.GetLayerIndex("Actions"));

            MVPPreviewInProgress = false;
        }
    }
}