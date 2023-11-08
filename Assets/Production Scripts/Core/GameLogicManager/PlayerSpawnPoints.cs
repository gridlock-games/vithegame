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

        public TransformData GetSpawnOrientation(GameLogicManager.GameMode gameMode, GameLogicManager.Team team)
        {
            List<TransformData> possibleSpawnPoints = GetPossibleSpawnOrientations(gameMode, team);
            TransformData spawnPoint = possibleSpawnPoints[Random.Range(0, possibleSpawnPoints.Count)];
            return spawnPoint;
        }

        private List<TransformData> GetPossibleSpawnOrientations(GameLogicManager.GameMode gameMode, GameLogicManager.Team team)
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
                Gizmos.color = GameLogicManager.GetTeamColor(spawnPoint.teams[0]);
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
            public GameLogicManager.GameMode[] gameModes;
            public GameLogicManager.Team[] teams;
            public Vector3[] spawnPositions;
            public Vector3[] spawnRotations;
        }
    }
}