using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Vi.Core;

namespace Vi.UI
{
    public class FreeForAllUI : GameModeManagerUI
    {
        [Header("Free For All UI")]
        [SerializeField] private Text killsToWinRoundThresholdText;

        private new void Start()
        {
            base.Start();
            killsToWinRoundThresholdText.text = "KOs To Win Round: " + gameModeManager.GetComponent<FreeForAllManager>().GetKillsToWinRound();
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