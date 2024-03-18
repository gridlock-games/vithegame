using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.GameModeManagers;
using UnityEngine.UI;

namespace Vi.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private ScoreboardElement scoreboardElementPrefab;
        [SerializeField] private Transform scoreboardElementParent;
        [SerializeField] private Text scoreboardHeaderText;

        private void Start()
        {
            if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
                scoreboardHeaderText.text = "Training Room | " + PlayerDataManager.Singleton.GetMapName();
            else
                scoreboardHeaderText.text = LobbyUI.FromCamelCase(PlayerDataManager.Singleton.GetGameMode().ToString()) + " | " + PlayerDataManager.Singleton.GetMapName();

            List<ScoreboardElement> elementList = new List<ScoreboardElement>();
            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                GameObject instance = Instantiate(scoreboardElementPrefab.gameObject, scoreboardElementParent);

                if (instance.TryGetComponent(out ScoreboardElement scoreboardElement))
                {
                    scoreboardElement.Initialize(attributes);
                    elementList.Add(scoreboardElement);
                }
                else
                {
                    Debug.LogError("Scoreboard element prefab doesn't have a ScoreboardElement component!");
                }
            }

            elementList.Sort((x,y) => PlayerDataManager.Singleton.GetPlayerData(x.Attributes.GetPlayerDataId()).team.CompareTo(PlayerDataManager.Singleton.GetPlayerData(y.Attributes.GetPlayerDataId()).team));
            for (int i = 0; i < elementList.Count; i++)
            {
                elementList[i].transform.SetSiblingIndex(i);
            }
        }
    }
}