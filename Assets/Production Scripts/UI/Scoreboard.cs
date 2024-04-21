using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Player;

namespace Vi.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private GameObject teamDividerScoreboardLine;
        [SerializeField] private ScoreboardElement scoreboardElementPrefab;
        [SerializeField] private Transform scoreboardElementParent;
        [SerializeField] private Text scoreboardHeaderText;

        public void CloseSelf()
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ActionMapHandler>().CloseScoreboard();
        }

        private void Start()
        {
            if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
                scoreboardHeaderText.text = "Training Room | " + PlayerDataManager.Singleton.GetMapName();
            else
                scoreboardHeaderText.text = LobbyUI.FromCamelCase(PlayerDataManager.Singleton.GetGameMode().ToString()) + " | " + PlayerDataManager.Singleton.GetMapName();

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
                    if (PlayerDataManager.GetTeamColor(team) != Color.black)
                    {
                        GameObject dividerLine = Instantiate(teamDividerScoreboardLine, scoreboardElementParent);
                        dividerLine.GetComponentInChildren<Image>().color = PlayerDataManager.GetTeamColor(team);
                        dividerLine.GetComponentInChildren<Text>().text = team + " Team";
                        dividerLine.transform.SetSiblingIndex(i + dividerCounter);
                        dividerCounter++;
                        lastTeam = team;
                    }
                }
                elementList[i].transform.SetSiblingIndex(i + dividerCounter);
            }
        }
    }
}