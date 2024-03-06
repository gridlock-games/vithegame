using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class TeamDeathmatchManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;
        [SerializeField] protected Text killsToWinRoundThresholdText;

        protected TeamDeathmatchManager teamDeathmatchManager;

        protected void Start()
        {
            teamDeathmatchManager = GetComponentInParent<TeamDeathmatchManager>();

            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";

            roundWinThresholdText.text = "Rounds To Win Game: " + teamDeathmatchManager.GetNumberOfRoundsWinsToWinGame().ToString();
            killsToWinRoundThresholdText.text = "Kills To Win Round: " + teamDeathmatchManager.GetKillsToWinRound();
        }

        protected void Update()
        {
            if (!teamDeathmatchManager.IsSpawned) { return; }

            roundTimerText.text = teamDeathmatchManager.GetRoundTimerDisplayString();
            roundTimerText.color = teamDeathmatchManager.IsInOvertime() ? Color.red : Color.white;
            leftScoreText.text = teamDeathmatchManager.GetLeftScoreString();
            rightScoreText.text = teamDeathmatchManager.GetRightScoreString();

            roundResultText.enabled = teamDeathmatchManager.ShouldDisplayNextGameAction();
            roundResultText.text = teamDeathmatchManager.AreAllPlayersConnected() ? teamDeathmatchManager.GetRoundResultMessage() + teamDeathmatchManager.GetNextGameActionTimerDisplayString() : "WAITING FOR PLAYERS";

            gameEndText.text = teamDeathmatchManager.GetGameEndMessage();
        }
    }
}