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
            UpdateUI();
            initialized = true;
            player = PlayerDataManager.Singleton.ContainsId(playerDataId) ? PlayerDataManager.Singleton.GetPlayerObjectById(playerDataId) : null;
        }

        private void SetRoundWinsColumnActive(bool isActive)
        {
            if (isActive)
            {
                if (!roundWinsParent.gameObject.activeSelf)
                {
                    playerNameParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerNameParent.sizeDelta.x - roundWinsParent.sizeDelta.x);
                    roundWinsParent.gameObject.SetActive(true);
                }
            }
            else
            {
                if (roundWinsParent.gameObject.activeSelf)
                {
                    playerNameParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, playerNameParent.sizeDelta.x + roundWinsParent.sizeDelta.x);
                    roundWinsParent.gameObject.SetActive(false);
                }
            }
        }

        public PlayerDataManager.Team GetTeam()
        {
            if (PlayerDataManager.Singleton.ContainsId(playerDataId))
            {
                return PlayerDataManager.Singleton.GetPlayerData(playerDataId).team;
            }
            else if (PlayerDataManager.Singleton.ContainsDisconnectedPlayerData(playerDataId))
            {
                return PlayerDataManager.Singleton.GetDisconnectedPlayerData(playerDataId).team;
            }
            return default;
        }

        private void OnEnable()
        {
            disconnectedPlayerIcon.enabled = false;
            SetRoundWinsColumnActive(PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.FreeForAll);
        }

        private void OnDisable()
        {
            initialized = false;
            player = null;
            playerDataId = default;
        }

        private void Update()
        {
            if (!initialized) { return; }
            UpdateUI();
        }

        public GameModeManager.PlayerScore PlayerScore { get; private set; }
        void UpdateUI()
        {
            if (PlayerDataManager.Singleton.ContainsId(playerDataId))
            {
                disconnectedPlayerIcon.enabled = false;
                PlayerScore = GameModeManager.Singleton.GetPlayerScore(playerDataId);
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
                roundWinsText.text = PlayerScore.roundWins.ToString();
                killsText.text = PlayerScore.cumulativeKills.ToString();
                assistsText.text = PlayerScore.cumulativeAssists.ToString();
                deathsText.text = PlayerScore.cumulativeDeaths.ToString();

                if (kdRatioText) kdRatioText.text = PlayerScore.cumulativeDeaths == 0 ? PlayerScore.cumulativeKills.ToString("F2") : (PlayerScore.cumulativeKills / (float)PlayerScore.cumulativeDeaths).ToString("F2");
                if (damageDealtText) damageDealtText.text = PlayerScore.cumulativeDamageDealt.ToString("F0");
                if (damageRecievedText) damageRecievedText.text = PlayerScore.damageRecievedThisRound.ToString("F0");
            }
            else if (PlayerDataManager.Singleton.ContainsDisconnectedPlayerData(playerDataId))
            {
                disconnectedPlayerIcon.enabled = true;
                PlayerScore = GameModeManager.Singleton.GetDisconnectedPlayerScore(playerDataId);
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetDisconnectedPlayerData(playerDataId);
                for (int i = 0; i < backgroundImagesToColor.Length; i++)
                {
                    backgroundImagesToColor[i].color = (int)NetworkManager.Singleton.LocalClientId == playerDataId ? PlayerDataManager.LocalPlayerBackgroundColor : PlayerDataManager.Singleton.GetRelativeTeamColor(playerData.team);
                    backgroundImagesToColor[i].color += Color.black;
                    backgroundImagesToColor[i].color /= 2;
                }
                playerNameText.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name;
                roundWinsText.text = PlayerScore.roundWins.ToString();
                killsText.text = PlayerScore.cumulativeKills.ToString();
                assistsText.text = PlayerScore.cumulativeAssists.ToString();
                deathsText.text = PlayerScore.cumulativeDeaths.ToString();

                if (kdRatioText) kdRatioText.text = PlayerScore.cumulativeDeaths == 0 ? PlayerScore.cumulativeKills.ToString("F2") : (PlayerScore.cumulativeKills / (float)PlayerScore.cumulativeDeaths).ToString("F2");
                if (damageDealtText) damageDealtText.text = PlayerScore.cumulativeDamageDealt.ToString("F0");
                if (damageRecievedText) damageRecievedText.text = PlayerScore.damageRecievedThisRound.ToString("F0");
            }
            else
            {
                Debug.LogError("Scoreboard element doesn't have a player data element to reference " + playerDataId);
            }
        }
    }
}