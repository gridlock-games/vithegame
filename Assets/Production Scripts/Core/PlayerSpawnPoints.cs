using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.Core
{
    public class PlayerSpawnPoints : MonoBehaviour
    {
        [SerializeField] private TransformData[] environmentViewPoints = new TransformData[0];
        [SerializeField] private TransformData[] gameItemSpawnPoints = new TransformData[0];
        [SerializeField] private SpawnPointDefinition[] spawnPoints = new SpawnPointDefinition[0];

        [Header("Damage Circle")]
        [SerializeField] private Vector3 damageCircleMaxScale = new Vector3(100, 200, 100);
        [SerializeField] private Vector3 damageCircleMinScale = new Vector3(5, 200, 5);
        [SerializeField] private float shrinkSize = 20;

        [Header("Essence War")]
        public TransformData ancientBossLightSpawnPoint;
        public TransformData ancientBossCorruptSpawnPoint;
        public TransformData ancientBossNeutralSpawnPoint;

        public TransformData[] GetEnvironmentViewPoints() { return environmentViewPoints; }

        public TransformData[] GetGameItemSpawnPoints() { return gameItemSpawnPoints; }

        public Vector3 GetDamageCircleMaxScale() { return damageCircleMaxScale; }

        public Vector3 GetDamageCircleMinScale() { return damageCircleMinScale; }

        public float GetDamageCircleShrinkSize() { return shrinkSize; }

        [System.Serializable]
        public struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;

            public TransformData(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }

        public (bool, TransformData) GetSpawnOrientation(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team)
        {
            List<TransformData> possibleSpawnPoints = GetPossibleSpawnOrientations(gameMode, team);
            if (possibleSpawnPoints.Count == 0) { Debug.LogError("Possible spawn point count is 0! - Game mode: " + gameMode + " - Team: " + team); }

            List<(float, TransformData)> verifiedSpawnPoints = new List<(float, TransformData)>();
            List<Attributes> activePlayerObjects = PlayerDataManager.Singleton.GetActivePlayerObjects();
            foreach (TransformData transformData in possibleSpawnPoints)
            {
                float minDistance = Mathf.Infinity;
                foreach (Attributes attributes in activePlayerObjects)
                {
                    float distance = Vector3.Distance(attributes.transform.position, transformData.position);
                    if (distance < minDistance) { minDistance = distance; }
                }
                verifiedSpawnPoints.Add((minDistance, transformData));
            }
            // Get the spawn points where we have the largest minimum distance to another player object
            float minDistanceInList = verifiedSpawnPoints.Max(item => item.Item1);
            verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 == minDistanceInList).ToList();

            float distanceOfSelectedPoint = Mathf.Infinity;
            TransformData spawnPoint = new TransformData();
            if (verifiedSpawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, verifiedSpawnPoints.Count);
                (distanceOfSelectedPoint, spawnPoint) = verifiedSpawnPoints[randomIndex];
            }

            return (distanceOfSelectedPoint > spawnDistanceThreshold, spawnPoint);
        }

        private const float spawnDistanceThreshold = 5;

        private List<TransformData> GetPossibleSpawnOrientations(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team)
        {
            List<TransformData> returnedSpawnPoints = new List<TransformData>();
            foreach (SpawnPointDefinition spawnPoint in spawnPoints)
            {
                if (spawnPoint.gameModes.Contains(gameMode) & spawnPoint.teams.Contains(team))
                {
                    for (int i = 0; i < spawnPoint.spawnPositions.Length; i++)
                    {
                        returnedSpawnPoints.Add(new TransformData(spawnPoint.spawnPositions[i], Quaternion.Euler(spawnPoint.spawnRotations[i])));
                    }
                }
            }
            return returnedSpawnPoints;
        }


        [Header("Gizmos")]
        [SerializeField] private bool displayDefaultGizmos;
        [SerializeField] private bool displayDamageCircleGizmos;
        [SerializeField] private bool displayEssenceWarGizmos;

        private void OnDrawGizmos()
        {
            if (displayDefaultGizmos)
            {
                foreach (TransformData transformData in environmentViewPoints)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(transformData.position, 2);
                    Gizmos.color = Color.black;
                    Gizmos.DrawRay(transformData.position, transformData.rotation * Vector3.forward * 5);
                }

                foreach (TransformData transformData in gameItemSpawnPoints)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(transformData.position, 0.5f);
                    Gizmos.DrawRay(transformData.position, transformData.rotation * Vector3.forward);
                }

                foreach (SpawnPointDefinition spawnPoint in spawnPoints)
                {
                    Gizmos.color = PlayerDataManager.GetTeamColor(spawnPoint.teams[0]);
                    for (int i = 0; i < spawnPoint.spawnPositions.Length; i++)
                    {
                        Vector3 spawnPosition = spawnPoint.spawnPositions[i];
                        Quaternion spawnRotation = Quaternion.Euler(spawnPoint.spawnRotations[i]);

                        Gizmos.DrawWireSphere(spawnPosition, 2);
                        Gizmos.DrawRay(spawnPosition, spawnRotation * Vector3.forward * 5);
                    }
                }
            }
            
            if (displayDamageCircleGizmos)
            {
                // Damage Circle
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(Vector3.zero, damageCircleMaxScale.x / 2);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(Vector3.zero, damageCircleMinScale.x / 2);
            }
            
            if (displayEssenceWarGizmos)
            {
                // Essence War
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(ancientBossLightSpawnPoint.position, 0.5f);
                Gizmos.DrawRay(ancientBossLightSpawnPoint.position, ancientBossLightSpawnPoint.rotation * Vector3.forward);

                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(ancientBossCorruptSpawnPoint.position, 0.5f);
                Gizmos.DrawRay(ancientBossCorruptSpawnPoint.position, ancientBossCorruptSpawnPoint.rotation * Vector3.forward);

                Gizmos.color = Color.black;
                Gizmos.DrawSphere(ancientBossNeutralSpawnPoint.position, 0.5f);
                Gizmos.DrawRay(ancientBossNeutralSpawnPoint.position, ancientBossNeutralSpawnPoint.rotation * Vector3.forward);
            }
        }

        [System.Serializable]
        public class SpawnPointDefinition
        {
            public PlayerDataManager.GameMode[] gameModes;
            public PlayerDataManager.Team[] teams;
            public Vector3[] spawnPositions;
            public Vector3[] spawnRotations;
        }
    }
}