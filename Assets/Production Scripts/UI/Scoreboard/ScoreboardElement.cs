using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Vi.ScriptableObjects;
using Vi.Core.CombatAgents;
using Unity.Netcode;

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
        private Attributes player;

        public void Initialize(int playerDataId)
        {
            this.playerDataId = playerDataId;
            if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.FreeForAll) { HideRoundWinsColumn(); }
            UpdateUI();
            initialized = true;
            player = PlayerDataManager.Singleton.GetPlayerObjectById(playerDataId);
        }

        public void HideRoundWinsColumn()
        {
            if (roundWinsParent.gameObject.activeSelf)
            {
                playerNameParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerNameParent.sizeDelta.x + roundWinsParent.sizeDelta.x);
                roundWinsParent.gameObject.SetActive(false);
            }
        }

        public PlayerDataManager.Team GetTeam()
        {
            return PlayerDataManager.Singleton.GetPlayerData(playerDataId).team;
        }

        private void OnDisable()
        {
            initialized = false;
            player = null;
            playerDataId = default;
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
                    backgroundImagesToColor[i].color = (int)NetworkManager.Singleton.LocalClientId == playerDataId ? PlayerDataManager.LocalPlayerBackgroundColor : PlayerDataManager.Singleton.GetRelativeTeamColor(playerData.team);
                    if (player)
                    {
                        if (player.GetAilment() == ActionClip.Ailment.Death)
                        {
                            backgroundImagesToColor[i].color += Color.black;
                            backgroundImagesToColor[i].color /= 2;
                        }
                    }
                }
                playerNameText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name;
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.cumulativeKills.ToString();
                assistsText.text = playerScore.cumulativeAssists.ToString();
                deathsText.text = playerScore.cumulativeDeaths.ToString();

                if (kdRatioText) kdRatioText.text = playerScore.cumulativeDeaths == 0 ? playerScore.cumulativeKills.ToString("F2") : (playerScore.cumulativeKills / (float)playerScore.cumulativeDeaths).ToString("F2");
                if (damageDealtText) damageDealtText.text = playerScore.cumulativeDamageDealt.ToString("F0");
                if (damageRecievedText) damageRecievedText.text = playerScore.damageRecievedThisRound.ToString("F0");
            }
            else if (PlayerDataManager.Singleton.ContainsDisconnectedPlayerData(playerDataId))
            {
                disconnectedPlayerIcon.enabled = true;
                GameModeManager.PlayerScore playerScore = GameModeManager.Singleton.GetDisconnectedPlayerScore(playerDataId);
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetDisconnectedPlayerData(playerDataId);
                for (int i = 0; i < backgroundImagesToColor.Length; i++)
                {
                    backgroundImagesToColor[i].color = (int)NetworkManager.Singleton.LocalClientId == playerDataId ? PlayerDataManager.LocalPlayerBackgroundColor : PlayerDataManager.Singleton.GetRelativeTeamColor(playerData.team);
                    backgroundImagesToColor[i].color += Color.black;
                    backgroundImagesToColor[i].color /= 2;
                }
                playerNameText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name;
                roundWinsText.text = playerScore.roundWins.ToString();
                killsText.text = playerScore.cumulativeKills.ToString();
                assistsText.text = playerScore.cumulativeAssists.ToString();
                deathsText.text = playerScore.cumulativeDeaths.ToString();

                if (kdRatioText) kdRatioText.text = playerScore.cumulativeDeaths == 0 ? playerScore.cumulativeKills.ToString("F2") : (playerScore.cumulativeKills / (float)playerScore.cumulativeDeaths).ToString("F2");
                if (damageDealtText) damageDealtText.text = playerScore.cumulativeDamageDealt.ToString("F0");
                if (damageRecievedText) damageRecievedText.text = playerScore.damageRecievedThisRound.ToString("F0");
            }
            else
            {
                Debug.LogError("Scoreboard element doesn't have a player data element to reference " + playerDataId);
            }
        }
    }
}