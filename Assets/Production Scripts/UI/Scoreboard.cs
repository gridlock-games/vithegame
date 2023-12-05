using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private ScoreboardElement scoreboardElementPrefab;
        [SerializeField] private Transform scoreboardElementParent;

        private void Start()
        {
            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                GameObject instance = Instantiate(scoreboardElementPrefab.gameObject, scoreboardElementParent);

                if (instance.TryGetComponent(out ScoreboardElement scoreboardElement))
                {
                    scoreboardElement.Initialize(attributes);
                }
                else
                {
                    Debug.LogError("Scoreboard element prefab doesn't have a ScoreboardElement component!");
                }
            }
        }
    }
}