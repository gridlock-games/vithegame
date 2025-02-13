using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vi.Core;
using UnityEngine.UI;
using System.Linq;

namespace Vi.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Transform elementParent;
        [SerializeField] private LeaderboardElement killsElementPrefab;
        [SerializeField] private LeaderboardElement hordeElementPrefab;

        [SerializeField] private GameObject FFAHeaders;
        [SerializeField] private GameObject teamElimHeaders;
        [SerializeField] private GameObject hordeModeHeaders;

        [SerializeField] private Text leaderboardRequestText;

        private List<PlayerDataManager.GameMode> gameModeValues = new List<PlayerDataManager.GameMode>();

        private void Awake()
        {
            gameModeDropdown.ClearOptions();
            List<string> gameModeStrings = new List<string>();
            foreach (PlayerDataManager.GameMode gameMode in System.Enum.GetValues(typeof(PlayerDataManager.GameMode)))
            {
                if (gameMode != PlayerDataManager.GameMode.FreeForAll & gameMode != PlayerDataManager.GameMode.TeamElimination & gameMode != PlayerDataManager.GameMode.HordeMode) { continue; }
                gameModeStrings.Add(PlayerDataManager.GetGameModeString(gameMode));
                gameModeValues.Add(gameMode);
            }
            gameModeDropdown.AddOptions(gameModeStrings);
        }

        private void OnEnable()
        {
            leaderboardRequestText.text = "";
            OnGameModeChange();
        }

        public void OnGameModeChange()
        {
            foreach (Transform child in elementParent)
            {
                Destroy(child.gameObject);
            }

            FFAHeaders.SetActive(false);
            teamElimHeaders.SetActive(false);
            hordeModeHeaders.SetActive(false);
            switch (gameModeValues[gameModeDropdown.value])
            {
                case PlayerDataManager.GameMode.FreeForAll:
                    FFAHeaders.SetActive(true);
                    break;
                case PlayerDataManager.GameMode.TeamElimination:
                    teamElimHeaders.SetActive(true);
                    break;
                case PlayerDataManager.GameMode.HordeMode:
                    hordeModeHeaders.SetActive(true);
                    break;
                default:
                    Debug.LogError("Unsure of header object for game mode " + gameModeValues[gameModeDropdown.value]);
                    break;
            }

            if (gameModeValues[gameModeDropdown.value] == PlayerDataManager.GameMode.HordeMode)
            {
                foreach (LeaderboardManager.HordeLeaderboardEntry hordeLeaderboardEntry in WebRequestManager.Singleton.LeaderboardManager.hordeLeaderboardEntries.OrderByDescending(item => item.record.wave).ThenByDescending(item => item.record.clearTime).ThenByDescending(item => item.record.damageDealt).Take(100))
                {
                    if (hordeLeaderboardEntry.record.gameMode == gameModeDropdown.options[gameModeDropdown.value].text)
                    {
                        LeaderboardElement ele = Instantiate(hordeElementPrefab.gameObject, elementParent).GetComponent<LeaderboardElement>();
                        ele.Initialize(hordeLeaderboardEntry);
                    }
                }
            }
            else
            {
                foreach (LeaderboardManager.KillsLeaderboardEntry killsLeaderboardEntry in WebRequestManager.Singleton.LeaderboardManager.killsLeaderboardEntries)
                {
                    if (killsLeaderboardEntry.record.gameMode == gameModeDropdown.options[gameModeDropdown.value].text)
                    {
                        LeaderboardElement ele = Instantiate(killsElementPrefab.gameObject, elementParent).GetComponent<LeaderboardElement>();
                        ele.Initialize(killsLeaderboardEntry);
                    }
                }
            }
        }

        public void RefreshLeaderboard()
        {
            StartCoroutine(RefreshLeaderboardCoroutine());
        }

        private IEnumerator RefreshLeaderboardCoroutine()
        {
            refreshButton.interactable = false;
            leaderboardRequestText.text = "LOADING LEADERBOARD";
            yield return WebRequestManager.Singleton.LeaderboardManager.GetLeaderboard();
            leaderboardRequestText.text = "";
            refreshButton.interactable = true;
            OnGameModeChange();
        }
    }
}