using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.ArtificialIntelligence
{
    public class BotSpawner : MonoBehaviour
    {
        [SerializeField] private BotDefinition[] botDefinitions;

        private void Start()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (!bool.Parse(FasterPlayerPrefs.Singleton.GetString("TutorialInProgress")))
                {
                    StartCoroutine(SpawnBots());
                }
            }
        }

        private IEnumerator SpawnBots()
        {
            yield return new WaitUntil(() => NetSceneManager.Singleton.ShouldSpawnPlayer);
            foreach (BotDefinition botDefinition in botDefinitions)
            {
                PlayerDataManager.Singleton.AddBotData(botDefinition.team, false);
            }
        }

        [System.Serializable]
        private class BotDefinition
        {
            public PlayerDataManager.Team team;
        }
    }
}