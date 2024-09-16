using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Vi.Core;
using Unity.Netcode;

namespace Vi.UI
{
    public class TeamEliminationUI : GameModeManagerUI
    {
        [SerializeField] private Image viLogoImage;
        [SerializeField] private Sprite viEssenceIcon;

        private TeamEliminationManager teamEliminationManager;
        private Sprite originalViLogoSprite;
        private new void Start()
        {
            base.Start();
            teamEliminationManager = gameModeManager.GetComponent<TeamEliminationManager>();
            originalViLogoSprite = viLogoImage.sprite;

            leftScoreTeamColorImage.enabled = true;
            leftScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(teamEliminationManager.GetLeftScoreTeam());
            rightScoreTeamColorImage.enabled = true;
            rightScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(teamEliminationManager.GetRightScoreTeam());
        }

        private const float colorTransitionSpeed = 2;
        private const float colorTransitionThreshold = 0.001f;
        private readonly static Color onColor = new Color(1, 1, 1, 1);
        private readonly static Color offColor = new Color(1, 1, 1, 0);

        private new void Update()
        {
            base.Update();

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

            if (teamEliminationManager.IsViEssenceSpawned())
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