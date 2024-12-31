using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;
using Vi.Core.Structures;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Player;

namespace Vi.UI
{
    public class HordeModeManagerUI : GameModeManagerUI
    {
        [Header("Horde Mode Specific")]
        [SerializeField] private Text wavesLeftText;
        [SerializeField] private OnScreenHittableAgentHealthBar structureHealthBar;

        private HordeModeManager hordeModeManager;

        private new void Start()
        {
            base.Start();
            hordeModeManager = GameModeManager.Singleton.GetComponent<HordeModeManager>();
            EvaluateWavesText();
        }

        protected override void UpdateDiscordRichPresence()
        {
            string scoreString = null;
            if (hordeModeManager) { scoreString = "Waves Completed: " + hordeModeManager.GetWavesCompleted(); }
            DiscordManager.UpdateActivity("In " + PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()), scoreString);
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

            FindStructure();

            // Display essence menu
            if (lastEssenceUIState != hordeModeManager.ShouldDisplayEssenceUI)
            {
                if (actionMapHandler)
                {
                    if (!actionMapHandler.GetComponent<Spectator>())
                    {
                        if (hordeModeManager.ShouldDisplayEssenceUI)
                        {
                            essenceBuffMenu.Initialize(actionMapHandler);
                        }
                        else
                        {
                            essenceBuffMenu.CloseMenu();
                        }
                    }
                }
            }

            lastEssenceUIState = hordeModeManager.ShouldDisplayEssenceUI;
        }

        [SerializeField] private EssenceBuffMenu essenceBuffMenu;
        private bool lastEssenceUIState;

        private void EvaluateWavesText()
        {
            roundWinThresholdText.text = "Waves Remaining: " + (gameModeManager.GetNumberOfRoundsWinsToWinGame() - gameModeManager.GetRoundCount()).ToString();
            wavesLeftText.text = "Waves Completed: " + hordeModeManager.GetWavesCompleted().ToString();

            lastRoundCount = gameModeManager.GetRoundCount();
            lastWavesCompleted = hordeModeManager.GetWavesCompleted();
        }

        private void FindStructure()
        {
            if (structureHealthBar.HittableAgent) { return; }
            Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
            if (structures.Length > 0)
            {
                structureHealthBar.Initialize(structures[0]);
            }
        }
    }
}