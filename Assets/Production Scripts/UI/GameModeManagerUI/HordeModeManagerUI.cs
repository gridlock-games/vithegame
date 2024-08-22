using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;
using Vi.Core.Structures;
using Vi.Core;

namespace Vi.UI
{
    public class HordeModeManagerUI : GameModeManagerUI
    {
        [Header("Horde Mode Specific")]
        [SerializeField] private Text wavesLeftText;
        [SerializeField] private GameObject structureHealthBarParent;
        [SerializeField] private Text structureHPText;
        [SerializeField] private Image structureHPImage;
        [SerializeField] private Image structureIntermHPImage;

        private HordeModeManager hordeModeManager;
        private Structure structure;

        private new void Start()
        {
            base.Start();
            hordeModeManager = GameModeManager.Singleton.GetComponent<HordeModeManager>();
            EvaluateWavesText();
            structureHealthBarParent.SetActive(false);
        }

        private int lastRoundCount = -1;
        private int lastWavesCompleted = -1;

        private float lastHP = -1;
        private float lastMaxHP = -1;
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

            if (structure)
            {
                float HP = structure.GetHP();
                if (HP < 0.1f & HP > 0) { HP = 0.1f; }

                float maxHP = structure.GetMaxHP();

                if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
                {
                    structureHPText.text = structure.GetName() + " HP " + (HP < 10 & HP > 0 ? HP.ToString("F1") : HP.ToString("F0")) + " / " + maxHP.ToString("F0");
                    structureHPImage.fillAmount = HP / maxHP;
                }

                lastHP = HP;
                lastMaxHP = maxHP;

                structureIntermHPImage.fillAmount = Mathf.Lerp(structureIntermHPImage.fillAmount, HP / maxHP, Time.deltaTime * PlayerCard.fillSpeed);
            }
            else
            {
                FindStructure();
            }
        }

        private void EvaluateWavesText()
        {
            roundWinThresholdText.text = "Waves Remaining: " + (gameModeManager.GetNumberOfRoundsWinsToWinGame() - gameModeManager.GetRoundCount()).ToString();
            wavesLeftText.text = "Waves Completed: " + hordeModeManager.GetWavesCompleted().ToString();

            lastRoundCount = gameModeManager.GetRoundCount();
            lastWavesCompleted = hordeModeManager.GetWavesCompleted();
        }

        private void FindStructure()
        {
            Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
            if (structures.Length > 0)
            {
                structure = structures[0];
                structureHealthBarParent.SetActive(true);
                structureHPImage.color = PlayerDataManager.GetTeamColor(structure.GetTeam());
            }
        }
    }
}