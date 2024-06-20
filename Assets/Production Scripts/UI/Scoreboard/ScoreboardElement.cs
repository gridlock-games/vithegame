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
        [SerializeField] private bool isPreviewObject;
        [SerializeField] private RectTransform playerNameParent;
        [SerializeField] private RectTransform roundWinsParent;
        [SerializeField] private Image disconnectedPlayerIcon;
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text roundWinsText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text assistsText;
        [SerializeField] private Text deathsText;
        [SerializeField] private Text kdRatioText;
        [SerializeField] private Text damageDealtText;
        [SerializeField] private Text damageRecievedText;
        [SerializeField] private Image[] backgroundImagesToColor = new Image[0];

        [HideInInspector] public int playerDataId;
        private bool initialized;

        public void Initialize(int playerDataId)
        {
            this.playerDataId = playerDataId;
            if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.FreeForAll) { HideRoundWinsColumn(); }
            UpdateUI();
            initialized = true;
        }

        public void HideRoundWinsColumn()
        {
            playerNameParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerNameParent.sizeDelta.x + roundWinsParent.sizeDelta.x);
            roundWinsParent.gameObject.SetActive(false);
        }

        public PlayerDataManager.Team GetTeam()
        {
            return PlayerDataManager.Singleton.GetPlayerData(playerDataId).team;
        }

        private void Start()
        {
            disconnectedPlayerIcon.enabled = false;

            if (isPreviewObject)
            {
                if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.FreeForAll) { HideRoundWinsColumn(); }
                enabled = false;
            }
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
                playerNameText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name;
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.cumulativeKills.ToString();
                assistsText.text = playerScore.cumulativeAssists.ToString();
                deathsText.text = playerScore.cumulativeDeaths.ToString();
                kdRatioText.text = playerScore.cumulativeDeaths == 0 ? playerScore.cumulativeKills.ToString("F2") : (playerScore.cumulativeKills / (float)playerScore.cumulativeDeaths).ToString("F2");
                damageDealtText.text = playerScore.cumulativeDamageDealt.ToString("F0");
                damageRecievedText.text = playerScore.damageRecievedThisRound.ToString("F0");
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
                playerNameText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name;
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.cumulativeKills.ToString();
                assistsText.text = playerScore.cumulativeAssists.ToString();
                deathsText.text = playerScore.cumulativeDeaths.ToString();
                kdRatioText.text = playerScore.cumulativeDeaths == 0 ? playerScore.cumulativeKills.ToString("F2") : (playerScore.cumulativeKills / (float)playerScore.cumulativeDeaths).ToString("F2");
                damageDealtText.text = playerScore.cumulativeDamageDealt.ToString();
                damageRecievedText.text = playerScore.damageRecievedThisRound.ToString();
            }
            else
            {
                Debug.LogError("Scoreboard element doesn't have a player data element to reference " + playerDataId);
            }
        }
    }
}