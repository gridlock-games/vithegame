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
        }

        private const float colorTransitionSpeed = 2;
        private const float colorTransitionThreshold = 0.1f;
        private readonly static Color onColor = new Color(1, 1, 1, 1);
        private readonly static Color offColor = new Color(1, 1, 1, 0);

        private new void Update()
        {
            base.Update();

            if (gameModeManager.ShouldDisplayRoundStartMessage())
            {
                if (PlayerDataManager.Singleton.ContainsId((int)NetworkManager.Singleton.LocalClientId))
                {
                    roundResultText.enabled = true;
                    roundResultText.text = "Fight for " + PlayerDataManager.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(NetworkManager.Singleton.LocalClientId).team) + "'s Glory!";
                }
            }

            if (teamEliminationManager.IsViEssenceSpawned())
            {
                viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, offColor, Time.deltaTime * colorTransitionSpeed);
                if (1 - viLogoImage.color.a < colorTransitionThreshold)
                {
                    viLogoImage.sprite = viEssenceIcon;
                }

                // If the logo is on the vi essence icon
                if (viLogoImage.sprite == viEssenceIcon)
                {
                    // Transition to On Color
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, onColor, Time.deltaTime * colorTransitionSpeed);
                }
                else // Vi logo image has sprite of vi logo
                {
                    // Transition to the off color
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, offColor, Time.deltaTime * colorTransitionSpeed);
                    // Once the alpha is less than the transition threshold, change to the vi essence icon
                    if (viLogoImage.color.a < colorTransitionThreshold) { viLogoImage.sprite = viEssenceIcon; }
                }
            }
            else // Vi Essence isn't spawned
            {
                // If the logo is on the vi essence icon
                if (viLogoImage.sprite == viEssenceIcon)
                {
                    // Transition to the off color
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, offColor, Time.deltaTime * colorTransitionSpeed);
                    // Once the alpha is less than the transition threshold, change to the original vi logo
                    if (viLogoImage.color.a < colorTransitionThreshold) { viLogoImage.sprite = originalViLogoSprite; }
                }
                else // Vi logo image has sprite of vi logo
                {
                    // Transition to On Color
                    viLogoImage.color = Vector4.MoveTowards(viLogoImage.color, onColor, Time.deltaTime * colorTransitionSpeed);
                }
            }
        }
    }
}