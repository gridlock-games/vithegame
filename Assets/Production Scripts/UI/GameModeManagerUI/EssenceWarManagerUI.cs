using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class EssenceWarManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text gameEndText;
        [SerializeField] protected Text roundResultText;
        [SerializeField] protected Text roundWinThresholdText;

        protected EssenceWarManager essenceWarManager;

        protected void Start()
        {
            essenceWarManager = GetComponentInParent<EssenceWarManager>();

            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";

            roundWinThresholdText.text = "Rounds To Win Game: " + essenceWarManager.GetNumberOfRoundsWinsToWinGame().ToString();
        }

        protected void Update()
        {
            if (!essenceWarManager.IsSpawned) { return; }

            roundTimerText.text = essenceWarManager.GetRoundTimerDisplayString();
            roundTimerText.color = essenceWarManager.IsInOvertime() ? Color.red : Color.white;
            leftScoreText.text = essenceWarManager.GetLeftScoreString();
            rightScoreText.text = essenceWarManager.GetRightScoreString();

            roundResultText.enabled = essenceWarManager.ShouldDisplayNextGameAction();
            roundResultText.text = essenceWarManager.AreAllPlayersConnected() ? essenceWarManager.GetRoundResultMessage() + essenceWarManager.GetNextGameActionTimerDisplayString() : "WAITING FOR PLAYERS";

            gameEndText.text = essenceWarManager.GetGameEndMessage();
        }
    }
}