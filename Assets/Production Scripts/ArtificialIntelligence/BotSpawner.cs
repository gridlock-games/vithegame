using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class BotSpawner : MonoBehaviour
    {
        [SerializeField] private BotDefinition[] botDefinitions;

        private void Start()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(SpawnBots());
            }
        }

        private IEnumerator SpawnBots()
        {
            yield return new WaitUntil(() => NetSceneManager.Singleton.ShouldSpawnPlayer());
            foreach (BotDefinition botDefinition in botDefinitions)
            {
                PlayerDataManager.Singleton.AddBotData(botDefinition.team);
                yield return new WaitForSeconds(1);
            }
        }

        [System.Serializable]
        private class BotDefinition
        {
            public PlayerDataManager.Team team;
        }
    }
}