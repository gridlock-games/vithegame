using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class AccountCard : MonoBehaviour
    {
        [SerializeField] private Text nameDisplayText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image lobbyLeaderImage;
        [SerializeField] private Button kickButton;
        [SerializeField] private Image lockedUIImage;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        private int playerDataId;
        private bool initialized;
        public void Initialize(int playerDataId, bool isLocked)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            nameDisplayText.text = playerData.character.name.ToString();
            backgroundImage.color = playerData.team == PlayerDataManager.Team.Competitor ? Color.white : PlayerDataManager.GetTeamColor(playerData.team);

            lockedUIImage.sprite = isLocked | playerDataId < 0 ? lockedSprite : unlockedSprite;

            lobbyLeaderImage.gameObject.SetActive(PlayerDataManager.Singleton.GetLobbyLeader().Value.id == playerDataId);

            kickButton.gameObject.SetActive(!lobbyLeaderImage.gameObject.activeSelf & PlayerDataManager.Singleton.IsLobbyLeader() & !GameModeManager.Singleton);

            initialized = true;

            nameDisplayText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name.ToString();
        }

        private void Update()
        {
            if (initialized)
            {
                if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame | PlayerDataManager.Singleton.TeamNameOverridesUpdatedThisFrame)
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
                    nameDisplayText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name.ToString();
                }
            }
        }

        public void KickPlayer()
        {
            PlayerDataManager.Singleton.KickPlayer(playerDataId);
        }
    }
}