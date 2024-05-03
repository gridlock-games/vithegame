using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class TeamEliminationUI : GameModeManagerUI
    {
        [SerializeField] private Image viLogoImage;
        [SerializeField] private Sprite viEssenceIcon;

        private const float colorTransitionSpeed = 8;

        private TeamEliminationManager teamEliminationManager;
        private Sprite originalViLogoSprite;
        private new void Start()
        {
            base.Start();
            teamEliminationManager = gameModeManager.GetComponent<TeamEliminationManager>();
            originalViLogoSprite = viLogoImage.sprite;
        }

        private new void Update()
        {
            base.Update();

            if (teamEliminationManager.IsViEssenceSpawned())
            {
                viLogoImage.sprite = viEssenceIcon;
            }
            else
            {
                viLogoImage.sprite = originalViLogoSprite;
                //viEssenceImage.color = Color.Lerp(viEssenceImage.color, , Time.deltaTime * colorTransitionSpeed);
            }
        }
    }
}