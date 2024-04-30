using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

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
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }
            gameModeManager = GetComponent<GameModeManager>();

            roundResultText.enabled = false;

            roundWinThresholdText.text = "Rounds To Win Game: " + gameModeManager.GetNumberOfRoundsWinsToWinGame().ToString();
        }

        protected void Update()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }

            if (!gameModeManager.IsSpawned) { return; }

            roundTimerText.text = gameModeManager.GetRoundTimerDisplayString();
            roundTimerText.color = gameModeManager.IsInOvertime() ? Color.red : Color.white;
            leftScoreText.text = gameModeManager.GetLeftScoreString();
            rightScoreText.text = gameModeManager.GetRightScoreString();

            roundResultText.enabled = gameModeManager.ShouldDisplayNextGameAction();
            roundResultText.text = gameModeManager.AreAllPlayersConnected() ? gameModeManager.GetRoundResultMessage() + gameModeManager.GetNextGameActionTimerDisplayString() : "WAITING FOR PLAYERS";

            gameEndText.text = gameModeManager.GetGameEndMessage();
        }
    }
}