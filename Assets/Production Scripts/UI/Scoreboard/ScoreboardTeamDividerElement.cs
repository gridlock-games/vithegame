using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;
using System.Linq;

namespace Vi.UI
{
    public class ScoreboardTeamDividerElement : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text teamText;

        private bool initialized;
        private PlayerDataManager.Team team;

        public void Initialize(PlayerDataManager.Team team)
        {
            this.team = team;
            backgroundImage.color = PlayerDataManager.GetTeamColor(team);
            UpdateUI();
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) { return; }
            UpdateUI();
        }

        private void UpdateUI()
        {
            try
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().First(item => item.team == team);
                teamText.text = PlayerDataManager.Singleton.GetTeamText(team) + " - " + GameModeManager.Singleton.GetPlayerScore(playerData.id).roundWins;
            }
            catch
            {
                teamText.text = PlayerDataManager.Singleton.GetTeamText(team);
            }
        }
    }
}
