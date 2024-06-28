using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Player;
using Vi.Utility;

namespace Vi.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private ScoreboardTeamDividerElement teamDividerScoreboardLine;
        [SerializeField] private ScoreboardElement scoreboardElementPrefab;
        [SerializeField] private Transform scoreboardElementParent;
        [SerializeField] private Text scoreboardHeaderText;
        [SerializeField] private Text spectatorCountText;

        public void CloseSelf()
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ActionMapHandler>().CloseScoreboard();
        }

        private void Start()
        {
            if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
                scoreboardHeaderText.text = "No Game Mode | " + PlayerDataManager.Singleton.GetMapName();
            else
                scoreboardHeaderText.text = StringUtility.FromCamelCase(PlayerDataManager.Singleton.GetGameMode().ToString()) + " | " + PlayerDataManager.Singleton.GetMapName();

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                GameObject instance = Instantiate(scoreboardElementPrefab.gameObject, scoreboardElementParent);
                if (instance.TryGetComponent(out ScoreboardElement scoreboardElement))
                {
                    scoreboardElement.Initialize(playerData.id);
                    elementList.Add(scoreboardElement);
                }
                else
                {
                    Debug.LogError("Scoreboard element prefab doesn't have a ScoreboardElement component!");
                }
            }
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetDisconnectedPlayerDataList())
            {
                if (playerData.team == PlayerDataManager.Team.Spectator) { continue; }

                GameObject instance = Instantiate(scoreboardElementPrefab.gameObject, scoreboardElementParent);
                if (instance.TryGetComponent(out ScoreboardElement scoreboardElement))
                {
                    scoreboardElement.Initialize(playerData.id);
                    elementList.Add(scoreboardElement);
                }
                else
                {
                    Debug.LogError("Scoreboard element prefab doesn't have a ScoreboardElement component!");
                }
            }

            elementList.Sort((x, y) => x.GetTeam().CompareTo(y.GetTeam()));
            for (int i = 0; i < elementList.Count; i++)
            {
                elementList[i].transform.SetSiblingIndex(i);
            }

            int dividerCounter = 0;
            PlayerDataManager.Team lastTeam = PlayerDataManager.Team.Environment;
            for (int i = 0; i < elementList.Count; i++)
            {
                PlayerDataManager.Team team = elementList[i].GetTeam();
                if (team != lastTeam)
                {
                    if (team != PlayerDataManager.Team.Competitor & team != PlayerDataManager.Team.Peaceful)
                    {
                        ScoreboardTeamDividerElement dividerElement = Instantiate(teamDividerScoreboardLine.gameObject, scoreboardElementParent).GetComponent<ScoreboardTeamDividerElement>();
                        dividerElement.Initialize(team);
                        dividerElement.transform.SetSiblingIndex(i + dividerCounter);
                        dividerCounter++;
                        lastTeam = team;
                    }
                }
                elementList[i].transform.SetSiblingIndex(i + dividerCounter);
            }
        }

        private void Update()
        {
            spectatorCountText.text = "Spectator Count: " + PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().FindAll(item => item.team == PlayerDataManager.Team.Spectator).Count.ToString();
        }
    }
}