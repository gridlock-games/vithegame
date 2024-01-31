using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class AccountCard : MonoBehaviour
    {
        [SerializeField] private Text nameDisplayText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image lockedUIImage;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        private int playerDataId;

        public void Initialize(int playerDataId, bool isLocked)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            nameDisplayText.text = playerData.character.name.ToString();
            backgroundImage.color = PlayerDataManager.GetTeamColor(playerData.team);

            lockedUIImage.sprite = isLocked | playerDataId < 0 ? lockedSprite : unlockedSprite;
        }
    }
}