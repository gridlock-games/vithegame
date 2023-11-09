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

        public override void OnNetworkSpawn()
        {
            foreach (BotDefinition botDefinition in botDefinitions)
            {
                GameLogicManager.Singleton.AddBotData(botDefinition.characterIndex, botDefinition.skinIndex, botDefinition.team);
            }
        }

        [System.Serializable]
        private struct BotDefinition
        {
            public int characterIndex;
            public int skinIndex;
            public GameLogicManager.Team team;
        }
    }
}