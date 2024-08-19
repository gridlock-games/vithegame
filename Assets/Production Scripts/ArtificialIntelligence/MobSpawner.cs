using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.Core.CombatAgents;

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
                SpawnPoints.TransformData transformData = PlayerDataManager.Singleton.GetPlayerSpawnPoints().GetMobSpawnPoint(mobDefinition.mobPrefab);
                GameObject g = Instantiate(mobDefinition.mobPrefab.gameObject, transformData.position, transformData.rotation);
                g.GetComponent<Mob>().SetTeam(mobDefinition.team);
                g.GetComponent<NetworkObject>().Spawn(true);
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