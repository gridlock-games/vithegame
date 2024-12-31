using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Unity.Netcode;
using Vi.Core.Structures;

namespace Vi.UI
{
    public class EssenceWarUI : GameModeManagerUI
    {
        [Header("Essence War UI")]
        [SerializeField] private Image viLogoImage;
        [SerializeField] private Sprite viEssenceIcon;

        [SerializeField] private OnScreenHittableAgentHealthBar leftStructureHealthBar;
        [SerializeField] private OnScreenHittableAgentHealthBar rightStructureHealthBar;

        private EssenceWarManager essenceWarManager;
        private Sprite originalViLogoSprite;
        protected override void Start()
        {
            base.Start();
            essenceWarManager = gameModeManager.GetComponent<EssenceWarManager>();
            originalViLogoSprite = viLogoImage.sprite;

            leftScoreTeamColorImage.enabled = true;
            rightScoreTeamColorImage.enabled = true;
            RefreshTeamColors();
            FindStructures();
        }

        private void RefreshTeamColors()
        {
            leftScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(essenceWarManager.GetLeftScoreTeam());
            rightScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(essenceWarManager.GetRightScoreTeam());
        }

        private void FindStructures()
        {
            PlayerDataManager.Team leftTeam = essenceWarManager.GetLeftScoreTeam();
            PlayerDataManager.Team rightTeam = essenceWarManager.GetRightScoreTeam();

            Structure[] structures = PlayerDataManager.Singleton.GetActiveStructures();
            int lightIndex = System.Array.FindIndex(structures, item => item.GetTeam() == PlayerDataManager.Team.Light);
            if (lightIndex != -1)
            {
                if (leftTeam == PlayerDataManager.Team.Light)
                {
                    leftStructureHealthBar.Initialize(structures[lightIndex]);
                }
                else if (rightTeam == PlayerDataManager.Team.Light)
                {
                    rightStructureHealthBar.Initialize(structures[lightIndex]);
                }
            }

            int corruptionIndex = System.Array.FindIndex(structures, item => item.GetTeam() == PlayerDataManager.Team.Corruption);
            if (corruptionIndex != -1)
            {
                if (leftTeam == PlayerDataManager.Team.Corruption)
                {
                    leftStructureHealthBar.Initialize(structures[corruptionIndex]);
                }
                else if (rightTeam == PlayerDataManager.Team.Corruption)
                {
                    rightStructureHealthBar.Initialize(structures[corruptionIndex]);
                }
            }
        }

        private const float colorTransitionSpeed = 2;
        private const float colorTransitionThreshold = 0.001f;
        private readonly static Color onColor = new Color(1, 1, 1, 1);
        private readonly static Color offColor = new Color(1, 1, 1, 0);

        protected override void Update()
        {
            base.Update();

            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame)
            {
                RefreshTeamColors();
                FindStructures();
            }
            else if (PlayerDataManager.Singleton.StructuresListWasUpdatedThisFrame)
            {
                FindStructures();
            }

            if (gameModeManager.ShouldDisplaySpecialNextGameActionMessage())
            {
                if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.Singleton.LocalClientId))
                {
                    roundResultText.enabled = true;
                    PlayerDataManager.Team team = PlayerDataManager.Singleton.LocalPlayerData.team;
                    if (team == PlayerDataManager.Team.Spectator)
                        roundResultText.text = "Fight!";
                    else
                        roundResultText.text = "Fight for " + PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.LocalPlayerData.team) + "'s Glory!";
                }

                if (gameModeManager.ShouldDisplayNextGameActionTimer())
                {
                    roundResultText.text += " " + gameModeManager.GetNextGameActionTimerDisplayString();
                }
                else
                {
                    roundResultText.text = roundResultText.text.Trim();
                }
            }

            if (essenceWarManager.IsViEssenceSpawned())
            {
                if (viLogoImage.sprite == viEssenceIcon)
                {
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, onColor, Time.deltaTime * colorTransitionSpeed);
                }
                else // Image is vi logo
                {
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, offColor, Time.deltaTime * colorTransitionSpeed);
                    if (Vector4.Distance(viLogoImage.color, offColor) < colorTransitionThreshold)
                    {
                        viLogoImage.sprite = viEssenceIcon;
                    }
                }
            }
            else // Vi Essence isn't spawned
            {
                if (viLogoImage.sprite == viEssenceIcon)
                {
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, offColor, Time.deltaTime * colorTransitionSpeed);
                    if (Vector4.Distance(viLogoImage.color, offColor) < colorTransitionThreshold)
                    {
                        viLogoImage.sprite = originalViLogoSprite;
                    }
                }
                else // Image is vi logoo
                {
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, onColor, Time.deltaTime * colorTransitionSpeed);
                }
            }
        }
    }
}