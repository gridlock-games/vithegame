using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class HordeModeManagerUI : GameModeManagerUI
    {
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
        }
    }
}