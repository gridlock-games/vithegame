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
    }
}