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
        [SerializeField] private Text scoreboardHeaderText;
        [SerializeField] private Text spectatorCountText;

        [Header("1 Team")]
        [SerializeField] private ScoreboardTeamDividerElement teamDividerScoreboardLine;
        [SerializeField] private ScoreboardElement scoreboardElementPrefabFull;
        [SerializeField] private GameObject singleTeamParent;
        [SerializeField] private Transform scoreboardElementParentCenter;

        [Header("2 Teams")]
        [SerializeField] private ScoreboardElement scoreboardElementPrefabHalf;
        [SerializeField] private GameObject doubleTeamParent;
        [SerializeField] private Transform teamDividerParentLeft;
        [SerializeField] private Transform scoreboardElementParentLeft;
        [SerializeField] private Transform teamDividerParentRight;
        [SerializeField] private Transform scoreboardElementParentRight;

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


            PlayerDataManager.GameModeInfo gameModeInfo = PlayerDataManager.Singleton.GetGameModeInfo();

            if (gameModeInfo.possibleTeams.Length == 1)
            {
                PopulateScoreboardWith1Column();
            }
            else if (gameModeInfo.possibleTeams.Length == 2)
            {
                PopulateScoreboardWith2Columns(gameModeInfo.possibleTeams);
            }
            else
            {
                Debug.LogError("Haven't implemented scoreboard UI when there are " + gameModeInfo.possibleTeams.Length + " possible teams");
                PopulateScoreboardWith1Column();
            }
        }

        private void Update()
        {
            spectatorCountText.text = "Spectator Count: " + PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().FindAll(item => item.team == PlayerDataManager.Team.Spectator).Count.ToString();
        }

        private void PopulateScoreboardWith2Columns(PlayerDataManager.Team[] teams)
        {
            singleTeamParent.SetActive(false);
            doubleTeamParent.SetActive(true);

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                int teamIndex = System.Array.IndexOf(teams, playerData.team);
                if (teamIndex == -1) { Debug.LogError(playerData.character.name + " player data's team isn't in the possible teams list!"); continue; }

                GameObject instance = Instantiate(scoreboardElementPrefabHalf.gameObject, teamIndex == 0 ? scoreboardElementParentLeft : scoreboardElementParentRight);
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

                int teamIndex = System.Array.IndexOf(teams, playerData.team);
                if (teamIndex == -1) { Debug.LogError(playerData.character.name + " player data's team isn't in the possible teams list!"); continue; }

                GameObject instance = Instantiate(scoreboardElementPrefabHalf.gameObject, teamIndex == 0 ? scoreboardElementParentLeft : scoreboardElementParentRight);
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
                        int teamIndex = System.Array.IndexOf(teams, team);
                        if (teamIndex == -1) { Debug.LogError(team + " scoreboard team header's team isn't in the possible teams list!"); continue; }

                        ScoreboardTeamDividerElement dividerElement = Instantiate(teamDividerScoreboardLine.gameObject, teamIndex == 0 ? teamDividerParentLeft : teamDividerParentRight).GetComponent<ScoreboardTeamDividerElement>();
                        dividerElement.Initialize(team);
                        dividerElement.transform.SetSiblingIndex(i + dividerCounter);
                        dividerCounter++;
                        lastTeam = team;
                    }
                }
                elementList[i].transform.SetSiblingIndex(i + dividerCounter);
            }
        }

        private void PopulateScoreboardWith1Column()
        {
            singleTeamParent.SetActive(true);
            doubleTeamParent.SetActive(false);

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                GameObject instance = Instantiate(scoreboardElementPrefabFull.gameObject, scoreboardElementParentCenter);
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

                GameObject instance = Instantiate(scoreboardElementPrefabFull.gameObject, scoreboardElementParentCenter);
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
                        ScoreboardTeamDividerElement dividerElement = Instantiate(teamDividerScoreboardLine.gameObject, scoreboardElementParentCenter).GetComponent<ScoreboardTeamDividerElement>();
                        dividerElement.Initialize(team);
                        dividerElement.transform.SetSiblingIndex(i + dividerCounter);
                        dividerCounter++;
                        lastTeam = team;
                    }
                }
                elementList[i].transform.SetSiblingIndex(i + dividerCounter);
            }
        }
    }
}