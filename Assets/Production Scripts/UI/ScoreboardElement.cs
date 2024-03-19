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

        public Attributes Attributes { get; private set; }

        public void Initialize(Attributes attributes)
        {
            this.Attributes = attributes;
            UpdateUI();
            gameObject.SetActive(attributes);
        }

        private void Update()
        {
            if (!Attributes) { gameObject.SetActive(false); return; }
            UpdateUI();
        }

        void UpdateUI()
        {
            GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetPlayerScore(Attributes.GetPlayerDataId());
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(Attributes.GetPlayerDataId());
            for (int i = 0; i < backgroundImagesToColor.Length; i++)
            {
                if (i % 2 == 0)
                    backgroundImagesToColor[i].color = PlayerDataManager.GetTeamColor(playerData.team);
                else
                    backgroundImagesToColor[i].color = PlayerDataManager.GetTeamColor(playerData.team) + new Color(50 / 255, 50 / 255, 50 / 255, 255 / 255);
            }
            playerNameText.text = playerData.character.name.ToString();
            roundWinsText.text = playerScore.roundWins.ToString();
            killsText.text = playerScore.kills.ToString();
            deathsText.text = playerScore.deaths.ToString();
            kdRatioText.text = playerScore.deaths == 0 ? playerScore.kills.ToString("F2") : (playerScore.kills / (float)playerScore.deaths).ToString("F2");
        }
    }
}