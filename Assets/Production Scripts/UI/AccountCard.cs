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
        [SerializeField] private Image changeTeamImage;
        [SerializeField] private Sprite leftArrowSprite;
        [SerializeField] private Sprite rightArrowSprite;

        private int playerDataId;
        private bool initialized;
        public void Initialize(int playerDataId, bool isLocked)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            backgroundImage.color = playerData.team == PlayerDataManager.Team.Competitor ? Color.white : PlayerDataManager.Singleton.GetRelativeTeamColor(playerData.team);

            lockedUIImage.sprite = isLocked | playerDataId < 0 ? lockedSprite : unlockedSprite;

            lobbyLeaderImage.gameObject.SetActive(PlayerDataManager.Singleton.GetLobbyLeader().Value.id == playerDataId);

            kickButton.gameObject.SetActive(!lobbyLeaderImage.gameObject.activeSelf & PlayerDataManager.Singleton.IsLobbyLeader() & !GameModeManager.Singleton);

            initialized = true;

            nameDisplayText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name.ToString();

            changeTeamImage.gameObject.SetActive(false);
        }

        private PlayerDataManager.Team teamToChangeTo = PlayerDataManager.Team.Environment;
        public void SetChangeTeamLogic(PlayerDataManager.Team teamToChangeTo, bool isRightSideCard)
        {
            if (teamToChangeTo == PlayerDataManager.Team.Environment) { return; }

            if (isRightSideCard)
            {
                changeTeamImage.transform.SetAsFirstSibling();
                changeTeamImage.sprite = leftArrowSprite;
            }
            else
            {
                changeTeamImage.transform.SetAsLastSibling();
                changeTeamImage.sprite = rightArrowSprite;
            }
            changeTeamImage.gameObject.SetActive(true);
            this.teamToChangeTo = teamToChangeTo;
        }

        public void InitializeAsMVPScore(int playerDataId)
        {
            this.playerDataId = playerDataId;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            backgroundImage.color = playerData.team == PlayerDataManager.Team.Competitor ? Color.white : PlayerDataManager.Singleton.GetRelativeTeamColor(playerData.team);

            nameDisplayText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name.ToString();

            lobbyLeaderImage.gameObject.SetActive(true);
            kickButton.gameObject.SetActive(false);
            changeTeamImage.gameObject.SetActive(false);
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

        public void ChangeTeam()
        {
            if (teamToChangeTo == PlayerDataManager.Team.Environment) { return; }

            if (PlayerDataManager.Singleton.ContainsId((int)playerDataId))
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
                playerData.team = teamToChangeTo;
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
        }
    }
}