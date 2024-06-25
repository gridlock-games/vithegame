using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Vi.Utility;

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

        protected GameModeManager gameModeManager;
        protected void Start()
        {
            RefreshStatus();
            gameModeManager = GetComponentInParent<GameModeManager>();
            gameModeManager.SubscribeScoreListCallback(delegate { OnScoreListChanged(); });
            
            roundResultText.enabled = false;

            roundWinThresholdText.text = "Rounds To Win Game: " + gameModeManager.GetNumberOfRoundsWinsToWinGame().ToString();

            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();

            leftScoreTeamColorImage.enabled = false;
            rightScoreTeamColorImage.enabled = false;
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
                    MVPCanvasGroup.alpha = Mathf.MoveTowards(MVPCanvasGroup.alpha, 1, Time.deltaTime * opacityTransitionSpeed);
                    MVPAccountCard.Initialize(gameModeManager.GetMVPScore().id, true);
                    break;
                case GameModeManager.PostGameStatus.Scoreboard:
                    MVPCanvasGroup.alpha = Mathf.MoveTowards(MVPCanvasGroup.alpha, 0, Time.deltaTime * opacityTransitionSpeed);
                    break;
                default:
                    Debug.LogError("Unsure how to handle post game status " + gameModeManager.GetPostGameStatus());
                    break;
            }
        }

        private const float opacityTransitionSpeed = 2;
    }
}