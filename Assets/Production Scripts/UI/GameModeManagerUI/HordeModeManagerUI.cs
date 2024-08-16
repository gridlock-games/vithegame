using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;

namespace Vi.UI
{
    public class HordeModeManagerUI : GameModeManagerUI
    {
        [SerializeField] private Text wavesLeftText;

        private new void Start()
        {
            base.Start();
            EvaluateWaveText();
        }

        private int lastRoundCount = -1;
        private bool lastDisplayNextGameActionStatus;
        protected new void Update()
        {
            base.Update();
            if (gameModeManager.ShouldDisplaySpecialNextGameActionMessage())
            {
                roundResultText.enabled = true;
                roundResultText.text = "Push Back The Corruption!";

                if (gameModeManager.ShouldDisplayNextGameActionTimer())
                {
                    roundResultText.text += " " + gameModeManager.GetNextGameActionTimerDisplayString();
                }
                else
                {
                    roundResultText.text = roundResultText.text.Trim();
                }
            }

            if (gameModeManager.GetRoundCount() != lastRoundCount | lastDisplayNextGameActionStatus != gameModeManager.ShouldDisplayNextGameAction())
            {
                EvaluateWaveText();
            }
            lastRoundCount = gameModeManager.GetRoundCount();
            lastDisplayNextGameActionStatus = gameModeManager.ShouldDisplayNextGameAction();
        }

        private void EvaluateWaveText()
        {
            roundWinThresholdText.text = "Waves Remaining: " + (gameModeManager.GetNumberOfRoundsWinsToWinGame() - gameModeManager.GetRoundCount()).ToString();
            if (gameModeManager.ShouldDisplayNextGameAction())
            {
                wavesLeftText.text = "Waves Completed: " + (gameModeManager.GetRoundCount() >= 0 ? gameModeManager.GetRoundCount() : 0).ToString();
            }
            else
            {
                wavesLeftText.text = "Waves Completed: " + (gameModeManager.GetRoundCount() - 1 >= 0 ? gameModeManager.GetRoundCount() - 1 : 0).ToString();
            }
        }
    }
}