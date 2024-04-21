using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class FreeForAllManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;
        [SerializeField] protected Text killsToWinRoundThresholdText;

        protected FreeForAllManager freeForAllManager;

        protected void Start()
        {
            freeForAllManager = GetComponentInParent<FreeForAllManager>();

            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";

            roundWinThresholdText.text = "Rounds To Win Game: " + freeForAllManager.GetNumberOfRoundsWinsToWinGame().ToString();
            killsToWinRoundThresholdText.text = "Kills To Win Round: " + freeForAllManager.GetKillsToWinRound();
        }

        protected void Update()
        {
            if (!freeForAllManager.IsSpawned) { return; }

            roundTimerText.text = freeForAllManager.GetRoundTimerDisplayString();
            roundTimerText.color = freeForAllManager.IsInOvertime() ? Color.red : Color.white;
            leftScoreText.text = freeForAllManager.GetLeftScoreString();
            rightScoreText.text = freeForAllManager.GetRightScoreString();

            roundResultText.enabled = freeForAllManager.ShouldDisplayNextGameAction();
            roundResultText.text = freeForAllManager.AreAllPlayersConnected() ? freeForAllManager.GetRoundResultMessage() + freeForAllManager.GetNextGameActionTimerDisplayString() : "WAITING FOR PLAYERS";

            gameEndText.text = freeForAllManager.GetGameEndMessage();
        }
    }
}