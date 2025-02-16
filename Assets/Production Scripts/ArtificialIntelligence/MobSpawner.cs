using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.Core.CombatAgents;
using Vi.Utility;

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
            yield return new WaitUntil(() => NetSceneManager.Singleton.ShouldSpawnPlayerCached);
            foreach (MobDefinition mobDefinition in mobDefinitions)
            {
                if (!mobDefinition.enabled) { continue; }
                SpawnPoints.TransformData transformData = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetGenericMobSpawnPoint();
                PooledObject g = ObjectPoolingManager.SpawnObject(mobDefinition.mobPrefab.gameObject.GetComponent<PooledObject>(), transformData.position, transformData.rotation);
                g.GetComponent<Mob>().SetTeam(mobDefinition.team);
                g.GetComponent<NetworkObject>().Spawn(true);
            }
        }

        [System.Serializable]
        private class MobDefinition
        {
            public PlayerDataManager.Team team;
            public Mob mobPrefab;
            public bool enabled;
        }
    }
}