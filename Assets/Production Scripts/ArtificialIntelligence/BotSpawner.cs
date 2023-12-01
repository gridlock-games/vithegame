using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class BotSpawner : NetworkBehaviour
    {
        [SerializeField] private BotDefinition[] botDefinitions;

        private void Start()
        {
            foreach (BotDefinition botDefinition in botDefinitions)
            {
                PlayerDataManager.Singleton.AddBotData(botDefinition.characterIndex, botDefinition.skinIndex, botDefinition.team);
            }
        }

        [System.Serializable]
        private class BotDefinition
        {
            public int characterIndex;
            public int skinIndex;
            public PlayerDataManager.Team team;
        }
    }
}