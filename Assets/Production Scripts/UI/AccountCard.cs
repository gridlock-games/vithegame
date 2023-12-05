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

        private int playerDataId;

        public void Initialize(int playerDataId)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            nameDisplayText.text = playerData.playerName.ToString();
            backgroundImage.color = PlayerDataManager.GetTeamColor(playerData.team);
        }
    }
}