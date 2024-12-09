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
        [SerializeField] private GameObject structureHealthBarParent;
        [SerializeField] private Image structureHPImage;
        [SerializeField] private Image structureIntermHPImage;
        [Header("Structure Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

        private HordeModeManager hordeModeManager;
        private Structure structure;

        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        private new void Start()
        {
            base.Start();
            hordeModeManager = GameModeManager.Singleton.GetComponent<HordeModeManager>();
            EvaluateWavesText();
            structureHealthBarParent.SetActive(false);

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }
        }

        protected override void UpdateDiscordRichPresence()
        {
            string scoreString = null;
            if (hordeModeManager) { scoreString = "Waves Completed: " + hordeModeManager.GetWavesCompleted(); }
            DiscordManager.UpdateActivity("In " + PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()), scoreString);
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

            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame | PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { FindStructure(); }

            if (structure)
            {
                float HP = structure.GetHP();
                if (HP < 0.1f & HP > 0) { HP = 0.1f; }

                float maxHP = structure.GetMaxHP();

                if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
                {
                    structureHPImage.fillAmount = HP / maxHP;
                }

                lastHP = HP;
                lastMaxHP = maxHP;

                structureIntermHPImage.fillAmount = Mathf.Lerp(structureIntermHPImage.fillAmount, HP / maxHP, Time.deltaTime * PlayerCard.fillSpeed);

                if (structure.StatusAgent.ActiveStatusesWasUpdatedThisFrame)
                {
                    List<ActionClip.Status> activeStatuses = structure.StatusAgent.GetActiveStatuses();
                    foreach (StatusIcon statusIcon in statusIcons)
                    {
                        if (activeStatuses.Contains(statusIcon.Status))
                        {
                            statusIcon.SetActive(true);
                            statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                        }
                        else
                        {
                            statusIcon.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                FindStructure();
            }

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
            Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
            if (structures.Length > 0)
            {
                structure = structures[0];
                structureHealthBarParent.SetActive(true);
                structureHPImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(structure.GetTeam());

                float HP = structure.GetHP();
                if (HP < 0.1f & HP > 0) { HP = 0.1f; }
                float maxHP = structure.GetMaxHP();
                structureHPImage.fillAmount = HP / maxHP;
                structureIntermHPImage.fillAmount = HP / maxHP;

                List<ActionClip.Status> activeStatuses = structure.StatusAgent.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }
            }
        }
    }
}