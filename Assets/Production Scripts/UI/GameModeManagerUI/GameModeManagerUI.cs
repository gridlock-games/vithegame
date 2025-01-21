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
        [SerializeField] private Text rewardsHeaderText;
        [SerializeField] private Text viEssenceEarnedText;
        [SerializeField] private AnimationCurve rewardsAppearanceCurve;
        [SerializeField] private UIParticleSystem sparkleEffect;
        [SerializeField] private UIParticleSystem[] rewardsParticleSystems;
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

            transitionGroup.alpha = 0;
            topImage.enabled = false;
            bottomImage.enabled = false;

            ResetMVPUIElements();
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

        private const float transitionSpeed = 3;
        private const float transitionPeakLimit = 540;
        private const float transitionValleyLimit = 1280;

        [Header("Transition")]
        [SerializeField] private CanvasGroup transitionGroup;
        [SerializeField] private AnimationCurve transitionInAlphaCurve;
        [SerializeField] private AnimationCurve transitionOutAlphaCurve;
        [SerializeField] private Image topImage;
        [SerializeField] private Image bottomImage;
        private bool transitionRunning;
        private bool transitionPeakReached;
        private IEnumerator PlayTransition()
        {
            transitionRunning = true;

            topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, transitionValleyLimit);
            bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -transitionValleyLimit);

            topImage.enabled = true;
            bottomImage.enabled = true;

            float t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * transitionSpeed;
                t = Mathf.Clamp01(t);

                transitionGroup.alpha = transitionInAlphaCurve.EvaluateNormalizedTime(t);
                topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, Mathf.Lerp(transitionValleyLimit, transitionPeakLimit, t));
                bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -Mathf.Lerp(transitionValleyLimit, transitionPeakLimit, t));
                yield return null;
            }

            transitionPeakReached = true;
            yield return new WaitForSeconds(0.05f);
            yield return null;
            transitionPeakReached = false;

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * transitionSpeed;
                t = Mathf.Clamp01(t);

                transitionGroup.alpha = transitionOutAlphaCurve.EvaluateNormalizedTime(t);
                topImage.rectTransform.offsetMin = new Vector2(topImage.rectTransform.offsetMin.x, Mathf.Lerp(transitionPeakLimit, transitionValleyLimit, t));
                bottomImage.rectTransform.offsetMax = new Vector2(bottomImage.rectTransform.offsetMax.x, -Mathf.Lerp(transitionPeakLimit, transitionValleyLimit, t));
                yield return null;
            }

            topImage.enabled = false;
            bottomImage.enabled = false;

            transitionRunning = false;
        }

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
                    if (gameModeManager.GetPostGameStatus() != lastPostGameStatus & !transitionRunning)
                    {
                        StartCoroutine(PlayTransition());
                    }

                    if (transitionPeakReached)
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

                    gameResultText.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(1.1f, 1, 1), Mathf.PingPong(Time.time, 1));
                    rewardsHeaderText.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(1.1f, 1, 1), Mathf.PingPong(Time.time, 1));

                    viEssenceEarnedText.text = gameModeManager.TokensEarnedFromMatch.ToString();
                    if (gameModeManager.TokensEarnedFromMatch > 0)
                    {
                        viEssenceEarnedText.text += "x";
                    }

                    if (!displayRewardsHasBeenRun & !transitionRunning)
                    {
                        displayRewardsCoroutine = StartCoroutine(DisplayRewards());
                    }
                    break;
                case GameModeManager.PostGameStatus.MVP:
                    if (displayRewardsCoroutine != null) { StopCoroutine(displayRewardsCoroutine); }

                    if (gameModeManager.GetPostGameStatus() != lastPostGameStatus)
                    {
                        if (!transitionRunning) { StartCoroutine(PlayTransition()); }
                    }

                    if (transitionPeakReached)
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
                case GameModeManager.PostGameStatus.Scoreboard:
                    if (displayRewardsCoroutine != null) { StopCoroutine(displayRewardsCoroutine); }

                    MVPCanvasGroup.alpha = Mathf.MoveTowards(MVPCanvasGroup.alpha, 0, Time.deltaTime * opacityTransitionSpeed);
                    MVPCanvas.enabled = MVPCanvasGroup.alpha == 0;

                    if (actionMapHandler)
                    {
                        actionMapHandler.OpenScoreboard();
                    }

                    if (Mathf.Approximately(MVPCanvasGroup.alpha, 0))
                    {
                        RemoveCharPreview();
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

            expGainedMessage.text = "+" + gameModeManager.ExpEarnedFromMatch.ToString() + " XP";

            float t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime;
                t = Mathf.Clamp01(t);
                expGainedParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, experienceAppearanceCurve.EvaluateNormalizedTime(t));
                yield return null;
            }

            // TODO change this to be calculated based on exp to next level and current exp earned
            float maxExpFillAmount = 0.7f;
            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime;
                t = Mathf.Clamp01(t);
                expGainedBar.fillAmount = Mathf.LerpUnclamped(0, maxExpFillAmount, t);
                expMessageParent.transform.localScale = expMessageCurve.EvaluateNormalized(t);
                yield return null;
            }

            yield return new WaitForSeconds(transitionWaitTime);

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * rewardsTransitionSpeed;
                t = Mathf.Clamp01(t);
                expGainedParent.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, t);
                yield return null;
            }

            t = 0;
            bool psPlayed = false;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime;
                t = Mathf.Clamp01(t);

                if (!psPlayed)
                {
                    if (t >= 0.7f)
                    {
                        psPlayed = true;
                        if (gameModeManager.TokensEarnedFromMatch > 0)
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
                t += Time.deltaTime * rewardsTransitionSpeed;
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

            foreach (UIParticleSystem ps in rewardsParticleSystems)
            {
                ps.PlayWorldPoint(killsParticleLocation.position);
            }

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * scaleTransitionSpeed;
                t = Mathf.Clamp01(t);
                deathsParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                yield return null;
            }

            foreach (UIParticleSystem ps in rewardsParticleSystems)
            {
                ps.PlayWorldPoint(deathsParticleLocation.position);
            }

            t = 0;
            while (!Mathf.Approximately(t, 1))
            {
                t += Time.deltaTime * scaleTransitionSpeed;
                t = Mathf.Clamp01(t);
                assistsParent.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleLerpTimeCurve.Evaluate(t));
                yield return null;
            }

            foreach (UIParticleSystem ps in rewardsParticleSystems)
            {
                ps.PlayWorldPoint(assistsParticleLocation.position);
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

        private const float rewardsTransitionSpeed = 3;
        private const float opacityTransitionSpeed = 0.5f;
        private const float scaleTransitionSpeed = 2;

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

            WebRequestManager.Character character = PlayerDataManager.Singleton.GetPlayerData(playerScoreToPreview.id).character;
            var playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(character.raceAndGender);

            RemoveCharPreview();

            // Instantiate the player model
            Vector3 basePos = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetCharacterPreviewPosition(playerScoreToPreview.id);
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

            yield return new WaitUntil(() => !transitionPeakReached);

            string stateName = "MVP";
            if (gameModeManager.GetPostGameStatus() != GameModeManager.PostGameStatus.MVP)
            {
                yield return new WaitUntil(() => gameModeManager.GetGameWinnerIds().Count > 0);
                stateName = gameModeManager.GetGameWinnerIds().Contains(PlayerDataManager.Singleton.LocalPlayerData.id) ? "Victory" : "Defeat";
            }
            animationHandler.Animator.CrossFadeInFixedTime(stateName, 0.15f, animationHandler.Animator.GetLayerIndex("Actions"));

            MVPPreviewInProgress = false;
        }
    }
}