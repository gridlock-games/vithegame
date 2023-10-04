using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

namespace LightPat.Core
{
    public class TeamSpawnManager : MonoBehaviour
    {
        [SerializeField] private TeamSpawnPoint[] spawnPoints = new TeamSpawnPoint[0];

        public TeamSpawnPoint[] GetSpawnPoints(GameMode gameMode)
        {
            List<TeamSpawnPoint> returnedSpawnPoints = new List<TeamSpawnPoint>();
            foreach (TeamSpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint.gameModes.Contains(gameMode)) { returnedSpawnPoints.Add(spawnPoint); }
            }

            return returnedSpawnPoints.ToArray();
        }

        private void OnDrawGizmos()
        {
            foreach (TeamSpawnPoint spawnPoint in spawnPoints)
            {
                try
                {
                    Gizmos.color = (Color)typeof(Color).GetProperty(spawnPoint.team.ToString().ToLowerInvariant()).GetValue(null, null);
                }
                catch
                {
                    Gizmos.color = Color.black;
                }

                for (int i = 0; i < spawnPoint.spawnPositions.Length; i++)
                {
                    Vector3 spawnPosition = spawnPoint.spawnPositions[i];
                    Quaternion spawnRotation = Quaternion.Euler(spawnPoint.spawnRotations[i]);

                    Gizmos.DrawWireSphere(spawnPosition, 2);
                    Gizmos.DrawRay(spawnPosition, spawnRotation * Vector3.forward * 5);
                }
            }
        }
    }

    [Serializable]
    public class TeamSpawnPoint
    {
        public GameMode[] gameModes;
        public Team team;
        public Vector3[] spawnPositions;
        public Vector3 spawnRotation;
        public Vector3[] spawnRotations;
    }
}