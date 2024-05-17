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
            viLogoImage.sprite = teamEliminationManager.IsViEssenceSpawned() ? viEssenceIcon : originalViLogoSprite;
        }
    }
}