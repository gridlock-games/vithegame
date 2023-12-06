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
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text roundWinsText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text deathsText;
        [SerializeField] private Text kdRatioText;

        private Attributes attributes;

        public void Initialize(Attributes attributes)
        {
            this.attributes = attributes;
            UpdateUI();
            gameObject.SetActive(attributes);
        }

        private void Update()
        {
            if (!attributes) { gameObject.SetActive(false); return; }
            UpdateUI();
        }

        void UpdateUI()
        {
            GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetPlayerScore(attributes.GetPlayerDataId());
            playerNameText.text = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).playerName.ToString();
            roundWinsText.text = playerScore.roundWins.ToString();
            killsText.text = playerScore.kills.ToString();
            deathsText.text = playerScore.deaths.ToString();
            kdRatioText.text = playerScore.deaths == 0 ? playerScore.kills.ToString("F2") : (playerScore.kills / (float)playerScore.deaths).ToString("F2");
        }
    }
}