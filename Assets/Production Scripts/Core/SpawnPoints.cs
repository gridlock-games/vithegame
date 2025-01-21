using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vi.Core.CombatAgents;

namespace Vi.Core
{
    public class SpawnPoints : MonoBehaviour
    {
        [SerializeField] private Vector3[] playerPreviewModelSpawnPoints = new Vector3[0];

        public float MinRenderDistance { get { return minimumRenderDistance; } }
        [SerializeField] private float minimumRenderDistance = 100;
        [SerializeField] private TransformData[] environmentViewPoints = new TransformData[0];
        [SerializeField] private TransformData[] gameItemSpawnPoints = new TransformData[0];
        [SerializeField] private SpawnPointDefinition[] spawnPoints = new SpawnPointDefinition[0];

        public Vector3 GetCharacterPreviewPosition()
        {
            if (playerPreviewModelSpawnPoints.Length > 0)
            {
                return playerPreviewModelSpawnPoints[Random.Range(0, playerPreviewModelSpawnPoints.Length)];
            }
            else
            {
                Debug.LogWarning("Player Preview Model Spawn Points Length is 0!");
                return new Vector3(0, 5, 0);
            }
        }

        public Vector3 GetCharacterPreviewPosition(int playerDataIdToPreview)
        {
            if (PlayerDataManager.Singleton.IdHasLocalPlayer(playerDataIdToPreview))
            {
                return PlayerDataManager.Singleton.GetPlayerObjectById(playerDataIdToPreview).transform.position;
            }
            else
            {
                foreach (CombatAgent combatAgent in PlayerDataManager.Singleton.GetActiveCombatAgents())
                {
                    return combatAgent.transform.position;
                }
            }
            return new Vector3(0, 5, 0);
        }

        public static readonly Vector3 previewCharacterRotation = new Vector3(0, 180, 0);

        // Old
        //public static readonly Vector3 cameraPreviewCharacterPositionOffset = new Vector3(0, 1.733f, -2.592f);
        //public static readonly Vector3 cameraPreviewCharacterRotation = new Vector3(18.07f, 0, 0);

        // Calculated from character select
        public static readonly Vector3 cameraPreviewCharacterPositionOffset = new Vector3(0, 0.8f, -3);
        public static readonly Vector3 cameraPreviewCharacterRotation = new Vector3(0, 0, 0);

        public static readonly Vector3 previewLightPositionOffset = new Vector3(0, 3.45f, 2.48f);
        public static readonly Vector3 previewLightRotation = new Vector3(30, 0, 0);

        [Header("Damage Circle")]
        [SerializeField] private Vector3 damageCircleSpawnPosition;
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
                // Get list of spawn points where min distance to nearest player is greater than threshold
                var thresholdFilteredSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 > spawnPointFoundDistanceThreshold).ToList();

                // Take the highest spawn priority from that list
                if (thresholdFilteredSpawnPoints.Count > 0)
                {
                    int maxSpawnPriority = thresholdFilteredSpawnPoints.Max(item => item.Item3);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority & item.Item1 > spawnPointFoundDistanceThreshold).ToList();
                }
                else
                {
                    // Sort by distance
                    float maxDistanceInList = verifiedSpawnPoints.Max(item => item.Item1);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 == maxDistanceInList).ToList();

                    // Sort by spawn priority
                    int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
                }
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
            return (!findMostClearSpawnPoint || distanceOfSelectedPoint > spawnPointFoundDistanceThreshold, spawnPoint);
        }

        public (bool, TransformData) GetRespawnOrientation(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team, Attributes attributesToExcludeInLogic)
        {
            (bool findMostClearSpawnPoint, List<(int, TransformData)> possibleSpawnPoints) = GetPossibleSpawnOrientations(gameMode, team);
            if (possibleSpawnPoints.Count == 0) { Debug.LogError("Possible spawn point count is 0! - Game mode: " + gameMode + " - Team: " + team); }

            List<(float, TransformData, int)> verifiedSpawnPoints = new List<(float, TransformData, int)>();
            List<CombatAgent> activeCombatAgents = PlayerDataManager.Singleton.GetActiveCombatAgents(attributesToExcludeInLogic);
            foreach ((int spawnPriority, TransformData transformData) in possibleSpawnPoints)
            {
                float minDistance = Mathf.Infinity;
                foreach (CombatAgent combatAgent in activeCombatAgents)
                {
                    float distance = Vector3.Distance(combatAgent.MovementHandler.GetPosition(), transformData.position);
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
                // Get list of spawn points where min distance to nearest player is greater than threshold
                var thresholdFilteredSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 > spawnPointFoundDistanceThreshold).ToList();

                // Take the highest spawn priority from that list
                if (thresholdFilteredSpawnPoints.Count > 0)
                {
                    int maxSpawnPriority = thresholdFilteredSpawnPoints.Max(item => item.Item3);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority & item.Item1 > spawnPointFoundDistanceThreshold).ToList();
                }
                else
                {
                    // Sort by distance
                    float maxDistanceInList = verifiedSpawnPoints.Max(item => item.Item1);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item1 == maxDistanceInList).ToList();

                    // Sort by spawn priority
                    int maxSpawnPriority = verifiedSpawnPoints.Max(item => item.Item3);
                    verifiedSpawnPoints = verifiedSpawnPoints.Where(item => item.Item3 == maxSpawnPriority).ToList();
                }
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
            return (!findMostClearSpawnPoint || distanceOfSelectedPoint > spawnPointFoundDistanceThreshold, spawnPoint);
        }

        private (bool, List<(int, TransformData)>) GetPossibleSpawnOrientations(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team)
        {
            bool findMostClearSpawnPoint = gameMode == PlayerDataManager.GameMode.FreeForAll
                | gameMode == PlayerDataManager.GameMode.HordeMode;

            if (team == PlayerDataManager.Team.Spectator) { findMostClearSpawnPoint = false; }

            List<(int, TransformData)> returnedSpawnPoints = new List<(int, TransformData)>();
            foreach (SpawnPointDefinition spawnPoint in spawnPoints)
            {
                if (spawnPoint.gameModes.Contains(gameMode) & spawnPoint.teams.Contains(team))
                {
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
            public Vector3[] spawnPositions = new Vector3[0];
            public Vector3[] spawnRotations = new Vector3[0];
            public int[] spawnPriorities = new int[0];
        }

        [SerializeField] private MobSpawnPointDefinition[] mobSpawnPoints = new MobSpawnPointDefinition[0];

        private int mobSpawnPointIndex = -1;
        public TransformData GetGenericMobSpawnPoint()
        {
            if (mobSpawnPoints.Length == 0) { return new TransformData(new Vector3(Random.Range(-10, 10), Random.Range(3, 5), Random.Range(-10, 10)), default); }
            mobSpawnPointIndex++;
            if (mobSpawnPointIndex >= mobSpawnPoints.Length) { mobSpawnPointIndex = 0; }
            MobSpawnPointDefinition mobSpawnPointDefinition = mobSpawnPoints[mobSpawnPointIndex];
            if (mobSpawnPointDefinition == null) { Debug.LogWarning("Could not find generic mob spawn point!"); }
            else { return mobSpawnPointDefinition.GetRandomOrientation(); }
            return default;
        }

        public TransformData GetMobSpecificSpawnPoint(Mob mobPrefab, PlayerDataManager.Team team)
        {
            if (mobSpawnPoints.Length == 0) { return new TransformData(new Vector3(Random.Range(-10, 10), Random.Range(3, 5), Random.Range(-10, 10)), default); }

            int index = System.Array.FindIndex(mobSpawnPoints, item => item.mobPrefab.name == mobPrefab.name & item.team == team);
            if (index == -1)
            {
                Debug.LogWarning("Mob spawn point index is -1! This should never happen");
                return new TransformData(new Vector3(Random.Range(-10, 10), Random.Range(3, 5), Random.Range(-10, 10)), default);
            }
            else
            {
                return mobSpawnPoints[index].GetRandomOrientation();
            }
        }

        [System.Serializable]
        private class MobSpawnPointDefinition
        {
            public Mob mobPrefab;
            public PlayerDataManager.Team team;
            public BoxCollider[] spawnAreas;
            public Vector3[] spawnRotations;

            public TransformData GetRandomOrientation()
            {
                TransformData transformData = new TransformData();

                BoxCollider spawnArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
                Vector3 extents = spawnArea.size / 2f;
                Vector3 point = new Vector3(
                    Random.Range(-extents.x, extents.x),
                    Random.Range(-extents.y, extents.y),
                    Random.Range(-extents.z, extents.z)
                );
                transformData.position = spawnArea.transform.TransformPoint(point);

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
        [SerializeField] private bool displayPreviewCharacterGizmos;

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
                    Gizmos.color = spawnPoint.teams[0] == PlayerDataManager.Team.Competitor ? Color.black : PlayerDataManager.GetTeamColor(spawnPoint.teams[0]);
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
                Gizmos.DrawWireSphere(damageCircleSpawnPosition, damageCircleMaxScale.x / 2);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(damageCircleSpawnPosition, damageCircleMinScale.x / 2);
            }

            if (displayPreviewCharacterGizmos)
            {
                Gizmos.color = Color.black;
                foreach (Vector3 position in playerPreviewModelSpawnPoints)
                {
                    Gizmos.DrawWireSphere(position, 0.4f);
                }
            }
        }
    }
}