using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.UI
{
    public class SpectatorUI : MonoBehaviour
    {
        [SerializeField] private PlayerCard[] leftPlayerCards;
        [SerializeField] private PlayerCard[] rightPlayerCards;

        private void Update()
        {
            GameLogicManager.GameModeInfo gameModeInfo = GameLogicManager.Singleton.GetGameModeInfo();

            if (gameModeInfo.possibleTeams.Length == 1)
            {
                List<Attributes> attributesList = GameLogicManager.Singleton.GetPlayersOnTeam(gameModeInfo.possibleTeams[0]);
                for (int i = 0; i < attributesList.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (i / 2 < leftPlayerCards.Length) { leftPlayerCards[i / 2].Initialize(attributesList[i]); }
                    }
                    else
                    {
                        if (i / 2 < rightPlayerCards.Length) { rightPlayerCards[i / 2].Initialize(attributesList[i]); }
                    }
                }
            }
            else if (gameModeInfo.possibleTeams.Length == 2)
            {
                for (int teamIndex = 0; teamIndex < gameModeInfo.possibleTeams.Length; teamIndex++)
                {
                    List<Attributes> attributesList = GameLogicManager.Singleton.GetPlayersOnTeam(gameModeInfo.possibleTeams[teamIndex]);
                    for (int i = 0; i < attributesList.Count; i++)
                    {
                        if (teamIndex == 0)
                        {
                            if (i < leftPlayerCards.Length) { leftPlayerCards[i].Initialize(attributesList[i]); }
                        }
                        else
                        {
                            if (i < rightPlayerCards.Length) { rightPlayerCards[i].Initialize(attributesList[i]); }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Haven't implemented spectator UI when there are " + gameModeInfo.possibleTeams.Length + " possible teams");
            }
        }
    }
}