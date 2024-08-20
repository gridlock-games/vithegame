using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vi.Core.CombatAgents;

namespace Vi.Core
{
    public class SpawnPoints : MonoBehaviour
    {
        [SerializeField] private TransformData[] environmentViewPoints = new TransformData[0];
        [SerializeField] private TransformData[] gameItemSpawnPoints = new TransformData[0];
        [SerializeField] private SpawnPointDefinition[] spawnPoints = new SpawnPointDefinition[0];

        [Header("MVP Presentation Info")]
        public Vector3 previewCharacterPosition;

        public static readonly Vector3 previewCharacterPositionOffset = new Vector3(0, 0.3f, -7);
        public static readonly Vector3 previewCharacterRotation = new Vector3(0, 180, 0);

        public static readonly Vector3 cameraPreviewCharacterPositionOffset = new Vector3(0, 2.033f, -9.592f);
        public static readonly Vector3 cameraPreviewCharacterRotation = new Vector3(18.07f, 0, 0);

        [Header("Damage Circle")]
        [SerializeField] private Vector3 damageCircleMaxScale = new Vector3(100, 200, 100);
        [SerializeField] private Vector3 damageCircleMinScale = new Vector3(5, 200, 5);
        [SerializeField] private float shrinkSize = 20;

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

        private const float spawnPointFoundDistanceThreshold = 5;

        public (bool, TransformData) GetSpawnOrientation(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team, int channel)
        {
            (bool findMostClearSpawnPoint, List<(int, TransformData)> possibleSpawnPoints) = GetPossibleSpawnOrientations(gameMode, team);
            if (possibleSpawnPoints.Count == 0)
            {
                Debug.LogError("Possible spawn point count is 0! - Game mode: " + gameMode + " - Team: " + team);
                return default;
            }

            List<(float, TransformData, int)> verifiedSpawnPoints = new List<(float, TransformData, int)>();
            List<Attributes> activePlayerObjects = PlayerDataManager.Singleton.GetActivePlayerObjectsInChannel(channel);
            foreach ((int spawnPriority, TransformData transformData) in possibleSpawnPoints)
            {
                float minDistance = Mathf.Infinity;
                foreach (Attributes attributes in activePlayerObjects)
                {
                    float distance = Vector3.Distance(attributes.MovementHandler.GetPosition(), transformData.position);
                    if (distance < minDistance) { minDistance = distance; }
                }
                verifiedSpawnPoints.Add((minDistance, transformData, spawnPriority));
            }

            if (findMostClearSpawnPoint)
            {
                // Get the spawn points where we have the largest minimum distance to another player object
                float maxDistanceInList = verifiedSpawnPoints.Max(item => item.Item1);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 == maxDistanceInList).ToList();

                // Take the highest spawn priority from that list
                int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
            }
            else // Don't find most clear spawn point
            {
                // Get list of spawn points where 
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 > spawnPointFoundDistanceThreshold).ToList();

                // Take the highest spawn priority from that list
                int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
            }

            float distanceOfSelectedPoint = Mathf.Infinity;
            TransformData spawnPoint = new TransformData();
            if (verifiedSpawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, verifiedSpawnPoints.Count);
                (distanceOfSelectedPoint, spawnPoint, _) = verifiedSpawnPoints[randomIndex];
            }
            else
            {
                int randomIndex = Random.Range(0, possibleSpawnPoints.Count);
                (distanceOfSelectedPoint, spawnPoint) = possibleSpawnPoints[randomIndex];
                distanceOfSelectedPoint = 0;
            }
            return (distanceOfSelectedPoint > spawnPointFoundDistanceThreshold, spawnPoint);
        }

        public (bool, TransformData) GetRespawnOrientation(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team, Attributes attributesToExcludeInLogic)
        {
            (bool findMostClearSpawnPoint, List<(int, TransformData)> possibleSpawnPoints) = GetPossibleSpawnOrientations(gameMode, team);
            if (possibleSpawnPoints.Count == 0) { Debug.LogError("Possible spawn point count is 0! - Game mode: " + gameMode + " - Team: " + team); }

            List<(float, TransformData, int)> verifiedSpawnPoints = new List<(float, TransformData, int)>();
            List<Attributes> activePlayerObjects = PlayerDataManager.Singleton.GetActivePlayerObjects(attributesToExcludeInLogic);
            foreach ((int spawnPriority, TransformData transformData) in possibleSpawnPoints)
            {
                float minDistance = Mathf.Infinity;
                foreach (Attributes attributes in activePlayerObjects)
                {
                    float distance = Vector3.Distance(attributes.MovementHandler.GetPosition(), transformData.position);
                    if (distance < minDistance) { minDistance = distance; }
                }
                verifiedSpawnPoints.Add((minDistance, transformData, spawnPriority));
            }

            if (findMostClearSpawnPoint)
            {
                // Get the spawn points where we have the largest minimum distance to another player object
                float maxDistanceInList = verifiedSpawnPoints.Max(item => item.Item1);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 == maxDistanceInList).ToList();

                // Take the highest spawn priority from that list
                int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
            }
            else // Don't find most clear spawn point
            {
                // Get list of spawn points where 
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 > spawnPointFoundDistanceThreshold).ToList();

                // Take the highest spawn priority from that list
                int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
            }
            
            float distanceOfSelectedPoint = Mathf.Infinity;
            TransformData spawnPoint = new TransformData();
            if (verifiedSpawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, verifiedSpawnPoints.Count);
                (distanceOfSelectedPoint, spawnPoint, _) = verifiedSpawnPoints[randomIndex];
            }
            else
            {
                int randomIndex = Random.Range(0, possibleSpawnPoints.Count);
                (distanceOfSelectedPoint, spawnPoint) = possibleSpawnPoints[randomIndex];
                distanceOfSelectedPoint = 0;
            }
            return (distanceOfSelectedPoint > spawnPointFoundDistanceThreshold, spawnPoint);
        }

        private (bool, List<(int, TransformData)>) GetPossibleSpawnOrientations(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team)
        {
            bool findMostClearSpawnPoint = true;
            List<(int, TransformData)> returnedSpawnPoints = new List<(int, TransformData)>();
            foreach (SpawnPointDefinition spawnPoint in spawnPoints)
            {
                if (spawnPoint.gameModes.Contains(gameMode) & spawnPoint.teams.Contains(team))
                {
                    findMostClearSpawnPoint = spawnPoint.findMostClearSpawnPoint;
                    for (int i = 0; i < spawnPoint.spawnPositions.Length; i++)
                    {
                        if (i < spawnPoint.spawnPriorities.Length)
                            returnedSpawnPoints.Add((spawnPoint.spawnPriorities[i], new TransformData(spawnPoint.spawnPositions[i], Quaternion.Euler(spawnPoint.spawnRotations[i]))));
                        else
                            returnedSpawnPoints.Add((0, new TransformData(spawnPoint.spawnPositions[i], Quaternion.Euler(spawnPoint.spawnRotations[i]))));
                    }
                }
            }
            return (findMostClearSpawnPoint, returnedSpawnPoints);
        }

        [System.Serializable]
        public class SpawnPointDefinition
        {
            public PlayerDataManager.GameMode[] gameModes;
            public PlayerDataManager.Team[] teams;
            public bool findMostClearSpawnPoint = true;
            public Vector3[] spawnPositions = new Vector3[0];
            public Vector3[] spawnRotations = new Vector3[0];
            public int[] spawnPriorities = new int[0];
        }

        [SerializeField] private MobSpawnPointDefinition[] mobSpawnPoints = new MobSpawnPointDefinition[0];

        private int mobSpawnPointIndex = -1;
        public TransformData GetMobSpawnPoint(Mob mobPrefab)
        {
            mobSpawnPointIndex++;
            if (mobSpawnPointIndex >= mobSpawnPoints.Length) { mobSpawnPointIndex = 0; }
            MobSpawnPointDefinition mobSpawnPointDefinition = mobSpawnPoints[mobSpawnPointIndex];
            if (mobSpawnPointDefinition == null) { Debug.LogError("Could not find mob spawn point for mob prefab! " + mobPrefab); }
            else { return mobSpawnPointDefinition.GetRandomOrientation(); }
            return default;
        }

        [System.Serializable]
        private class MobSpawnPointDefinition
        {
            public Vector3 spawnPosition;
            public Vector3 halfExtents;
            public Vector3[] spawnRotations;

            public TransformData GetRandomOrientation()
            {
                TransformData transformData = new TransformData();

                Vector3 pos = new Vector3();
                pos.x = Random.Range(spawnPosition.x-halfExtents.x, spawnPosition.x+halfExtents.x);
                pos.y = Random.Range(spawnPosition.y - halfExtents.y, spawnPosition.y + halfExtents.y);
                pos.z = Random.Range(spawnPosition.z - halfExtents.z, spawnPosition.z + halfExtents.z);
                transformData.position = pos;

                if (spawnRotations.Length > 0)
                {
                    transformData.rotation = Quaternion.Euler(spawnRotations[Random.Range(0, spawnRotations.Length)]);
                }
                return transformData;
            }
        }

        [Header("Gizmos")]
        [SerializeField] private bool displayDefaultGizmos = true;
        [SerializeField] private bool displayDamageCircleGizmos;

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

                foreach (MobSpawnPointDefinition mobSpawnPointDefinition in mobSpawnPoints)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(mobSpawnPointDefinition.spawnPosition, mobSpawnPointDefinition.halfExtents);
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
        }
    }
}