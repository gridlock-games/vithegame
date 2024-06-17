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
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;

        protected GameModeManager gameModeManager;

        private CanvasGroup[] canvasGroups;
        protected void Start()
        {
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            RefreshStatus();
            gameModeManager = GetComponentInParent<GameModeManager>();
            gameModeManager.SubscribeScoreListCallback(delegate { OnScoreListChanged(); });
            
            roundResultText.enabled = false;

            roundWinThresholdText.text = "Rounds To Win Game: " + gameModeManager.GetNumberOfRoundsWinsToWinGame().ToString();

            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();
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
            foreach (CanvasGroup canvasGroup in canvasGroups)
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
        }
    }
}