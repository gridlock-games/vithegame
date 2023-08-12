using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

namespace LightPat.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        [SerializeField] private TeamSpawnPoint[] spawnPoints = new TeamSpawnPoint[0];

        public virtual void OnPlayerKill(ulong killerClientId) { }

        public virtual void OnPlayerDeath(ulong deathClientId) { }

        private Dictionary<Team, int> spawnCountDict = new Dictionary<Team, int>();
        public KeyValuePair<Vector3, Quaternion> GetSpawnOrientation(Team team)
        {
            if (!spawnCountDict.ContainsKey(team))
                spawnCountDict.Add(team, 0);

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            bool spawnPointFound = false;
            foreach (TeamSpawnPoint teamSpawnPoint in spawnPoints)
            {
                if (teamSpawnPoint.team == team)
                {
                    spawnPointFound = true;

                    // If we are out of bounds with our spawn count, reset the count
                    if (spawnCountDict[team] >= teamSpawnPoint.spawnPositions.Length)
                        spawnCountDict[team] = 0;
                    
                    spawnPosition = teamSpawnPoint.spawnPositions[spawnCountDict[team]];
                    spawnRotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }

            if (!spawnPointFound)
            {
                Debug.LogError("No spawn point found for team: " + team + ". It will use the first spawn point in the list");

                if (spawnPoints.Length > 1)
                {
                    spawnPosition = spawnPoints[0].spawnPositions[0];
                    spawnRotation = Quaternion.Euler(spawnPoints[0].spawnRotation);
                }
                else
                {
                    Debug.LogError("Game Logic Manager's spawn point array length is less than 1");
                }
            }

            spawnCountDict[team] += 1;
            return new KeyValuePair<Vector3, Quaternion>(spawnPosition, spawnRotation);
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

                foreach (Vector3 spawnPosition in spawnPoint.spawnPositions)
                {
                    Gizmos.DrawWireSphere(spawnPosition, 2);
                    Gizmos.DrawRay(spawnPosition, Quaternion.Euler(spawnPoint.spawnRotation) * Vector3.forward * 5);
                }
            }
        }
    }

    [Serializable]
    public class TeamSpawnPoint
    {
        public Team team;
        public Vector3[] spawnPositions;
        public Vector3 spawnRotation;
    }

    public enum Team
    {
        Environment,
        Competitor,
        Red,
        Blue,
        Spectator
    }

    public enum GameMode
    {
        Duel,
        TeamElimination,
        TeamDeathmatch
    }
}