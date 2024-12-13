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
        [SerializeField] private PlayerDataManager.Team botTeam = PlayerDataManager.Team.Competitor;

        private void Start()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (!FasterPlayerPrefs.Singleton.GetBool("TutorialInProgress"))
                {
                    StartCoroutine(SpawnBots());
                }
            }
        }

        private IEnumerator SpawnBots()
        {
            yield return new WaitUntil(() => NetSceneManager.Singleton.ShouldSpawnPlayerCached);
            for (int i = 0; i < FasterPlayerPrefs.Singleton.GetInt("BotSpawnNumber"); i++)
            {
                PlayerDataManager.Singleton.AddBotData(botTeam, false);
            }
        }
    }
}