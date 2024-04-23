using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class TeamEliminationManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;

        protected TeamEliminationManager teamEliminationManager;

        private CanvasGroup[] canvasGroups;

        protected void Start()
        {
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }

            teamEliminationManager = GetComponentInParent<TeamEliminationManager>();

            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";

            roundWinThresholdText.text = "Rounds To Win Game: " + teamEliminationManager.GetNumberOfRoundsWinsToWinGame().ToString();
        }

        protected void Update()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }

            if (!teamEliminationManager.IsSpawned) { return; }

            roundTimerText.text = teamEliminationManager.GetRoundTimerDisplayString();
            roundTimerText.color = teamEliminationManager.IsInOvertime() ? Color.red : Color.white;
            leftScoreText.text = teamEliminationManager.GetLeftScoreString();
            rightScoreText.text = teamEliminationManager.GetRightScoreString();

            roundResultText.enabled = teamEliminationManager.ShouldDisplayNextGameAction();
            roundResultText.text = teamEliminationManager.AreAllPlayersConnected() ? teamEliminationManager.GetRoundResultMessage() + teamEliminationManager.GetNextGameActionTimerDisplayString() : "WAITING FOR PLAYERS";

            gameEndText.text = teamEliminationManager.GetGameEndMessage();
        }
    }
}