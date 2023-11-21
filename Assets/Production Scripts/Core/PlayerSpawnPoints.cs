using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.Core
{
    public class PlayerSpawnPoints : MonoBehaviour
    {
        [SerializeField] private SpawnPointDefinition[] spawnPoints = new SpawnPointDefinition[0];

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

        public void ResetSpawnTracker()
        {
            spawnIndexTracker.Clear();
        }

        private List<int> spawnIndexTracker = new List<int>();
        public TransformData GetSpawnOrientation(PlayerDataManager.GameMode gameMode, PlayerDataManager.Team team)
        {
            List<TransformData> possibleSpawnPoints = GetPossibleSpawnOrientations(gameMode, team);
            bool shouldResetSpawnTracker = false;
            foreach (int index in spawnIndexTracker)
            {
                try
                {
                    possibleSpawnPoints.RemoveAt(index);
                }
                catch
                {
                    shouldResetSpawnTracker = true;
                    break;
                }
            }

            if (shouldResetSpawnTracker)
            {
                ResetSpawnTracker();
                possibleSpawnPoints = GetPossibleSpawnOrientations(gameMode, team);
            }

            int randomIndex = Random.Range(0, possibleSpawnPoints.Count);
            spawnIndexTracker.Add(randomIndex);
            TransformData spawnPoint = new TransformData();
            if (possibleSpawnPoints.Count != 0)
                spawnPoint = possibleSpawnPoints[randomIndex];
            return spawnPoint;
        }

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

        private void OnDrawGizmos()
        {
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