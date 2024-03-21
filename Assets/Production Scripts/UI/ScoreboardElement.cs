using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class ScoreboardElement : MonoBehaviour
    {
        [SerializeField] private Image disconnectedPlayerIcon;
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text roundWinsText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text deathsText;
        [SerializeField] private Text kdRatioText;
        [SerializeField] private Image[] backgroundImagesToColor = new Image[0];

        public int playerDataId;
        private bool initialized;

        public void Initialize(int playerDataId)
        {
            this.playerDataId = playerDataId;
            UpdateUI();
            initialized = true;
        }

        public PlayerDataManager.Team GetTeam()
        {
            return PlayerDataManager.Singleton.GetPlayerData(playerDataId).team;
        }

        private void Start()
        {
            disconnectedPlayerIcon.enabled = false;
        }

        private void Update()
        {
            if (!initialized) { return; }

            UpdateUI();
        }

        void UpdateUI()
        {
            if (PlayerDataManager.Singleton.ContainsId(playerDataId))
            {
                disconnectedPlayerIcon.enabled = false;
                GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetPlayerScore(playerDataId);
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
                for (int i = 0; i < backgroundImagesToColor.Length; i++)
                {
                    backgroundImagesToColor[i].color = PlayerDataManager.GetTeamColor(playerData.team);
                }
                playerNameText.text = playerData.character.name.ToString();
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.kills.ToString();
                deathsText.text = playerScore.deaths.ToString();
                kdRatioText.text = playerScore.deaths == 0 ? playerScore.kills.ToString("F2") : (playerScore.kills / (float)playerScore.deaths).ToString("F2");
            }
            else if (PlayerDataManager.Singleton.ContainsDisconnectedPlayerData(playerDataId))
            {
                disconnectedPlayerIcon.enabled = true;
                GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetDisconnectedPlayerScore(playerDataId);
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetDisconnectedPlayerData(playerDataId);
                for (int i = 0; i < backgroundImagesToColor.Length; i++)
                {
                    backgroundImagesToColor[i].color = PlayerDataManager.GetTeamColor(playerData.team);
                }
                playerNameText.text = playerData.character.name.ToString();
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.kills.ToString();
                deathsText.text = playerScore.deaths.ToString();
                kdRatioText.text = playerScore.deaths == 0 ? playerScore.kills.ToString("F2") : (playerScore.kills / (float)playerScore.deaths).ToString("F2");
            }
            else
            {
                Debug.LogError("Scoreboard element doesn't have a player data element to reference " + playerDataId);
            }
        }
    }
}