using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class EssenceWarManagerUI : GameModeManagerUI
    {
        [SerializeField] private Image viLogoImage;
        [SerializeField] private Sprite viEssenceIcon;

        private EssenceWarManager essenceWarManager;
        private Sprite originalViLogoSprite;
        protected override void Start()
        {
            base.Start();
            essenceWarManager = gameModeManager.GetComponent<EssenceWarManager>();
            originalViLogoSprite = viLogoImage.sprite;

            leftScoreTeamColorImage.enabled = true;
            leftScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(essenceWarManager.GetLeftScoreTeam());
            rightScoreTeamColorImage.enabled = true;
            rightScoreTeamColorImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(essenceWarManager.GetRightScoreTeam());
        }

        private const float colorTransitionSpeed = 2;
        private const float colorTransitionThreshold = 0.001f;
        private readonly static Color onColor = new Color(1, 1, 1, 1);
        private readonly static Color offColor = new Color(1, 1, 1, 0);

        protected override void Update()
        {
            base.Update();
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