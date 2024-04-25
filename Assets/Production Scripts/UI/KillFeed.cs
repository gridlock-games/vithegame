using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using System.Linq;

namespace Vi.UI
{
    public class KillFeed : MonoBehaviour
    {
        [SerializeField] private KillFeedElement[] killFeedElementInstances;

        //private List<GameModeManager.KillHistoryElement> cachedKillHistory = new List<GameModeManager.KillHistoryElement>();
        private List<int> displayedKillHistoryIndices = new List<int>();

        private bool isPreview;
        private List<GameModeManager.KillHistoryElement> previewKillHistoryList;

        public void SetPreviewOn()
        {
            isPreview = true;
        }

        private void Start()
        {
            previewKillHistoryList = new List<GameModeManager.KillHistoryElement>()
            {
                new GameModeManager.KillHistoryElement(false),
                new GameModeManager.KillHistoryElement(false),
                new GameModeManager.KillHistoryElement(true),
                new GameModeManager.KillHistoryElement(false),
                new GameModeManager.KillHistoryElement(false),
                new GameModeManager.KillHistoryElement(true)
            };
        }

        private void Update()
        {
            if (isPreview)
            {
                List<GameModeManager.KillHistoryElement> cachedKillHistory = previewKillHistoryList;

                if (cachedKillHistory.Count == 0) { displayedKillHistoryIndices.Clear(); }

                Queue<(int, GameModeManager.KillHistoryElement)> killElementsToDisplay = new Queue<(int, GameModeManager.KillHistoryElement)>();
                for (int i = 0; i < cachedKillHistory.Count; i++)
                {
                    if (displayedKillHistoryIndices.Contains(i)) { continue; }
                    killElementsToDisplay.Enqueue((i, cachedKillHistory[i]));
                    if (killElementsToDisplay.Count >= killFeedElementInstances.Length) { break; }
                }

                for (int i = 0; i < killFeedElementInstances.Length; i++)
                {
                    if (killElementsToDisplay.Count == 0) { break; }
                    if (!killFeedElementInstances[i].IsNotRunning()) { continue; }

                    (int index, GameModeManager.KillHistoryElement killHistoryElement) = killElementsToDisplay.Dequeue();
                    killFeedElementInstances[i].Initialize(killHistoryElement);
                }
            }
            else
            {
                if (!GameModeManager.Singleton) { return; }

                List<GameModeManager.KillHistoryElement> cachedKillHistory = GameModeManager.Singleton.GetKillHistory();

                if (cachedKillHistory.Count == 0) { displayedKillHistoryIndices.Clear(); }

                Queue<(int, GameModeManager.KillHistoryElement)> killElementsToDisplay = new Queue<(int, GameModeManager.KillHistoryElement)>();
                for (int i = 0; i < cachedKillHistory.Count; i++)
                {
                    if (displayedKillHistoryIndices.Contains(i)) { continue; }
                    killElementsToDisplay.Enqueue((i, cachedKillHistory[i]));
                    if (killElementsToDisplay.Count >= killFeedElementInstances.Length) { break; }
                }

                for (int i = 0; i < killFeedElementInstances.Length; i++)
                {
                    if (killElementsToDisplay.Count == 0) { break; }
                    if (!killFeedElementInstances[i].IsNotRunning()) { continue; }

                    (int index, GameModeManager.KillHistoryElement killHistoryElement) = killElementsToDisplay.Dequeue();
                    killFeedElementInstances[i].Initialize(killHistoryElement);
                    displayedKillHistoryIndices.Add(index);
                }
            }
        }
    }
}