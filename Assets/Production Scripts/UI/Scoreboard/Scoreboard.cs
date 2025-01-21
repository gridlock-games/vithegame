using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Player;
using Vi.Utility;
using System.Linq;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private Text scoreboardHeaderText;
        [SerializeField] private Text spectatorCountText;
        [SerializeField] private GameObject closeButton;

        [Header("1 Team")]
        [SerializeField] private ScoreboardTeamDividerElement teamDividerScoreboardLine;
        [SerializeField] private ScoreboardElement scoreboardElementPrefabFull;
        [SerializeField] private GameObject singleTeamParent;
        [SerializeField] private Transform scoreboardElementParentCenter;

        [Header("2 Teams")]
        [SerializeField] private ScoreboardElement scoreboardElementPrefabHalf;
        [SerializeField] private GameObject doubleTeamParent;
        [SerializeField] private ScoreboardTeamDividerElement leftParentHeader;
        [SerializeField] private Transform scoreboardElementParentLeft;
        [SerializeField] private ScoreboardTeamDividerElement rightParentHeader;
        [SerializeField] private Transform scoreboardElementParentRight;

        public void CloseSelf()
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject)
            {
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ActionMapHandler>().CloseScoreboard();
            }
            else
            {
                ObjectPoolingManager.ReturnObjectToPool(GetComponent<PooledObject>());
            }
        }

        private void OnEnable()
        {
            if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
            {
                if (NetSceneManager.Singleton.IsSceneGroupLoaded("Training Room"))
                {
                    scoreboardHeaderText.text = "Training Room";
                }
                else if (NetSceneManager.Singleton.IsSceneGroupLoaded("Player Hub"))
                {
                    scoreboardHeaderText.text = "Player Hub";
                }
                else
                {
                    scoreboardHeaderText.text = "No Game Mode | " + PlayerDataManager.Singleton.GetMapName();
                }
            }
            else
                scoreboardHeaderText.text = PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()) + " | " + PlayerDataManager.Singleton.GetMapName();

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

            UpdateSpectatorText();

            if (GameModeManager.Singleton)
            {
                if (GameModeManager.Singleton.GetPostGameStatus() != GameModeManager.PostGameStatus.None)
                {
                    closeButton.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame) { UpdateSpectatorText(); }
        }

        private void LateUpdate()
        {
            if (GameModeManager.Singleton)
            {
                if (GameModeManager.Singleton.GetPostGameStatus() != GameModeManager.PostGameStatus.None)
                {
                    closeButton.gameObject.SetActive(true);
                }
            }
        }

        private void UpdateSpectatorText()
        {
            spectatorCountText.text = "Spectator Count: " + PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().Count(item => item.team == PlayerDataManager.Team.Spectator).ToString();
        }

        private void OnDisable()
        {
            foreach (PooledObject pooledObject in pooledChildObjects)
            {
                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            }
            pooledChildObjects.Clear();
        }

        private List<PooledObject> pooledChildObjects = new List<PooledObject>();
        private void PopulateScoreboardWith2Columns(PlayerDataManager.Team[] teams)
        {
            singleTeamParent.SetActive(false);
            doubleTeamParent.SetActive(true);

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                int teamIndex = System.Array.IndexOf(teams, playerData.team);
                if (teamIndex == -1) { Debug.LogError(playerData.character.name + " player data's team isn't in the possible teams list!"); continue; }

                PooledObject instance = ObjectPoolingManager.SpawnObject(scoreboardElementPrefabHalf.GetComponent<PooledObject>(), teamIndex == 0 ? scoreboardElementParentLeft : scoreboardElementParentRight);
                instance.transform.localScale = Vector3.one;
                pooledChildObjects.Add(instance);
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

                PooledObject instance = ObjectPoolingManager.SpawnObject(scoreboardElementPrefabHalf.GetComponent<PooledObject>(), teamIndex == 0 ? scoreboardElementParentLeft : scoreboardElementParentRight);
                instance.transform.localScale = Vector3.one;
                pooledChildObjects.Add(instance);
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

            elementList = elementList.OrderByDescending(item => item.GetTeam()).ThenByDescending(item => item.PlayerScore.cumulativeKills).ThenByDescending(item => item.PlayerScore.cumulativeAssists).ThenBy(item => item.PlayerScore.cumulativeDeaths).ToList();
            for (int i = 0; i < elementList.Count; i++)
            {
                elementList[i].transform.SetSiblingIndex(i);
            }

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

                        if (teamIndex == 0)
                        {
                            leftParentHeader.Initialize(team);
                        }
                        else
                        {
                            rightParentHeader.Initialize(team);
                        }
                        lastTeam = team;
                    }
                }
                elementList[i].transform.SetSiblingIndex(i);
            }
        }

        private void PopulateScoreboardWith1Column()
        {
            singleTeamParent.SetActive(true);
            doubleTeamParent.SetActive(false);

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators())
            {
                PooledObject instance = ObjectPoolingManager.SpawnObject(scoreboardElementPrefabFull.GetComponent<PooledObject>(), scoreboardElementParentCenter);
                instance.transform.localScale = Vector3.one;
                pooledChildObjects.Add(instance);
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

                PooledObject instance = ObjectPoolingManager.SpawnObject(scoreboardElementPrefabFull.GetComponent<PooledObject>(), scoreboardElementParentCenter);
                instance.transform.localScale = Vector3.one;
                pooledChildObjects.Add(instance);
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

            elementList = elementList.OrderByDescending(item => item.GetTeam()).ThenByDescending(item => item.PlayerScore.cumulativeKills).ThenByDescending(item => item.PlayerScore.cumulativeAssists).ThenBy(item => item.PlayerScore.cumulativeDeaths).ToList();
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
                        PooledObject instance = ObjectPoolingManager.SpawnObject(teamDividerScoreboardLine.GetComponent<PooledObject>(), scoreboardElementParentCenter);
                        instance.transform.localScale = Vector3.one;
                        pooledChildObjects.Add(instance);
                        ScoreboardTeamDividerElement dividerElement = instance.GetComponent<ScoreboardTeamDividerElement>();
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