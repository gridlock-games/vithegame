using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Utility;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class MobSpawner : MonoBehaviour
    {
        [SerializeField] private MobDefinition[] mobDefinitions;

        private void Start()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(SpawnMobs());
            }
        }

        private IEnumerator SpawnMobs()
        {
            yield return new WaitUntil(() => NetSceneManager.Singleton.ShouldSpawnPlayer);
            foreach (MobDefinition mobDefinition in mobDefinitions)
            {
                (bool spawnPointFound, PlayerSpawnPoints.TransformData transformData) = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetSpawnOrientation(PlayerDataManager.Singleton.GetGameMode(), mobDefinition.team, PlayerDataManager.defaultChannel);
                Instantiate(mobDefinition.mobPrefab.gameObject, transformData.position, transformData.rotation).GetComponent<NetworkObject>().Spawn();
            }
        }

        [System.Serializable]
        private class MobDefinition
        {
            public PlayerDataManager.Team team;
            public Mob mobPrefab;
        }
    }
}