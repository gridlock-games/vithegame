using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vi.Core;
using Vi.Player;

namespace Vi.UI
{
    public class SpectatorUI : MonoBehaviour
    {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private PlayerCard[] leftPlayerCards;
        [SerializeField] private PlayerCard[] rightPlayerCards;

        private Spectator spectator;
        private void Start()
        {
            spectator = GetComponentInParent<Spectator>();

            List<PlayerCard> leftPlayerCardsTemp = leftPlayerCards.ToList();
            List<PlayerCard> rightPlayerCardsTemp = rightPlayerCards.ToList();

            leftPlayerCardsTemp.RemoveAll(item => item == null);
            rightPlayerCardsTemp.RemoveAll(item => item == null);

            leftPlayerCards = leftPlayerCardsTemp.ToArray();
            rightPlayerCards = rightPlayerCardsTemp.ToArray();
        }

        public void OpenPauseMenu()
        {
            spectator.GetComponent<ActionMapHandler>().OnPause();
        }

        public void OpenInventoryMenu()
        {
            spectator.GetComponent<ActionMapHandler>().OnInventory();
        }

        public void OpenScoreboard()
        {
            spectator.GetComponent<ActionMapHandler>().OpenScoreboard();
        }

        private void Update()
        {
            PlayerDataManager.GameModeInfo gameModeInfo = PlayerDataManager.Singleton.GetGameModeInfo();
            List<Attributes> initializedAttributesList = new List<Attributes>();

            if (gameModeInfo.possibleTeams.Length == 1)
            {
                List<Attributes> attributesList = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(gameModeInfo.possibleTeams[0]);
                for (int i = 0; i < attributesList.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (i / 2 < leftPlayerCards.Length)
                        {
                            leftPlayerCards[i / 2].Initialize(attributesList[i]);
                            initializedAttributesList.Add(attributesList[i]);
                        }
                    }
                    else
                    {
                        if (i / 2 < rightPlayerCards.Length)
                        {
                            rightPlayerCards[i / 2].Initialize(attributesList[i]);
                            initializedAttributesList.Add(attributesList[i]);
                        }
                    }
                }
            }
            else if (gameModeInfo.possibleTeams.Length == 2)
            {
                for (int teamIndex = 0; teamIndex < gameModeInfo.possibleTeams.Length; teamIndex++)
                {
                    List<Attributes> attributesList = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(gameModeInfo.possibleTeams[teamIndex]);
                    for (int i = 0; i < attributesList.Count; i++)
                    {
                        if (teamIndex == 0)
                        {
                            if (i < leftPlayerCards.Length)
                            {
                                leftPlayerCards[i].Initialize(attributesList[i]);
                                initializedAttributesList.Add(attributesList[i]);
                            }
                        }
                        else
                        {
                            if (i < rightPlayerCards.Length)
                            {
                                rightPlayerCards[i].Initialize(attributesList[i]);
                                initializedAttributesList.Add(attributesList[i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Haven't implemented spectator UI when there are " + gameModeInfo.possibleTeams.Length + " possible teams");
            }

            spectator.SetPlayerList(initializedAttributesList);
        }
    }
}