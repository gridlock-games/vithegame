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

        private HordeModeManager hordeModeManager;

        private new void Start()
        {
            base.Start();
            hordeModeManager = GameModeManager.Singleton.GetComponent<HordeModeManager>();
            EvaluateWavesText();
        }

        private int lastRoundCount = -1;
        private int lastWavesCompleted = -1;
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

            if (gameModeManager.GetRoundCount() != lastRoundCount | lastWavesCompleted != hordeModeManager.GetWavesCompleted())
            {
                EvaluateWavesText();
            }
        }

        private void EvaluateWavesText()
        {
            roundWinThresholdText.text = "Waves Remaining: " + (gameModeManager.GetNumberOfRoundsWinsToWinGame() - gameModeManager.GetRoundCount()).ToString();
            wavesLeftText.text = "Waves Completed: " + hordeModeManager.GetWavesCompleted().ToString();

            lastRoundCount = gameModeManager.GetRoundCount();
            lastWavesCompleted = hordeModeManager.GetWavesCompleted();
        }
    }
}