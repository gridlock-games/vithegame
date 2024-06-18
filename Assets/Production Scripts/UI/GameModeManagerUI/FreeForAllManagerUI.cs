using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class FreeForAllManagerUI : GameModeManagerUI
    {
        [SerializeField] private Text killsToWinRoundThresholdText;

        private new void Start()
        {
            base.Start();
            killsToWinRoundThresholdText.text = "Kills To Win Round: " + gameModeManager.GetComponent<FreeForAllManager>().GetKillsToWinRound();
        }

        private new void Update()
        {
            base.Update();

            if (gameModeManager.ShouldDisplaySpecialNextGameActionMessage())
            {
                roundResultText.enabled = true;
                roundResultText.text = "Fight for Glory!";

                if (gameModeManager.ShouldDisplayNextGameActionTimer())
                {
                    roundResultText.text += " " + gameModeManager.GetNextGameActionTimerDisplayString();
                }
                else
                {
                    roundResultText.text = roundResultText.text.Trim();
                }
            }
        }
    }
}