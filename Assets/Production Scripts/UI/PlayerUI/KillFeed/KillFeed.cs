using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vi.Core.GameModeManagers;
using Vi.Utility;

namespace Vi.UI
{
    public class KillFeed : MonoBehaviour
    {
        [SerializeField] private KillFeedElement[] killFeedElementInstances;

        private List<int> displayedKillHistoryIndices = new List<int>();

        private bool isPreview;
        private List<GameModeManager.KillHistoryElement> previewKillHistoryList;

        public void SetPreviewOn()
        {
            isPreview = true;
        }

        private void Start()
        {
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                List<KillFeedElement> newList = new List<KillFeedElement>();
                int counter = 1;
                foreach (KillFeedElement ele in killFeedElementInstances)
                {
                    if (counter > 3)
                    {
                        ele.GetComponent<Canvas>().enabled = false;
                    }
                    else
                    {
                        newList.Add(ele);
                    }
                    counter++;
                }
                killFeedElementInstances = newList.ToArray();
            }

            previewKillHistoryList = new List<GameModeManager.KillHistoryElement>()
            {
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.Player),
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.PlayerWithAssist),
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.Environment),
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.Player),
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.PlayerWithAssist),
                new GameModeManager.KillHistoryElement(GameModeManager.KillHistoryElement.KillType.Environment)
            };

            if (GameModeManager.Singleton)
            {
                List<GameModeManager.KillHistoryElement> cachedKillHistory = GameModeManager.Singleton.GetKillHistory();
                for (int i = 0; i < cachedKillHistory.Count; i++)
                {
                    displayedKillHistoryIndices.Add(i);
                }
            }
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

                for (int i = 0; i < killElementsToDisplay.Count; i++)
                {
                    KillFeedElement killFeedElement = GetFirstAvailableKillFeedElement();
                    (int index, GameModeManager.KillHistoryElement killHistoryElement) = killElementsToDisplay.Dequeue();
                    killFeedElement.Initialize(killHistoryElement);
                    displayedKillHistoryIndices.Add(index);
                }
            }
        }

        private KillFeedElement GetFirstAvailableKillFeedElement()
        {
            Dictionary<int, KillFeedElement> childIndexMapping = new Dictionary<int, KillFeedElement>();
            foreach (KillFeedElement killFeedElement in killFeedElementInstances)
            {
                childIndexMapping.Add(killFeedElement.transform.GetSiblingIndex(), killFeedElement);

                if (!killFeedElement.IsNotRunning()) { continue; }

                return killFeedElement;
            }
            return childIndexMapping[childIndexMapping.Max(kvp => kvp.Key)];
        }
    }
}